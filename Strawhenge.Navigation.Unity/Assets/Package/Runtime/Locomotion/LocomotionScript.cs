using Strawhenge.Common.Unity.Helpers;
using Strawhenge.Common.Unity.Serialization;
using System;
using System.Linq;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public sealed class LocomotionScript : MonoBehaviour
    {
        [SerializeField] CharacterController _characterController;

        [SerializeField] SerializedSource<
            ILocomotionSettings,
            SerializedLocomotionSettings,
            LocomotionSettingsScriptableObject> _settings;

        ILocomotionSettings _locomotionSettings;
        Transform _rootTransform;

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
        bool _isLanding;

        float _turnAngle;
        bool _isPivoting;
        bool _isAwaitingStationaryPivot;
        SerializedPivot _stationaryPivot;

        void Awake()
        {
            ComponentRefHelper
                .EnsureHierarchyComponent(ref _characterController, nameof(_characterController), this);

            if (!_settings.TryGetValue(out _locomotionSettings))
            {
                Debug.LogWarning($"'{nameof(_settings)} not set - using defaults.");
                _locomotionSettings = DefaultLocomotionSettings.Instance;
            }

            _rootTransform = _characterController.transform;
        }

        void Update()
        {
            HandleFalling();
            HandleRotation();
            HandleMovement();
        }

        void OnEnable()
        {
            _characterController.enabled = true;
            IsActiveChanged?.Invoke(true);
        }

        void OnDisable()
        {
            _characterController.enabled = false;
            IsActiveChanged?.Invoke(false);
        }

        public event Action JumpTriggerRequested;

        public event Action JumpBegan;

        public event Action JumpEnded;

        public event Action<int> PivotRequested;

        public event Action FallBegan;

        public event Action FallEnded;

        public event Action<int> LandingRequested;

        public event Action<bool> IsActiveChanged;

        public Vector3 CurrentVelocity => _characterController.velocity;

        public bool IsActive => isActiveAndEnabled;

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
            if (_isJumping || _isPivoting || _isLanding || _fallTime > _locomotionSettings.CoyoteTime)
                return;

            _isAwaitingJumpTrigger = true;

            if (_locomotionSettings.DeferJumpTrigger)
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

            _rootTransform.rotation = rotationDelta * _rootTransform.rotation;
            _isPivoting = false;
        }

        public void CompleteLanding()
        {
            if (!_isLanding) return;
            _isLanding = false;
        }

        void HandleRotation()
        {
            if (_isPivoting || _isLanding) return;

            if (_horizontalSpeed >= 1f)
            {
                _isAwaitingStationaryPivot = false;
                _stationaryPivot = null;
            }

            if (Strafe)
            {
                _turnAngle = 0;
                _targetRotation = _rootTransform.rotation;
                return;
            }

            _turnAngle = Vector3.SignedAngle(
                _rootTransform.forward,
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

            _rootTransform.rotation = Quaternion.RotateTowards(
                _rootTransform.rotation,
                _targetRotation,
                _locomotionSettings.TurnSpeed * Time.deltaTime);
        }

        bool CheckForStationaryPivots(out SerializedPivot matchingPivot)
        {
            foreach (var pivot in _locomotionSettings.StationaryPivots)
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

            foreach (var pivot in _locomotionSettings.MovingPivots)
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
                _groundedY = _rootTransform.position.y;

            var fallDistance = _fallDistance;
            _fallDistance = Mathf.Max(0f, _groundedY - _rootTransform.position.y);

            var isFalling = !_characterController.isGrounded && _fallDistance >= _locomotionSettings.FallDistance;
            if (isFalling != _isFalling)
            {
                _isFalling = isFalling;
                if (_isFalling)
                {
                    FallBegan?.Invoke();
                    return;
                }

                foreach (var landing in _locomotionSettings.FallLandings)
                {
                    if (landing.SpeedRange.IsInRange(_horizontalSpeed) &&
                        landing.FallDistanceRange.IsInRange(fallDistance))
                    {
                        Land(landing.Id);
                        break;
                    }
                }

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

            if (_isLanding)
            {
                _horizontalSpeed = 0;
                return;
            }

            var targetSpeed = GetTargetSpeed();
            var acceleration = GetAcceleration(targetSpeed);

            var speed = Mathf.MoveTowards(
                _horizontalSpeed,
                targetSpeed,
                acceleration * Time.deltaTime);

            if (Mathf.Abs(_turnAngle) >= 120f)
                speed = Mathf.Min(speed, _locomotionSettings.WalkSpeed);
            else if (Mathf.Abs(_turnAngle) >= 40f)
                speed = Mathf.Min(speed, _locomotionSettings.RunSpeed);

            _horizontalSpeed = speed;
        }

        void CalculateVerticalSpeed()
        {
            if (_jump)
            {
                _jump = false;
                _isJumping = true;
                _verticalSpeed = Mathf.Sqrt(_locomotionSettings.JumpHeight * -2f * _locomotionSettings.Gravity);
                JumpBegan?.Invoke();
                return;
            }

            if (_isJumping && _characterController.isGrounded && _verticalSpeed <= 0f)
            {
                _isJumping = false;
                JumpEnded?.Invoke();

                if (!_isLanding)
                {
                    foreach (var landing in _locomotionSettings.JumpLandings)
                    {
                        if (landing.SpeedRange.IsInRange(_horizontalSpeed) &&
                            landing.FallDistanceRange.IsInRange(_fallDistance))
                        {
                            Land(landing.Id);
                            break;
                        }
                    }
                }
            }

            if (!_characterController.isGrounded)
            {
                _verticalSpeed += _locomotionSettings.Gravity * Time.deltaTime;
                return;
            }

            _verticalSpeed = _verticalSpeed < 0f
                ? _locomotionSettings.GroundedGravity
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
            var velocity = (_input.magnitude > 0.1f ? _input : _rootTransform.forward) * _horizontalSpeed;
            velocity.y = _verticalSpeed;
            return velocity;
        }

        float GetTargetSpeed()
        {
            if (_input.sqrMagnitude < 0.001f)
                return 0f;
            if (Sprint)
                return Strafe ? _locomotionSettings.RunSpeed : _locomotionSettings.SprintSpeed;
            if (Walk)
                return _locomotionSettings.WalkSpeed;
            return _locomotionSettings.RunSpeed;
        }

        float GetAcceleration(float targetSpeed)
        {
            return targetSpeed > _horizontalSpeed
                ? _locomotionSettings.Acceleration
                : _locomotionSettings.Deceleration;
        }

        void Pivot(int pivotId)
        {
            _isPivoting = true;
            PivotRequested?.Invoke(pivotId);
        }

        void Land(int landId)
        {
            _isLanding = true;
            LandingRequested?.Invoke(landId);
        }
    }
}