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
                _player.Walk = !_player.Walk;
        }

        public void Sprint(InputAction.CallbackContext context)
        {
            if (context.started)
                _player.Sprint = true;
            else if (context.canceled)
                _player.Sprint = false;
        }

        public void Jump(InputAction.CallbackContext context)
        {
            if (context.performed)
                _player.Jump();
        }

        public void Strafe(InputAction.CallbackContext context)
        {
            if (context.performed)
                _player.Strafe = !_player.Strafe;
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

            _player.Move(moveDirection);
        }
    }
}