using UnityEngine;
using Strawhenge.Common.Unity;
using System.Collections.Generic;

namespace Strawhenge.Navigation.Unity
{
    // Goes on the Animator's GameObject: PivotStateMachineBehaviour and LandingStateMachineBehaviour find it
    // through their Animator.
    [RequireComponent(typeof(Animator))]
    public partial class MovementAnimationSyncScript : MonoBehaviour
    {
        [SerializeField] MovementSourceManagerScript _movementSourceManager;
        [SerializeField] float _dampeningTime = 0.1f;

        [Tooltip("How long a requested pivot or landing animation may take to start (enter its state machine) " +
                 "before it's skipped, e.g. when the controller has no transition for its ID, or no pivot or " +
                 "landing behaviour. Only counts while the Animator is on. Must be longer than the transitions " +
                 "into the pivot and landing state machines take to start.")]
        [SerializeField] float _animationStartTimeout = 0.3f;

        Animator _animator;
        Transform _leftFoot;
        Transform _rightFoot;

        // What the movement source last said. Set on the Animator again every LateUpdate, as Unity resets its
        // parameters when it's turned off and on.
        bool _isJumping;
        bool _isFalling;

        // A pivot or landing requested from the Animator that hasn't started yet.
        int? _pendingPivotId;
        float _pendingPivotTime;
        int? _pendingLandingId;
        float _pendingLandingTime;
        readonly HashSet<int> _unstartedPivotIds = new();

        // A pivot or landing animation that has started and not yet ended. Cleared when it's cancelled, so
        // its end, if it still plays out, doesn't complete whatever the movement source is waiting for now.
        bool _isPivotPlaying;
        bool _isLandingPlaying;
        readonly HashSet<int> _unstartedLandingIds = new();

        void Awake()
        {
            _animator = GetComponent<Animator>();

            // Foot bones only exist on humanoid rigs; without them 'Pivot Right Foot' stays false.
            if (_animator.isHuman)
            {
                _leftFoot = _animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                _rightFoot = _animator.GetBoneTransform(HumanBodyBones.RightFoot);
            }

            SubscribeToMovement();
        }

        // The GameObject was deactivated (e.g. pooled), so Update stops too.
        void OnDisable() => CompleteAnimatorRequests();

        void OnDestroy() => UnsubscribeFromMovement();

        // Skips a pivot or landing whose animation never starts (usually no transition for its ID, or no pivot or
        // landing behaviour), and cleans up what it set on the Animator.
        void Update()
        {
            // Turned off (e.g. ragdolled): nothing can play or finish a pivot or landing, and Unity resets the
            // Animator when it's turned on again. Complete them now rather than leave them to the timeouts.
            if (!_animator.isActiveAndEnabled)
            {
                CompleteAnimatorRequests();
                return;
            }

            var deltaTime = _animator.updateMode == AnimatorUpdateMode.UnscaledTime
                ? Time.unscaledDeltaTime
                : Time.deltaTime;

            if (_pendingPivotId.HasValue)
            {
                _pendingPivotTime += deltaTime;

                if (_pendingPivotTime > _animationStartTimeout)
                {
                    var pivotId = _pendingPivotId.Value;
                    AbandonPendingPivot();
                    WarnNotStarted(_unstartedPivotIds, pivotId, "Pivot", nameof(PivotStateMachineBehaviour));

                    _movementSourceManager?.CompletePivot();
                }
            }

            if (_pendingLandingId.HasValue)
            {
                _pendingLandingTime += deltaTime;

                if (_pendingLandingTime > _animationStartTimeout)
                {
                    var landingId = _pendingLandingId.Value;
                    AbandonPendingLanding();
                    WarnNotStarted(_unstartedLandingIds, landingId, "Landing", nameof(LandingStateMachineBehaviour));

                    _movementSourceManager?.CompleteLanding();
                }
            }
        }

        public void OnJumpLaunch()
        {
            _movementSourceManager?.TriggerJump();
        }

