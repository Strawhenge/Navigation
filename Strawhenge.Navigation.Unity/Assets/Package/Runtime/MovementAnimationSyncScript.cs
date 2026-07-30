using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public class MovementAnimationSyncScript : MonoBehaviour
    {
        [SerializeField] Animator _animator;
        [SerializeField] LocomotionScript _locomotion;
        [SerializeField] float _dampeningTime = 0.1f;
        [SerializeField] float _minFallDistance = 1f;

        Transform _leftFoot;
        Transform _rightFoot;

        void Awake()
        {
            _locomotion.JumpTriggerRequested += OnJumpTriggerRequested;
            _locomotion.JumpBegan += OnJumpBegan;
            _locomotion.JumpEnded += OnJumpEnded;
            _locomotion.PivotRequested += OnPivotRequested;
            _locomotion.FallBegan += OnFallBegan;
            _locomotion.FallEnded += OnFallEnded;
            _locomotion.LandingRequested += OnLandingRequested;

            var pivotStateMachineBehavior = _animator.GetBehaviour<PivotStateMachineBehaviour>();
            pivotStateMachineBehavior.Ended += OnPivotEnded;

            var landingStateMachineBehavior = _animator.GetBehaviour<LandingStateMachineBehaviour>();
            landingStateMachineBehavior.Ended += OnLandingEnded;

            _leftFoot = _animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            _rightFoot = _animator.GetBoneTransform(HumanBodyBones.RightFoot);
        }

        public void OnJumpLaunch()
        {
            _locomotion.TriggerJump();
        }

        void LateUpdate()
        {
            var velocity = transform.root.InverseTransformDirection(_locomotion.CurrentVelocity);
            velocity.y = 0;

            if (_locomotion.Strafe)
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

            _locomotion.CompletePivot(rotationDelta);
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
            _locomotion.CompleteLanding();
        }
    }
}