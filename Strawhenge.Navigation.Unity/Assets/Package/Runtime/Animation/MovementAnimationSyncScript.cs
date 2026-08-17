using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public class MovementAnimationSyncScript : MonoBehaviour
    {
        [SerializeField] Animator _animator;
        [SerializeField] MovementSourceManagerScript _movementSourceManager;
        [SerializeField] float _dampeningTime = 0.1f;
        [SerializeField] float _minFallDistance = 1f;

        Transform _leftFoot;
        Transform _rightFoot;

        void Awake()
        {
            var pivotStateMachineBehavior = _animator.GetBehaviour<PivotStateMachineBehaviour>();
            pivotStateMachineBehavior.Ended += OnPivotEnded;

            var landingStateMachineBehavior = _animator.GetBehaviour<LandingStateMachineBehaviour>();
            landingStateMachineBehavior.Ended += OnLandingEnded;

            _leftFoot = _animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            _rightFoot = _animator.GetBoneTransform(HumanBodyBones.RightFoot);

            SubscribeToMovement();
        }

        void OnDestroy()
        {
            UnsubscribeFromMovement();
        }

        public void OnJumpLaunch()
        {
            _movementSourceManager?.TriggerJump();
        }

        void LateUpdate()
        {
            if (_movementSourceManager == null)
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

            _movementSourceManager?.CompletePivot(rotationDelta);
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
            _movementSourceManager?.CompleteLanding();
        }
    }
}