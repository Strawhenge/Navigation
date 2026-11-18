using UnityEngine;
using Strawhenge.Common.Unity;
using Strawhenge.Common.Unity.Helpers;
using System.Collections.Generic;

namespace Strawhenge.Navigation.Unity
{
    public partial class MovementAnimationSyncScript : MonoBehaviour
    {
        [SerializeField] Animator _animator;
        [SerializeField] MovementSourceManagerScript _movementSourceManager;
        [SerializeField] float _dampeningTime = 0.1f;

        [Tooltip("How long a requested pivot or landing animation may take to start (enter its state machine) " +
                 "before it's skipped, e.g. when the controller has no transition for its ID. Must be longer " +
                 "than the transitions into the pivot and landing state machines take to start.")]
        [SerializeField] float _animationStartTimeout = 0.3f;

        Transform _leftFoot;
        Transform _rightFoot;
        PivotStateMachineBehaviour _pivotBehaviour;
        LandingStateMachineBehaviour _landingBehaviour;

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

        // Setup problems are warned about once here, and fall back to moving without that animation,
        // so the character never waits for an animation that can't happen.
        void Awake()
        {
            ComponentRefHelper.EnsureHierarchyComponent(ref _animator, nameof(_animator), this);

            if (_animator == null)
            {
                Debug.LogWarning("No Animator - movement will play without animation.", this);
            }
            else
            {
                var controllerName = _animator.runtimeAnimatorController != null
                    ? $"'{_animator.runtimeAnimatorController.name}'"
                    : "The Animator (no controller assigned)";

                if (_pivotBehaviour == null)
                    _pivotBehaviour = _animator.GetBehaviour<PivotStateMachineBehaviour>();

                if (_pivotBehaviour != null)
                {
                    _pivotBehaviour.Started += OnPivotStarted;
                    _pivotBehaviour.Ended += OnPivotEnded;
                }
                else
                    Debug.LogWarning(
                        $"{controllerName} has no {nameof(PivotStateMachineBehaviour)} - pivots will be skipped.",
                        this);

                if (_landingBehaviour == null)
                    _landingBehaviour = _animator.GetBehaviour<LandingStateMachineBehaviour>();

                if (_landingBehaviour != null)
                {
                    _landingBehaviour.Started += OnLandingStarted;
                    _landingBehaviour.Ended += OnLandingEnded;
                }
                else
                    Debug.LogWarning(
                        $"{controllerName} has no {nameof(LandingStateMachineBehaviour)} - landings will be skipped.",
                        this);

                // Foot bones only exist on humanoid rigs; without them 'Pivot Right Foot' stays false.
                if (_animator.isHuman)
                {
                    _leftFoot = _animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                    _rightFoot = _animator.GetBoneTransform(HumanBodyBones.RightFoot);
                }
            }

            SubscribeToMovement();
        }

        void OnDestroy()
        {
            UnsubscribeFromMovement();

            if (_pivotBehaviour != null)
            {
                _pivotBehaviour.Started -= OnPivotStarted;
                _pivotBehaviour.Ended -= OnPivotEnded;
            }

            if (_landingBehaviour != null)
            {
                _landingBehaviour.Started -= OnLandingStarted;
                _landingBehaviour.Ended -= OnLandingEnded;
            }
        }

        // Skips a pivot or landing whose animation never starts (usually no transition for its ID), and
        // cleans up what it set on the Animator. Only counts while the Animator is updating; otherwise
        // LocomotionScript's own, longer timeout still applies.
        void Update()
        {
            if (_animator == null || !_animator.isActiveAndEnabled)
                return;

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
                    WarnNotStarted(_unstartedPivotIds, pivotId, "Pivot");

                    _movementSourceManager?.CompletePivot(Quaternion.identity);
                }
            }

