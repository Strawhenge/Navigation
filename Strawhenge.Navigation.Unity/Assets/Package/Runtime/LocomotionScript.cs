using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public class LocomotionScript : MonoBehaviour
    {
        [SerializeField] CharacterController _characterController;

        [SerializeField, Header("Speed")] float _walkSpeed = 1f;
        [SerializeField] float _runSpeed = 5f;
        [SerializeField] float _sprintSpeed = 8f;

        [SerializeField, Header("Acceleration")]
        float _acceleration = 10f;

        [SerializeField] float _deceleration = 30f;

        [SerializeField, Header("Turning")] float _turnSpeed = 360f;

        [SerializeField, Header("Jumping")] float _jumpHeight = 1.5f;
        [SerializeField] float _coyoteTime = 0.2f;

        [SerializeField, Header("Gravity")] float _gravity = -9.81f;
        [SerializeField] float _groundedGravity = -2f;

        Vector3 _input;
        bool _jumpInput;

        float _horizontalSpeed;
        float _verticalSpeed;
        bool _isJumping;

        Quaternion _targetRotation;

        float _fallTime;
        float _fallDistance;
        float _groundedY;

        public Vector3 CurrentVelocity => _characterController.velocity;

        public float FallDistance => _fallDistance;

        public bool Walk { get; set; }

        public bool Sprint { get; set; }

        public bool Strafe { get; set; }

        public void Move(Vector3 input)
        {
            input.y = 0;

            if (input.sqrMagnitude > 1f)
                input.Normalize();

            _input = input;

            if (_input.sqrMagnitude > 0.001f)
                _targetRotation = Quaternion.LookRotation(input);
        }

        public void Jump()
        {
            if (!_isJumping && _fallTime <= _coyoteTime)
                _jumpInput = true;
        }

        void Update()
        {
            HandleFalling();
            HandleRotation();
            HandleMovement();
        }

        void HandleRotation()
        {
            if (!Strafe)
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    _targetRotation,
                    _turnSpeed * Time.deltaTime);
        }

        void HandleMovement()
        {
            CalculateHorizontalSpeed();
            CalculateVerticalSpeed();

            var velocity = GetVelocity();
            var collisionFlags = _characterController.Move(velocity * Time.deltaTime);

            ManageCollisions(collisionFlags);
        }

        void HandleFalling()
        {
            if (_characterController.isGrounded)
                _groundedY = transform.position.y;

            _fallDistance = Mathf.Max(0f, _groundedY - transform.position.y);

            if (_characterController.isGrounded || _fallDistance <= 0)
                _fallTime = 0f;
            else
                _fallTime += Time.deltaTime;
        }

        void CalculateHorizontalSpeed()
        {
            var targetSpeed = GetTargetSpeed();
            var acceleration = GetAcceleration(targetSpeed);

            var speed = Mathf.MoveTowards(
                _horizontalSpeed,
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

            _horizontalSpeed = speed;
        }

        void CalculateVerticalSpeed()
        {
            if (_jumpInput)
            {
                _jumpInput = false;
                _isJumping = true;
                _verticalSpeed = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
                return;
            }

            if (_characterController.isGrounded)
                _isJumping = false;

            if (!_characterController.isGrounded)
            {
                _verticalSpeed += _gravity * Time.deltaTime;
                return;
            }

            _verticalSpeed = _verticalSpeed < 0f
                ? _groundedGravity
                : _verticalSpeed;
        }

        void ManageCollisions(CollisionFlags collisionFlags)
        {
            var blocked =
                (collisionFlags & CollisionFlags.Sides) != 0 &&
                _input.sqrMagnitude > 0.001f;

            if (blocked)
            {
                _horizontalSpeed = Mathf.Min(
                    _horizontalSpeed,
                    _characterController.velocity.magnitude);
            }
        }

        Vector3 GetVelocity()
        {
            var velocity = (_input.magnitude > 0.1f ? _input : transform.forward) * _horizontalSpeed;
            velocity.y = _verticalSpeed;
            return velocity;
        }

        float GetTargetSpeed()
        {
            if (_input.sqrMagnitude < 0.001f)
                return 0f;
            if (Sprint)
                return Strafe ? _runSpeed : _sprintSpeed;
            if (Walk)
                return _walkSpeed;
            return _runSpeed;
        }

        float GetAcceleration(float targetSpeed)
        {
            return targetSpeed > _horizontalSpeed
                ? _acceleration
                : _deceleration;
        }
    }
}