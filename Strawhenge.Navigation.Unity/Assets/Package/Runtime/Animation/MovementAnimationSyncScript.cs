using UnityEngine;
using Strawhenge.Common.Unity;
using Strawhenge.Common.Unity.Helpers;
using System.Collections.Generic;

namespace Strawhenge.Navigation.Unity
{
    public class MovementAnimationSyncScript : MonoBehaviour
    {
        [SerializeField] Animator _animator;
        [SerializeField] MovementSourceManagerScript _movementSourceManager;
        [SerializeField] float _dampeningTime = 0.1f;
        [SerializeField] float _minFallDistance = 1f;

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

            var velocity = transform.root.InverseTransformDirection(_movementSourceManager.CurrentVelocity);
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
                var leftOffset = _leftFoot.position - transform.root.position;
                var rightOffset = _rightFoot.position - transform.root.position;

                var leftForward = Vector3.Dot(leftOffset, transform.root.forward);
                var rightForward = Vector3.Dot(rightOffset, transform.root.forward);

                _animator.SetBool(AnimatorParameters.PivotRightFoot, rightForward > leftForward);
            }

            _animator.applyRootMotion = true;
            _animator.SetTrigger(AnimatorParameters.Pivot);

            _pendingPivotId = pivotId;
            _pendingPivotTime = 0f;
        }

        void OnPivotStarted() => _pendingPivotId = null;

        void OnPivotEnded()
        {
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

        void OnLandingStarted() => _pendingLandingId = null;

        void OnLandingEnded()
        {
            _pendingLandingId = null;

            _movementSourceManager?.CompleteLanding();
        }

        // Another movement source took over: its wait was already cancelled, so just undo what was set
        // on the Animator for an animation that hasn't started.
        void OnActiveSourceChanged()
        {
            if (_pendingPivotId.HasValue)
                AbandonPendingPivot();

            if (_pendingLandingId.HasValue)
                AbandonPendingLanding();
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