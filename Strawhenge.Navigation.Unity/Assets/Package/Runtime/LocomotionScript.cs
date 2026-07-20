using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public class LocomotionScript : MonoBehaviour
    {
        [SerializeField] CharacterController _characterController;
        [SerializeField] float _walkSpeed = 1f;
        [SerializeField] float _runSpeed = 5f;
        [SerializeField] float _sprintSpeed = 8f;

        [SerializeField] float _acceleration = 10f;
        [SerializeField] float _deceleration = 30f;

        [SerializeField] float _turnSpeed = 360f;

        [SerializeField] float _gravity = -9.81f;
        [SerializeField] float _groundedGravity = -2f;

        Vector3 _input;
        float _lastSpeed;
        float _lastVerticalSpeed;
        Quaternion _targetRotation;

        public Vector3 CurrentVelocity => _characterController.velocity;

        public bool Walk { get; set; }

        public bool Sprint { get; set; }

        public void Move(Vector3 input)
        {
            input.y = 0;

            if (input.sqrMagnitude > 1f)
                input.Normalize();

            _input = input;

            if (_input.sqrMagnitude > 0.001f)
                _targetRotation = Quaternion.LookRotation(input);
        }

        void Update()
        {
            HandleRotation();
            HandleMovement();
        }

        void HandleRotation()
        {
            if (_characterController.isGrounded)
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    _targetRotation,
                    _turnSpeed * Time.deltaTime);
        }

        void HandleMovement()
        {
            var targetSpeed = GetTargetSpeed();
            var acceleration = GetAcceleration(targetSpeed);

            var speed = Mathf.MoveTowards(
                _lastSpeed,
                targetSpeed,
                acceleration * Time.deltaTime);

            var angle = Vector3.SignedAngle(
                _characterController.velocity.normalized,
                _input.normalized,
                Vector3.up
            );
           
            if (Mathf.Abs(angle) >= 120f)
            {
                speed = Mathf.Min(speed, _walkSpeed);
            }
            else if (Mathf.Abs(angle) >= 40f)
            {
                speed = Mathf.Min(speed, _runSpeed);
            }

            var verticalSpeed = GetVerticalSpeed();

            var direction = _input.magnitude > 0.1f ? _input : transform.forward;
            var velocity = direction * speed;
            velocity.y = verticalSpeed;
            _lastSpeed = speed;
            var collisionFlags = _characterController.Move(velocity * Time.deltaTime);

            var blocked =
                (collisionFlags & CollisionFlags.Sides) != 0 &&
                _input.sqrMagnitude > 0.001f;

            if (blocked)
            {
                _lastSpeed = Mathf.Min(
                    _lastSpeed,
                    _characterController.velocity.magnitude);
            }
        }

        float GetTargetSpeed()
        {
            if (_input.sqrMagnitude < 0.001f)
                return 0f;
            if (Sprint)
                return _sprintSpeed;
            if (Walk)
                return _walkSpeed;
            return _runSpeed;
        }

        float GetAcceleration(float targetSpeed)
        {
            return targetSpeed > _lastSpeed
                ? _acceleration
                : _deceleration;
        }

        float GetVerticalSpeed()
        {
            if (_characterController.isGrounded)
            {
                if (_lastVerticalSpeed < 0f)
                    _lastVerticalSpeed = _groundedGravity;
            }
            else
            {
                _lastVerticalSpeed += _gravity * Time.deltaTime;
            }

            return _lastVerticalSpeed;
        }
    }
}