            if (_pendingLandingId.HasValue)
            {
                _pendingLandingTime += deltaTime;

                if (_pendingLandingTime > _animationStartTimeout)
                {
                    var landingId = _pendingLandingId.Value;
                    AbandonPendingLanding();
                    WarnNotStarted(_unstartedLandingIds, landingId, "Landing");

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
            if (_movementSourceManager == null || _animator == null)
                return;

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

        void OnJumpTriggerRequested()
        {
            // The jump normally launches from an animation event (OnJumpLaunch).
            if (_animator == null)
            {
                _movementSourceManager?.TriggerJump();
                return;
            }

            _animator.SetTrigger(AnimatorParameters.Jump);
        }

        void OnJumpBegan()
        {
            if (_animator != null)
                _animator.SetBool(AnimatorParameters.Jumping, true);
        }

        void OnJumpEnded()
        {
            if (_animator != null)
                _animator.SetBool(AnimatorParameters.Jumping, false);
        }

        void OnPivotRequested(int pivotId)
        {
            // Nothing would ever complete the pivot, so complete it now, without turning.
            // LocomotionScript then turns normally instead.
            if (_pivotBehaviour == null)
            {
                _movementSourceManager?.CompletePivot(Quaternion.identity);
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

        void OnPivotStarted()
        {
            _pendingPivotId = null;
            _isPivotPlaying = true;
        }

        void OnPivotEnded()
        {
            // Cancelled: the movement source has stopped waiting for it.
            if (!_isPivotPlaying)
                return;

            _isPivotPlaying = false;
            _pendingPivotId = null;

            var rotationDelta = _animator.deltaRotation;
            _animator.applyRootMotion = false;

            _movementSourceManager?.CompletePivot(rotationDelta);
        }

        void OnFallBegan()
        {
            if (_animator != null)
                _animator.SetBool(AnimatorParameters.Falling, true);
        }

        void OnFallEnded()
        {
            if (_animator != null)
                _animator.SetBool(AnimatorParameters.Falling, false);
        }

        void OnLandingRequested(int landingId)
        {
            // Nothing would ever complete the landing, so complete it now.
            if (_landingBehaviour == null)
            {
                _movementSourceManager?.CompleteLanding();
                return;
            }

            _animator.SetInteger(AnimatorParameters.LandingId, landingId);
            _animator.SetTrigger(AnimatorParameters.Land);

            _pendingLandingId = landingId;
            _pendingLandingTime = 0f;
        }

        void OnLandingStarted()
        {
            _pendingLandingId = null;
            _isLandingPlaying = true;
        }

        void OnLandingEnded()
        {
            // Cancelled: the movement source has stopped waiting for it.
            if (!_isLandingPlaying)
                return;

            _isLandingPlaying = false;
            _pendingLandingId = null;

            _movementSourceManager?.CompleteLanding();
        }

        // The movement source no longer wants the pivot (e.g. the character can't navigate, or it timed out).
        // Undo what was set on the Animator for it, including root motion if the animation has started.
        void OnPivotCancelled()
        {
            if (_animator == null)
                return;

            _isPivotPlaying = false;
            AbandonPendingPivot();
        }

        void OnLandingCancelled()
        {
            if (_animator == null)
                return;

            _isLandingPlaying = false;
            AbandonPendingLanding();
        }

        // A deferred jump abandoned before its animation launched it.
        void OnJumpTriggerCancelled()
        {
            if (_animator != null)
                _animator.ResetTrigger(AnimatorParameters.Jump.Id);
        }

        // Another movement source took over. The old one has dropped its waits, and won't end a jump or fall
        // it was in the middle of, so drop everything set on the Animator for it, as if it had cancelled
        // them: a pivot or landing (started or not), a deferred jump's trigger, and the Jumping and Falling
        // flags. The new source sets what it needs. An animation already playing carries on, but without
        // root motion, and its end completes nothing.
        void OnActiveSourceChanged()
        {
            if (_animator == null)
                return;

            OnPivotCancelled();
            OnLandingCancelled();
            OnJumpTriggerCancelled();

            _animator.SetBool(AnimatorParameters.Jumping, false);
            _animator.SetBool(AnimatorParameters.Falling, false);
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

        void WarnNotStarted(HashSet<int> unstartedIds, int id, string kind)
        {
            if (!unstartedIds.Add(id))
                return;

            Debug.LogWarning(
                $"{kind} ID {id} didn't start within {_animationStartTimeout} s - skipping it. " +
                $"Check the Animator controller has a transition for {kind} ID {id}.",
                this);
        }
    }
}