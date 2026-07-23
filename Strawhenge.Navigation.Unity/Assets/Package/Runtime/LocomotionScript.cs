using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public class LocomotionScript : MonoBehaviour
    {
        [SerializeField] CharacterController _characterController;
        [SerializeField] SerializedLocomotionSettings _settings;

        Vector3 _input;

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
            if (!_isJumping && _fallTime <= _settings.CoyoteTime)
            {
                _isJumping = true;
                _verticalSpeed = Mathf.Sqrt(_settings.JumpHeight * -2f * _settings.Gravity);
            }
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
                    _settings.TurnSpeed * Time.deltaTime);
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

            if (_characterController.isGrounded || _fallDistance < _settings.FallDistance)
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
                speed = Mathf.Min(speed, _settings.WalkSpeed);
            else if (Mathf.Abs(directionChangeAngle) >= 40f)
                speed = Mathf.Min(speed, _settings.RunSpeed);

            _horizontalSpeed = speed;
        }

        void CalculateVerticalSpeed()
        {
            if (_characterController.isGrounded)
                _isJumping = false;

            if (!_characterController.isGrounded)
            {
                _verticalSpeed += _settings.Gravity * Time.deltaTime;
                return;
            }

            _verticalSpeed = _verticalSpeed < 0f
                ? _settings.GroundedGravity
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
                return Strafe ? _settings.RunSpeed : _settings.SprintSpeed;
            if (Walk)
                return _settings.WalkSpeed;
            return _settings.RunSpeed;
        }

        float GetAcceleration(float targetSpeed)
        {
            return targetSpeed > _horizontalSpeed
                ? _settings.Acceleration
                : _settings.Deceleration;
        }
    }
}