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

            if (_input.sqrMagnitude > 0.001f && _characterController.isGrounded)
                _targetRotation = Quaternion.LookRotation(input);
        }

        public void Jump()
        {
            // TODO
            Debug.Log("Jump");
        }

        void Update()
        {
            HandleRotation();
            HandleMovement();
        }

        void HandleRotation()
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                _targetRotation,
                _turnSpeed * Time.deltaTime);
        }

        void HandleMovement()
        {
            var targetSpeed = GetTargetSpeed();
            var acceleration = GetAcceleration(targetSpeed);
            var speed = GetSpeed(targetSpeed, acceleration);
            var verticalSpeed = GetVerticalSpeed();
            var velocity = GetVelocity(speed, verticalSpeed);

            _lastSpeed = speed;
            _lastVerticalSpeed = verticalSpeed;
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

        Vector3 GetVelocity(float speed, float verticalSpeed)
        {
            var velocity = (_input.magnitude > 0.1f ? _input : transform.forward) * speed;
            velocity.y = verticalSpeed;
            return velocity;
        }

        float GetSpeed(float targetSpeed, float acceleration)
        {
            var speed = Mathf.MoveTowards(
                _lastSpeed,
                targetSpeed,
                acceleration * Time.deltaTime);

            var directionChangeAngle = Vector3.SignedAngle(
                _characterController.velocity.normalized,
                _input.normalized,
                Vector3.up
            );

            if (Mathf.Abs(directionChangeAngle) >= 120f)
                speed = Mathf.Min(speed, _walkSpeed);
            else if (Mathf.Abs(directionChangeAngle) >= 40f)
                speed = Mathf.Min(speed, _runSpeed);

            return speed;
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
            if (!_characterController.isGrounded)
                return _lastVerticalSpeed + _gravity * Time.deltaTime;

            return _lastVerticalSpeed < 0f
                ? _groundedGravity
                : _lastVerticalSpeed;
        }
    }
}