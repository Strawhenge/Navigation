using Strawhenge.Navigation.Unity.Destination;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public class MovementAnimationSyncScript : MonoBehaviour
    {
        [SerializeField] Animator _animator;
        [SerializeField] LocomotionScript _locomotion;
        [SerializeField] DestinationScript _destination;
        [SerializeField] float _dampeningTime = 0.1f;
        [SerializeField] float _minFallDistance = 1f;

        IMovementSource _movementSource;

        Transform _leftFoot;
        Transform _rightFoot;

        void Awake()
        {
            SetMovementSource(_locomotion);

            var pivotStateMachineBehavior = _animator.GetBehaviour<PivotStateMachineBehaviour>();
            pivotStateMachineBehavior.Ended += OnPivotEnded;

            var landingStateMachineBehavior = _animator.GetBehaviour<LandingStateMachineBehaviour>();
            landingStateMachineBehavior.Ended += OnLandingEnded;

            _leftFoot = _animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            _rightFoot = _animator.GetBoneTransform(HumanBodyBones.RightFoot);
        }

        void OnDestroy()
        {
            UnsubscribeFromMovementSource(_movementSource);
        }

        [ContextMenu(nameof(SetLocomotionSource))]
        public void SetLocomotionSource() =>
            SetMovementSource(_locomotion);

        [ContextMenu(nameof(SetDestinationSource))]
        public void SetDestinationSource() =>
            SetMovementSource(new DestinationMovementSource(_destination.DestinationController));

        void SetMovementSource(IMovementSource movementSource)
        {
            var nextSource = movementSource ?? _locomotion;
            if (ReferenceEquals(_movementSource, nextSource))
                return;

            UnsubscribeFromMovementSource(_movementSource);
            _movementSource = nextSource;
            SubscribeToMovementSource(_movementSource);
        }

        void SubscribeToMovementSource(IMovementSource movementSource)
        {
            if (movementSource == null)
                return;

            movementSource.JumpTriggerRequested += OnJumpTriggerRequested;
            movementSource.JumpBegan += OnJumpBegan;
            movementSource.JumpEnded += OnJumpEnded;
            movementSource.PivotRequested += OnPivotRequested;
            movementSource.FallBegan += OnFallBegan;
            movementSource.FallEnded += OnFallEnded;
            movementSource.LandingRequested += OnLandingRequested;
        }

        void UnsubscribeFromMovementSource(IMovementSource movementSource)
        {
            if (movementSource == null)
                return;

            movementSource.JumpTriggerRequested -= OnJumpTriggerRequested;
            movementSource.JumpBegan -= OnJumpBegan;
            movementSource.JumpEnded -= OnJumpEnded;
            movementSource.PivotRequested -= OnPivotRequested;
            movementSource.FallBegan -= OnFallBegan;
            movementSource.FallEnded -= OnFallEnded;
            movementSource.LandingRequested -= OnLandingRequested;
        }

        public void OnJumpLaunch()
        {
            _movementSource?.TriggerJump();
        }

        void LateUpdate()
        {
            if (_movementSource == null)
                return;

            var velocity = transform.root.InverseTransformDirection(_movementSource.CurrentVelocity);
            velocity.y = 0;

            if (_movementSource.Strafe)
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

        void OnJumpTriggerRequested()
        {
            _animator.SetTrigger(AnimatorParameters.Jump);
        }

        void OnJumpBegan()
        {
            _animator.SetBool(AnimatorParameters.Jumping, true);
        }

        void OnJumpEnded()
        {
            _animator.SetBool(AnimatorParameters.Jumping, false);
        }

        void OnPivotRequested(int pivotId)
        {
            _animator.SetInteger(AnimatorParameters.PivotId, pivotId);

            var leftOffset = _leftFoot.position - transform.root.position;
            var rightOffset = _rightFoot.position - transform.root.position;

            var leftForward = Vector3.Dot(leftOffset, transform.root.forward);
            var rightForward = Vector3.Dot(rightOffset, transform.root.forward);

            _animator.SetBool(AnimatorParameters.PivotRightFoot, rightForward > leftForward);

            _animator.applyRootMotion = true;
            _animator.SetTrigger(AnimatorParameters.Pivot);
        }

        void OnPivotEnded()
        {
            var rotationDelta = _animator.deltaRotation;
            _animator.applyRootMotion = false;

            _movementSource?.CompletePivot(rotationDelta);
        }

        void OnFallBegan()
        {
            _animator.SetBool(AnimatorParameters.Falling, true);
        }

        void OnFallEnded()
        {
            _animator.SetBool(AnimatorParameters.Falling, false);
        }

        void OnLandingRequested(int landingId)
        {
            _animator.SetInteger(AnimatorParameters.LandingId, landingId);
            _animator.SetTrigger(AnimatorParameters.Land);
        }

        void OnLandingEnded()
        {
            _movementSource?.CompleteLanding();
        }
    }
}