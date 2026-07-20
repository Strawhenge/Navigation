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
            _animator
                .SetFloat(
                    MoveSpeed,
                    _locomotion.CurrentVelocity.magnitude,
                    _dampeningTime,
                    Time.deltaTime);
        }
    }
}