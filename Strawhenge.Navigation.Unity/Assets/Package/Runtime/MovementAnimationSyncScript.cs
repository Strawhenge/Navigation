using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public class MovementAnimationSyncScript : MonoBehaviour
    {
        static readonly int MoveSpeed = Animator.StringToHash("Move Speed");
        static readonly int StrafeX = Animator.StringToHash("Strafe X");
        static readonly int StrafeY = Animator.StringToHash("Strafe Y");

        [SerializeField] Animator _animator;
        [SerializeField] LocomotionScript _locomotion;
        [SerializeField] float _dampeningTime = 0.1f;

        void LateUpdate()
        {
            var velocity = transform.root.InverseTransformDirection(_locomotion.CurrentVelocity);
            velocity.y = 0;

            if (_locomotion.Strafe)
            {
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

            _animator.SetFloat(StrafeX, 0);
            _animator.SetFloat(StrafeY, 0);
            
            _animator.SetFloat(
                MoveSpeed,
                velocity.magnitude,
                _dampeningTime,
                Time.deltaTime);
        }
    }
}