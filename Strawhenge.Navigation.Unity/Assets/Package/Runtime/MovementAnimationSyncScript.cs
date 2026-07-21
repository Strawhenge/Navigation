using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public class MovementAnimationSyncScript : MonoBehaviour
    {
        static readonly int MoveX = Animator.StringToHash("Move X");
        static readonly int MoveY = Animator.StringToHash("Move Y");
        
        [SerializeField] Animator _animator;
        [SerializeField] LocomotionScript _locomotion;
        [SerializeField] float _dampeningTime = 0.1f;

        void LateUpdate()
        {
            var velocity = transform.root.InverseTransformDirection(_locomotion.CurrentVelocity);
            velocity.y = 0;

            _animator
                .SetFloat(
                    MoveX,
                    velocity.x,
                    _dampeningTime,
                    Time.deltaTime);
            
            _animator
                .SetFloat(
                    MoveY,
                    velocity.z,
                    _dampeningTime,
                    Time.deltaTime);
        }
    }
}