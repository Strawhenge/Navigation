using Strawhenge.Common.Unity.Helpers;
using Strawhenge.Common.Unity.Serialization;
using System;
using System.Collections.Generic;
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

        [Tooltip("Longest wait for a pivot or landing to be completed, or a deferred jump to be launched, " +
                 "before carrying on without it. Must be longer than the longest pivot or landing animation.")]
        [SerializeField] float _animatorWaitTimeout = 2f;

        ILocomotionSettings _locomotionSettings;
        bool _settingsSetFromCode;
        Transform _rootTransform;

        Vector3 _input;

        float _horizontalSpeed;
        float _verticalSpeed;

        bool _isAwaitingJumpTrigger;
        bool _jump;
        bool _isJumping;

        Quaternion _targetRotation;

        bool _isFalling;
        float _timeSinceGrounded;
        float _fallDistance;
        float _groundedY;
        bool _isLanding;

        float _turnAngle;
        bool _isPivoting;
        bool _isAwaitingStationaryPivot;
        bool _suppressPivotsUntilTurned;
        IPivotProfile _stationaryPivot;

        int _pivotId;
        float _pivotWaitTime;
        int _landingId;
        float _landingWaitTime;
        float _jumpTriggerWaitTime;
        readonly HashSet<int> _timedOutPivotIds = new();
        readonly HashSet<int> _timedOutLandingIds = new();
        bool _jumpTriggerTimedOut;

        void Awake()
        {
            ComponentRefHelper
                .EnsureHierarchyComponent(ref _characterController, nameof(_characterController), this);

            _locomotionSettings ??= new CachedLocomotionSettings(ResolveSettings());

            _rootTransform = _characterController.transform;
        }

        // Inline settings edits, and swapping the settings asset, apply in play mode. Settings given to
        // SetSettings are kept.
        void OnValidate()
        {
            CachedLocomotionSettings.Invalidate();

            if (Application.isPlaying && _locomotionSettings != null && !_settingsSetFromCode)
                _locomotionSettings = new CachedLocomotionSettings(ResolveSettings());
        }

        ILocomotionSettings ResolveSettings()
        {
            if (_settings != null && _settings.TryGetValue(out var settings))
                return settings;

            Debug.LogWarning($"'{nameof(_settings)}' not set - using defaults.", this);
            return DefaultLocomotionSettings.Instance;
        }

        void Update()
        {
            HandleAnimatorWaitTimeouts();
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
            CancelAnimatorWaits();

            _characterController.enabled = false;
            IsActiveChanged?.Invoke(false);
        }

        // While disabled, MovementSourceManagerScript routes the Animator's completion calls
        // (CompletePivot, CompleteLanding, TriggerJump) to another movement source, so anything
        // still waiting on them would block this script forever once it is re-enabled.
        void CancelAnimatorWaits()
        {
            _isPivoting = false;
            _isAwaitingStationaryPivot = false;
            _stationaryPivot = null;
            _suppressPivotsUntilTurned = false;

            _isLanding = false;

            _isAwaitingJumpTrigger = false;
            _jump = false;
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

                    // Held since it matched, so its pivot asset may have been cleared meanwhile:
                    // then there's no pivot, and the character turns normally.
                    if (_stationaryPivot.Id is int stationaryPivotId)
                        Pivot(stationaryPivotId);

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
            if (_isJumping || _isPivoting || _isLanding || _timeSinceGrounded > _locomotionSettings.CoyoteTime)
                return;

            _isAwaitingJumpTrigger = true;
            _jumpTriggerWaitTime = 0f;

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

            // A pivot that didn't turn the character (no animation played it, e.g. no
            // PivotStateMachineBehaviour, or a clip without root rotation) would match again next frame
            // and stop the character ever turning. Turn normally until out of every pivot's angle range.
            if (rotationDelta == Quaternion.identity)
                _suppressPivotsUntilTurned = true;
        }

        public void CompleteLanding()
        {
            if (!_isLanding) return;
            _isLanding = false;
        }

        internal void SetCharacterController(CharacterController characterController) =>
            _characterController = characterController;

        internal void SetSettings(ILocomotionSettings settings)
        {
            _settingsSetFromCode = true;
            _locomotionSettings = new CachedLocomotionSettings(settings);
        }

        // Safety net for waits nobody completes: no MovementAnimationSyncScript, nothing wired up, or an
        // animation that never finishes. MovementAnimationSyncScript gives up much sooner on an animation that
        // never starts; this covers everything else.
        void HandleAnimatorWaitTimeouts()
        {
            var deltaTime = Time.deltaTime;

            if (_isPivoting)
            {
                _pivotWaitTime += deltaTime;

                if (_pivotWaitTime > _animatorWaitTimeout)
                {
                    WarnTimedOut(_timedOutPivotIds, _pivotId, "Pivot");
                    CompletePivot(Quaternion.identity);
                }
            }

            if (_isLanding)
            {
                _landingWaitTime += deltaTime;

                if (_landingWaitTime > _animatorWaitTimeout)
                {
                    WarnTimedOut(_timedOutLandingIds, _landingId, "Landing");
                    CompleteLanding();
                }
            }

            if (_isAwaitingJumpTrigger)
            {
                _jumpTriggerWaitTime += deltaTime;

                if (_jumpTriggerWaitTime > _animatorWaitTimeout)
                {
                    if (!_jumpTriggerTimedOut)
                    {
                        _jumpTriggerTimedOut = true;
                        Debug.LogWarning(
                            $"Deferred jump wasn't launched within {_animatorWaitTimeout} s - cancelling it. " +
                            "Check the jump animation calls MovementAnimationSyncScript.OnJumpLaunch.",
                            this);
                    }

                    // Cancelled rather than launched: a jump this long after the button press would be unexpected.
                    _isAwaitingJumpTrigger = false;
                }
            }
        }

        void WarnTimedOut(HashSet<int> timedOutIds, int id, string kind)
        {
            if (!timedOutIds.Add(id))
                return;

            Debug.LogWarning(
                $"{kind} ID {id} wasn't completed within {_animatorWaitTimeout} s - carrying on without it. " +
                $"Check a {nameof(MovementAnimationSyncScript)} is set up and the Animator has a transition for it.",
                this);
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

            if (_suppressPivotsUntilTurned)
            {
                if (!IsInAnyPivotAngleRange())
                    _suppressPivotsUntilTurned = false;
            }
            else
            {
                if (CheckForStationaryPivots(out var stationaryPivot))
                {
                    _isAwaitingStationaryPivot = true;
                    _stationaryPivot = stationaryPivot;
                }

                if (CheckForMovingPivots(out var movingPivot) && movingPivot.Id is int movingPivotId)
                {
                    Pivot(movingPivotId);
                    return;
                }
            }

            _rootTransform.rotation = Quaternion.RotateTowards(
                _rootTransform.rotation,
                _targetRotation,
                _locomotionSettings.TurnSpeed * Time.deltaTime);
        }

        bool IsInAnyPivotAngleRange() =>
            IsInAngleRange(_locomotionSettings.StationaryPivots) ||
            IsInAngleRange(_locomotionSettings.MovingPivots);

        bool IsInAngleRange(IPivotProfile[] pivots)
        {
            foreach (var pivot in pivots)
            {
                foreach (var angleRange in pivot.AngleRanges)
                {
                    if (angleRange.IsInRange(_turnAngle))
                        return true;
                }
            }

            return false;
        }

        bool CheckForStationaryPivots(out IPivotProfile matchingPivot)
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

        bool CheckForMovingPivots(out IPivotProfile matchingPivot)
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
            // Coyote time counts from leaving the ground, however far the character has dropped.
            // The fall itself (FallBegan, landings) still waits for FallDistance.
            _timeSinceGrounded = _characterController.isGrounded
                ? 0f
                : _timeSinceGrounded + Time.deltaTime;

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
                        landing.FallDistanceRange.IsInRange(fallDistance) &&
                        landing.Id is int landingId)
                    {
                        Land(landingId);
                        break;
                    }
                }

                FallEnded?.Invoke();
            }
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
                            landing.FallDistanceRange.IsInRange(_fallDistance) &&
                            landing.Id is int landingId)
                        {
                            Land(landingId);
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
            _pivotId = pivotId;
            _pivotWaitTime = 0f;
            PivotRequested?.Invoke(pivotId);
        }

        void Land(int landId)
        {
            _isLanding = true;
            _landingId = landId;
            _landingWaitTime = 0f;
            LandingRequested?.Invoke(landId);
        }
    }
}