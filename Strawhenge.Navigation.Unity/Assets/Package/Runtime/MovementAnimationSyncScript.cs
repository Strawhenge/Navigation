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

        [SerializeField] Animator _animator;
        [SerializeField] LocomotionScript _locomotion;
        [SerializeField] float _dampeningTime = 0.1f;
        [SerializeField] float _minFallDistance = 1f;

        void Awake()
        {
            _locomotion.JumpTriggerRequested += OnJumpTriggerRequested;
            _locomotion.JumpBegan += OnJumpBegan;
            _locomotion.JumpEnded += OnJumpEnded;
        }

        public void OnJumpLaunch()
        {
            _locomotion.TriggerJump();
        }

        void LateUpdate()
        {
            _animator.SetBool(Falling, _locomotion.FallDistance >= _minFallDistance);

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
    }
}