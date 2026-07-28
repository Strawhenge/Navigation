using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public class MovementAnimationSyncScript : MonoBehaviour
    {
        static readonly int MoveSpeed = Animator.StringToHash("Move Speed");
        static readonly int Strafing = Animator.StringToHash("Strafing");
        static readonly int StrafeX = Animator.StringToHash("Strafe X");
        static readonly int StrafeY = Animator.StringToHash("Strafe Y");
        static readonly int Falling = Animator.StringToHash("Falling");
        static readonly int Jump = Animator.StringToHash("Jump");
        static readonly int Jumping = Animator.StringToHash("Jumping");
        static readonly int Pivot = Animator.StringToHash("Pivot");
        static readonly int PivotId = Animator.StringToHash("Pivot ID");
        static readonly int PivotRightFoot = Animator.StringToHash("Pivot Right Foot");

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

            var pivotStateMachineBehavior = _animator.GetBehaviour<PivotStateMachineBehaviour>();
            pivotStateMachineBehavior.Ended += OnPivotEnded;

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
                _animator.SetBool(Strafing, true);
                _animator.SetFloat(MoveSpeed, 0);

                _animator
                    .SetFloat(
                        StrafeX,
                        velocity.x,
                        _dampeningTime,
                        Time.deltaTime);

                _animator
                    .SetFloat(
                        StrafeY,
                        velocity.z,
                        _dampeningTime,
                        Time.deltaTime);
                return;
            }

            _animator.SetBool(Strafing, false);
            _animator.SetFloat(StrafeX, 0);
            _animator.SetFloat(StrafeY, 0);

            _animator.SetFloat(
                MoveSpeed,
                velocity.magnitude,
                _dampeningTime,
                Time.deltaTime);
        }

        void OnJumpTriggerRequested()
        {
            _animator.SetTrigger(Jump);
        }

        void OnJumpBegan()
        {
            _animator.SetBool(Jumping, true);
        }

        void OnJumpEnded()
        {
            _animator.SetBool(Jumping, false);
        }

        void OnPivotRequested(int pivotId)
        {
            _animator.SetInteger(PivotId, pivotId);
            
            var leftOffset = _leftFoot.position - transform.root.position;
            var rightOffset = _rightFoot.position - transform.root.position;

            var leftForward = Vector3.Dot(leftOffset, transform.root.forward);
            var rightForward = Vector3.Dot(rightOffset, transform.root.forward);

            _animator.SetBool(PivotRightFoot, rightForward > leftForward);
            
            _animator.applyRootMotion = true;
            _animator.SetTrigger(Pivot);
        }

        void OnPivotEnded()
        {
            var rotationDelta = _animator.deltaRotation;
            _animator.applyRootMotion = false;

            _locomotion.CompletePivot(rotationDelta);
        }

        void OnFallBegan()
        {
            _animator.SetBool(Falling, true);
        }

        void OnFallEnded()
        {
            _animator.SetBool(Falling, false);
        }
    }
}