using Strawhenge.Navigation.Unity;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Development
{
    public class ControlsScript : MonoBehaviour
    {
        [SerializeField] LocomotionScript _player;
        [SerializeField] Camera _camera;

        void Awake()
        {
            if (_player == null)
                _player = FindObjectOfType<LocomotionScript>();

            if (_camera == null)
                _camera = Camera.main ?? FindObjectOfType<Camera>();
        }

        public void Move(InputAction.CallbackContext context)
        {
            var input = context.ReadValue<Vector2>();

            var cameraForward = _camera.transform.forward;
            var cameraRight = _camera.transform.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;

            cameraForward.Normalize();
            cameraRight.Normalize();

            var moveDirection =
                cameraForward * input.y +
                cameraRight * input.x;

            if (moveDirection.sqrMagnitude > 1f)
                moveDirection.Normalize();

            _player.Move(moveDirection);
        }

        public void Walk(InputAction.CallbackContext context)
        {
            if (context.performed)
                _player.Walk = !_player.Walk;
        }

        public void Sprint(InputAction.CallbackContext context)
        {
            if (context.started)
                _player.Sprint = true;
            else if (context.canceled)
                _player.Sprint = false;
        }
    }
}