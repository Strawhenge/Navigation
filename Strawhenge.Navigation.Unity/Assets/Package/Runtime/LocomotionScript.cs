using System;
using System.Linq;
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

        bool _isAwaitingJumpTrigger;
        bool _jump;
        bool _isJumping;

        Quaternion _targetRotation;

        bool _isFalling;
        float _fallTime;
        float _fallDistance;
        float _groundedY;

        float _turnAngle;
        bool _isPivoting;
        bool _isAwaitingStationaryPivot;
        SerializedPivot _stationaryPivot;

        public event Action JumpTriggerRequested;

        public event Action JumpBegan;

        public event Action JumpEnded;

        public event Action<int> PivotRequested;

        public event Action FallBegan;
        
        public event Action FallEnded;

        public Vector3 CurrentVelocity => _characterController.velocity;

        public bool Walk { get; set; }

        public bool Sprint { get; set; }

        public bool Strafe { get; set; }

        public void Move(Vector3 input)
        {
            input.y = 0;

            if (input.sqrMagnitude > 1f)
                input.Normalize();

            if (input.sqrMagnitude > 0.001f)
                _targetRotation = Quaternion.LookRotation(input);

            if (_isAwaitingStationaryPivot)
            {
                if (_input.magnitude < 0.001f)
                {
                    _isAwaitingStationaryPivot = false;
                    Pivot(_stationaryPivot.Id);
                    _stationaryPivot = null;
                }
                else if (input.magnitude > _input.magnitude)
                {
                    _isAwaitingStationaryPivot = false;
                    _stationaryPivot = null;
                }
            }
            
            _input = input;
        }

        public void Jump()
        {
            if (_isJumping || _fallTime > _settings.CoyoteTime)
                return;

            _isAwaitingJumpTrigger = true;

            if (_settings.DeferJumpTrigger)
                JumpTriggerRequested?.Invoke();
            else
                TriggerJump();
        }

        public void TriggerJump()
        {
            if (!_isAwaitingJumpTrigger) return;
            _isAwaitingJumpTrigger = false;

            _jump = true;
        }

        public void CompletePivot(Quaternion rotationDelta)
        {
            if (!_isPivoting) return;

            transform.root.rotation = rotationDelta * transform.root.rotation;
            _isPivoting = false;
        }

        void Update()
        {
            HandleFalling();
            HandleRotation();
            HandleMovement();
        }

        void HandleRotation()
        {
            if (_isPivoting) return;

            if (_horizontalSpeed >= 1f)
            {
                _isAwaitingStationaryPivot = false;
                _stationaryPivot = null;
            }

            if (Strafe)
            {
                _turnAngle = 0;
                _targetRotation = transform.rotation;
                return;
            }

            _turnAngle = Vector3.SignedAngle(
                transform.root.forward,
                _input.normalized,
                Vector3.up
            );

            if (CheckForStationaryPivots(out var stationaryPivot))
            {
                _isAwaitingStationaryPivot = true;
                _stationaryPivot = stationaryPivot;
            }

            if (CheckForMovingPivots(out var movingPivot))
            {
                Pivot(movingPivot.Id);
                return;
            }

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                _targetRotation,
                _settings.TurnSpeed * Time.deltaTime);
        }

        bool CheckForStationaryPivots(out SerializedPivot matchingPivot)
        {
            foreach (var pivot in _settings.StationaryPivots)
            {
                if (pivot.SpeedRange.IsInRange(_horizontalSpeed) &&
                    pivot.AngleRanges.Any(angleRange => angleRange.IsInRange(_turnAngle)))
                {
                    matchingPivot = pivot;
                    return true;
                }
            }

            matchingPivot = null;
            return false;
        }

        bool CheckForMovingPivots(out SerializedPivot matchingPivot)
        {
            if (_isAwaitingStationaryPivot)
            {
                matchingPivot = null;
                return false;
            }

            foreach (var pivot in _settings.MovingPivots)
            {
                if (pivot.SpeedRange.IsInRange(_horizontalSpeed) &&
                    pivot.AngleRanges.Any(angleRange => angleRange.IsInRange(_turnAngle)))
                {
                    matchingPivot = pivot;
                    return true;
                }
            }

            matchingPivot = null;
            return false;
        }

        void HandleMovement()
        {
            if (_isPivoting) return;

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

            var isFalling = !_characterController.isGrounded && _fallDistance >= _settings.FallDistance;
            if (isFalling != _isFalling)
            {
                _isFalling = isFalling;
                if (_isFalling)
                    FallBegan?.Invoke();
                else
                    FallEnded?.Invoke();
            }

            if (_isFalling)
                _fallTime += Time.deltaTime;
            else
                _fallTime = 0f;
        }

        void CalculateHorizontalSpeed()
        {
            if (_isAwaitingJumpTrigger)
                return;

            var targetSpeed = GetTargetSpeed();
            var acceleration = GetAcceleration(targetSpeed);

            var speed = Mathf.MoveTowards(
                _horizontalSpeed,
                targetSpeed,
                acceleration * Time.deltaTime);

            if (Mathf.Abs(_turnAngle) >= 120f)
                speed = Mathf.Min(speed, _settings.WalkSpeed);
            else if (Mathf.Abs(_turnAngle) >= 40f)
                speed = Mathf.Min(speed, _settings.RunSpeed);

            _horizontalSpeed = speed;
        }

        void CalculateVerticalSpeed()
        {
            if (_jump)
            {
                _jump = false;
                _isJumping = true;
                _verticalSpeed = Mathf.Sqrt(_settings.JumpHeight * -2f * _settings.Gravity);
                JumpBegan?.Invoke();
                return;
            }

            if (_characterController.isGrounded && _verticalSpeed <= 0f)
            {
                _isJumping = false;
                JumpEnded?.Invoke();
            }

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

        void Pivot(int pivotId)
        {
            _isPivoting = true;
            PivotRequested?.Invoke(pivotId);
        }
    }
}