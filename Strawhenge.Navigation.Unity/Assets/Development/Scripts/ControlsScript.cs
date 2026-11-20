using Strawhenge.Navigation.Unity;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Development
{
    public class ControlsScript : MonoBehaviour
    {
        [SerializeField] LocomotionScript _player;
        [SerializeField] Camera _camera;

        Vector2 _moveInput;
        
        void Awake()
        {
            if (_player == null)
                _player = FindObjectOfType<LocomotionScript>();

            if (_camera == null)
                _camera = Camera.main ?? FindObjectOfType<Camera>();
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
                _player.LocomotionController.Sprint = true;
            else if (context.canceled)
                _player.LocomotionController.Sprint = false;
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