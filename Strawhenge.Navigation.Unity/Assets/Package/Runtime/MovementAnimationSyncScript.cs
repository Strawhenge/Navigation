using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public class MovementAnimationSyncScript : MonoBehaviour
    {
        static readonly int MoveSpeed = Animator.StringToHash("Move Speed");
        [SerializeField] Animator _animator;
        [SerializeField] LocomotionScript _locomotion;
        [SerializeField] float _dampeningTime = 0.1f;

        void LateUpdate()
        {
            var velocity = _locomotion.CurrentVelocity;
            velocity.y = 0;

            _animator
                .SetFloat(
                    MoveSpeed,
                    velocity.magnitude,
                    _dampeningTime,
                    Time.deltaTime);
        }
    }
}