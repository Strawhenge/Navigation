using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public class LocomotionScript : MonoBehaviour
    {
        [SerializeField] CharacterController _characterController;

        Vector3 _direction;

        public void Move(Vector3 direction)
        {
            _direction = direction;
        }

        void Update()
        {
            var velocity = _direction * 5;
            _characterController.Move(velocity * Time.deltaTime);
        }
    }
}