        void LateUpdate()
        {
            if (_movementSourceManager == null || !_animator.isActiveAndEnabled)
                return;

            ApplyJumpingAndFalling();

            // Relative to the animated body, not transform.root, which is a container if the character has a parent.
            var velocity = _animator.transform.InverseTransformDirection(_movementSourceManager.CurrentVelocity);
            velocity.y = 0;

            if (_movementSourceManager.Strafe)
            {
                _animator.SetBool(AnimatorParameters.Strafing, true);
                _animator.SetFloat(AnimatorParameters.MoveSpeed, 0);

                _animator
                    .SetFloat(
                        AnimatorParameters.StrafeX,
                        velocity.x,
                        _dampeningTime,
                        Time.deltaTime);

                _animator
                    .SetFloat(
                        AnimatorParameters.StrafeY,
                        velocity.z,
                        _dampeningTime,
                        Time.deltaTime);
                return;
            }

            _animator.SetBool(AnimatorParameters.Strafing, false);
            _animator.SetFloat(AnimatorParameters.StrafeX, 0);
            _animator.SetFloat(AnimatorParameters.StrafeY, 0);

            _animator.SetFloat(
                AnimatorParameters.MoveSpeed,
                velocity.magnitude,
                _dampeningTime,
                Time.deltaTime);
        }

        // Called by PivotStateMachineBehaviour.
        internal void OnPivotStarted()
        {
            _pendingPivotId = null;
            _isPivotPlaying = true;
        }

        // Called by PivotStateMachineBehaviour.
        internal void OnPivotEnded()
        {
            // Cancelled: the movement source has stopped waiting for it.
            if (!_isPivotPlaying)
                return;

            _isPivotPlaying = false;
            _pendingPivotId = null;

            // Root motion has already turned the character: the movement source measures the turn itself.
            _animator.applyRootMotion = false;

            _movementSourceManager?.CompletePivot();
        }

        // Called by LandingStateMachineBehaviour.
        internal void OnLandingStarted()
        {
            _pendingLandingId = null;
            _isLandingPlaying = true;
        }

        // Called by LandingStateMachineBehaviour.
        internal void OnLandingEnded()
        {
            // Cancelled: the movement source has stopped waiting for it.
            if (!_isLandingPlaying)
                return;

            _isLandingPlaying = false;
            _pendingLandingId = null;

            _movementSourceManager?.CompleteLanding();
        }

        void SubscribeToMovement()
        {
            if (_movementSourceManager == null)
                return;

            _movementSourceManager.JumpTriggerRequested += OnJumpTriggerRequested;
            _movementSourceManager.JumpBegan += OnJumpBegan;
            _movementSourceManager.JumpEnded += OnJumpEnded;
            _movementSourceManager.PivotRequested += OnPivotRequested;
            _movementSourceManager.FallBegan += OnFallBegan;
            _movementSourceManager.FallEnded += OnFallEnded;
            _movementSourceManager.LandingRequested += OnLandingRequested;
            _movementSourceManager.PivotCancelled += OnPivotCancelled;
            _movementSourceManager.LandingCancelled += OnLandingCancelled;
            _movementSourceManager.JumpTriggerCancelled += OnJumpTriggerCancelled;
            _movementSourceManager.ActiveSourceChanged += OnActiveSourceChanged;
        }

        void UnsubscribeFromMovement()
        {
            if (_movementSourceManager == null)
                return;

            _movementSourceManager.JumpTriggerRequested -= OnJumpTriggerRequested;
            _movementSourceManager.JumpBegan -= OnJumpBegan;
            _movementSourceManager.JumpEnded -= OnJumpEnded;
            _movementSourceManager.PivotRequested -= OnPivotRequested;
            _movementSourceManager.FallBegan -= OnFallBegan;
            _movementSourceManager.FallEnded -= OnFallEnded;
            _movementSourceManager.LandingRequested -= OnLandingRequested;
            _movementSourceManager.PivotCancelled -= OnPivotCancelled;
            _movementSourceManager.LandingCancelled -= OnLandingCancelled;
            _movementSourceManager.JumpTriggerCancelled -= OnJumpTriggerCancelled;
            _movementSourceManager.ActiveSourceChanged -= OnActiveSourceChanged;
        }

        // A jump profile matched: play its wind-up, which launches the jump from an animation event (OnJumpLaunch).
        void OnJumpTriggerRequested(int jumpId)
        {
            // An Animator that's off can't play the wind-up.
            if (!_animator.isActiveAndEnabled)
            {
                _movementSourceManager.TriggerJump();
                return;
            }

            _animator.SetInteger(AnimatorParameters.JumpId, jumpId);
            _animator.SetTrigger(AnimatorParameters.Jump);
        }

        void OnJumpBegan()
        {
            _isJumping = true;
            ApplyJumpingAndFalling();
        }

        void OnJumpEnded()
        {
            _isJumping = false;
            ApplyJumpingAndFalling();
        }

        void OnFallBegan()
        {
            _isFalling = true;
            ApplyJumpingAndFalling();
        }

        void OnFallEnded()
        {
            _isFalling = false;
            ApplyJumpingAndFalling();
        }

