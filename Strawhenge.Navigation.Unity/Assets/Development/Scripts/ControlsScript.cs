using Strawhenge.Navigation.Unity;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Development
{
    public class ControlsScript : MonoBehaviour
    {
        [SerializeField] LocomotionScript _player;
        [SerializeField] Camera _camera;

        Vector2 _moveInput;
        bool _sprintHeld;
        
        void Awake()
        {
            if (_player == null)
                _player = FindObjectOfType<LocomotionScript>();

            // Unity's == null, not ??, which would miss a destroyed camera.
            if (_camera == null)
                _camera = Camera.main;

            if (_camera == null)
                _camera = FindObjectOfType<Camera>();
        }

        // Releases the previous character, so it doesn't carry on with the last input, and hands the held
        // sprint over to the new one. Walk and Strafe are toggles, so each character keeps its own.
        public void SetPlayer(LocomotionScript player)
        {
            if (_player != null && _player != player)
            {
                _player.LocomotionController.Move(Vector3.zero);
                _player.LocomotionController.Sprint = false;
            }

            _player = player;

            if (_player != null)
                _player.LocomotionController.Sprint = _sprintHeld;
        }

        public void Move(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
        }

        public void Walk(InputAction.CallbackContext context)
        {
            if (context.performed)
                _player.LocomotionController.Walk = !_player.LocomotionController.Walk;
        }

        public void Sprint(InputAction.CallbackContext context)
        {
            if (context.started)
                _sprintHeld = true;
            else if (context.canceled)
                _sprintHeld = false;
            else
                return;

            _player.LocomotionController.Sprint = _sprintHeld;
        }

        public void Jump(InputAction.CallbackContext context)
        {
            if (context.performed)
                _player.LocomotionController.Jump();
        }

        public void Strafe(InputAction.CallbackContext context)
        {
            if (context.performed)
                _player.LocomotionController.Strafe = !_player.LocomotionController.Strafe;
        }

        void Update()
        {
            var cameraForward = _camera.transform.forward;
            var cameraRight = _camera.transform.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;

            cameraForward.Normalize();
            cameraRight.Normalize();

            var moveDirection =
                cameraForward * _moveInput.y +
                cameraRight * _moveInput.x;

            _player.LocomotionController.Move(moveDirection);
        }
    }
}