        void OnPivotRequested(int pivotId)
        {
            // An Animator that's off can't play the pivot, so complete it now, without turning.
            // The movement source then turns normally instead.
            if (!_animator.isActiveAndEnabled)
            {
                _movementSourceManager.CompletePivot();
                return;
            }

            _animator.SetInteger(AnimatorParameters.PivotId, pivotId);

            if (_leftFoot != null && _rightFoot != null)
            {
                var body = _animator.transform;
                var leftOffset = _leftFoot.position - body.position;
                var rightOffset = _rightFoot.position - body.position;

                var leftForward = Vector3.Dot(leftOffset, body.forward);
                var rightForward = Vector3.Dot(rightOffset, body.forward);

                _animator.SetBool(AnimatorParameters.PivotRightFoot, rightForward > leftForward);
            }

            _animator.applyRootMotion = true;
            _animator.SetTrigger(AnimatorParameters.Pivot);

            _pendingPivotId = pivotId;
            _pendingPivotTime = 0f;
        }

        void OnLandingRequested(int landingId)
        {
            // An Animator that's off can't play the landing, so complete it now.
            if (!_animator.isActiveAndEnabled)
            {
                _movementSourceManager.CompleteLanding();
                return;
            }

            _animator.SetInteger(AnimatorParameters.LandingId, landingId);
            _animator.SetTrigger(AnimatorParameters.Land);

            _pendingLandingId = landingId;
            _pendingLandingTime = 0f;
        }

        // The movement source no longer wants the pivot (e.g. the character can't navigate, or it timed out).
        // Undo what was set on the Animator for it, including root motion if the animation has started.
        void OnPivotCancelled()
        {
            _isPivotPlaying = false;
            AbandonPendingPivot();
        }

        void OnLandingCancelled()
        {
            _isLandingPlaying = false;
            AbandonPendingLanding();
        }

        // A deferred jump abandoned before its animation launched it.
        void OnJumpTriggerCancelled()
        {
            _animator.ResetTrigger(AnimatorParameters.Jump.Id);
        }

        // Another movement source took over. The old one has dropped its waits, and won't end a jump or fall
        // it was in the middle of, so drop everything set on the Animator for it, as if it had cancelled
        // them: a pivot or landing (started or not), a deferred jump's trigger, and the Jumping and Falling
        // flags. The new source sets what it needs. An animation already playing carries on, but without
        // root motion, and its end completes nothing.
        void OnActiveSourceChanged()
        {
            OnPivotCancelled();
            OnLandingCancelled();
            OnJumpTriggerCancelled();

            _isJumping = false;
            _isFalling = false;
            ApplyJumpingAndFalling();
        }

        // While the Animator is off there's nothing to set: Unity resets its parameters when it's turned on
        // again, and LateUpdate sets these again from then on.
        void ApplyJumpingAndFalling()
        {
            if (!_animator.isActiveAndEnabled)
                return;

            _animator.SetBool(AnimatorParameters.Jumping, _isJumping);
            _animator.SetBool(AnimatorParameters.Falling, _isFalling);
        }

        // The Animator was turned off, or this GameObject deactivated. A pivot or landing that was waiting to
        // start, or playing, will never finish, so complete it now (a pivot without turning). Unity resets the
        // Animator's parameters when it's turned on again, so only root motion needs undoing.
        void CompleteAnimatorRequests()
        {
            if (_pendingPivotId.HasValue || _isPivotPlaying)
            {
                _pendingPivotId = null;
                _isPivotPlaying = false;

                if (_animator != null)
                    _animator.applyRootMotion = false;

                if (_movementSourceManager != null)
                    _movementSourceManager.CompletePivot();
            }

            if (_pendingLandingId.HasValue || _isLandingPlaying)
            {
                _pendingLandingId = null;
                _isLandingPlaying = false;

                if (_movementSourceManager != null)
                    _movementSourceManager.CompleteLanding();
            }
        }

        void AbandonPendingPivot()
        {
            _pendingPivotId = null;
            _animator.ResetTrigger(AnimatorParameters.Pivot.Id);
            _animator.applyRootMotion = false;
        }

        void AbandonPendingLanding()
        {
            _pendingLandingId = null;
            _animator.ResetTrigger(AnimatorParameters.Land.Id);
        }

        void WarnNotStarted(HashSet<int> unstartedIds, int id, string kind, string behaviourName)
        {
            if (!unstartedIds.Add(id))
                return;

            Debug.LogWarning(
                $"{kind} ID {id} didn't start within {_animationStartTimeout} s - skipping it. " +
                $"Check the Animator controller has a {behaviourName} and a transition for {kind} ID {id}.",
                this);
        }
    }
}
