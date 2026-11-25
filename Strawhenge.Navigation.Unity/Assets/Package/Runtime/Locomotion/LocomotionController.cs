using System;
using System.Collections.Generic;
using UnityEngine;
using ILogger = Strawhenge.Common.Logging.ILogger;

namespace Strawhenge.Navigation.Unity
{
    // Direct-control locomotion: speeds, turning, pivots, jumps, coyote time, falls and landings. Knows nothing
    // about how the character is moved; ICharacterMotor does that. LocomotionScript wires it up, steps it and
    // turns it on and off; consumers make their requests here (LocomotionScript.LocomotionController).
    //
    // Each step is two halves either side of the motor's move:
    // BeginStep reads the motor (grounded, position, rotation) and works out the move; EndStep reacts to the
    // result. Pull motors are driven by Step; externally stepped motors call the halves from their callbacks,
    // through ILocomotionStepper.
    public sealed class LocomotionController : ILocomotionStepper
    {
        // From this turn angle on, the speed is limited to RunSpeed, and the turn is remembered for pivots.
        const float SharpTurnAngle = 40f;

        // From this turn angle on, the speed is limited to WalkSpeed.
        const float ReversalAngle = 120f;

        readonly ICharacterMotor _motor;
        readonly float _animatorWaitTimeout;
        readonly ILogger _logger;

        ILocomotionSettings _settings;
        bool _isActive;
        bool _isAwaitingEndStep;
        bool _isGrounded;
        bool _wasGrounded;
        Vector3 _velocity;

        Vector3 _input;

        float _horizontalSpeed;
        float _verticalSpeed;

        // The horizontal velocity asked for in the last step that moved. In the air it's the velocity itself:
        // steered towards the input, or held.
        Vector3 _momentum;

        bool _isAwaitingJumpTrigger;
        bool _jump;
        bool _isJumping;

        Quaternion _targetRotation;

        bool _isFalling;
        float _timeSinceGrounded;
        float _fallDistance;
        float _groundedY;
        float _landingDrop;
        bool _isLanding;

        float _turnAngle;
        bool _isPivoting;
        Quaternion _pivotStartRotation;
        bool _isAwaitingStationaryPivot;
        bool _suppressPivotsUntilTurned;
        IPivotProfile _stationaryPivot;

        // Where a sharp turn began: the facing and speed before it slowed the character. A stick sweeps through
        // the angles in between on its way round, limiting the speed and turning the character as it goes, so
        // moving pivots match on these rather than on where the sweep has got to.
        bool _isSharpTurning;
        Quaternion _sharpTurnStartRotation;
        float _sharpTurnStartSpeed;

        int _pivotId;
        float _pivotWaitTime;
        int _landingId;
        float _landingWaitTime;
        float _jumpTriggerWaitTime;
        readonly HashSet<int> _timedOutPivotIds = new();
        readonly HashSet<int> _timedOutLandingIds = new();
        readonly HashSet<int> _timedOutJumpIds = new();

        // The jump profile whose wind-up is awaited (while _isAwaitingJumpTrigger).
        int _jumpId;

        // settings: pass a CachedLocomotionSettings; the profile arrays are read every step.
        public LocomotionController(
            ICharacterMotor motor,
            ILocomotionSettings settings,
            float animatorWaitTimeout,
            ILogger logger)
        {
            _motor = motor;
            _settings = settings;
            _animatorWaitTimeout = animatorWaitTimeout;
            _logger = logger;
        }

        // A jump profile matched: its wind-up animation should play, and launch the jump (TriggerJump).
        public event Action<int> JumpTriggerRequested;

        // A requested jump wind-up timed out and the jump was cancelled, so nothing should launch it.
        public event Action JumpTriggerCancelled;

        public event Action JumpBegan;

        public event Action JumpEnded;

        public event Action<int> PivotRequested;

        public event Action FallBegan;

        public event Action FallEnded;

        public event Action<int> LandingRequested;

        // Follows LocomotionScript being enabled and disabled.
        public event Action<bool> IsActiveChanged;

        public bool IsActive => _isActive;

        // The velocity of the last step that moved, kept through steps that don't (a pivot playing): Pivoting and
        // the animation sync expect the velocity coming in. Kept here so motors only report their last move.
        public Vector3 CurrentVelocity => _velocity;

        public bool Walk { get; set; }

        public bool Sprint { get; set; }

        public bool Strafe { get; set; }

        // Jumping, or falling once the character has dropped FallDistance. Not just off the ground: stepping down
        // stairs or a small bump, and coyote time, are still on the ground as far as control goes.
        bool IsAirborne => _isJumping || _isFalling;

        // The same threshold GetVelocity has always used for following the input rather than the facing.
        bool HasMoveInput => _input.sqrMagnitude > 0.01f;

        // In the air, whether _momentum is held rather than steered: always with Momentum, and while there's no
        // input with ControlledKeepsMomentum.
        bool HoldsMomentum =>
            _settings.AirborneMovement == AirborneMovement.Momentum ||
            (_settings.AirborneMovement == AirborneMovement.ControlledKeepsMomentum && !HasMoveInput);

        internal void SetSettings(ILocomotionSettings settings) => _settings = settings;

        internal void Activate()
        {
            _isActive = true;
            _wasGrounded = false;
            _velocity = Vector3.zero;
            _momentum = Vector3.zero;

            // Keep facing the way the character does until there's input. Otherwise it would turn to the input
            // from before it was deactivated (something else may have turned it since), or, before any input,
            // towards an all-zero quaternion.
            _targetRotation = _motor.Rotation;

            _motor.SetActive(true);
            IsActiveChanged?.Invoke(true);
        }

        // While inactive, MovementSourceManagerScript routes the Animator's completion calls
        // (CompletePivot, CompleteLanding, TriggerJump) to another movement source, so anything
        // still waiting on them would block this controller forever once it is activated again.
        internal void Deactivate()
        {
            CancelAnimatorWaits();

            _isActive = false;
            _isAwaitingEndStep = false;
            _velocity = Vector3.zero;
            _momentum = Vector3.zero;
            _motor.SetActive(false);
            IsActiveChanged?.Invoke(false);
        }

        internal void Step(float deltaTime)
        {
            var step = BeginStep(deltaTime);

            if (step.ShouldMove)
                EndStep(_motor.Move(step, deltaTime));
        }

        // Explicit, so the stepping only shows to the motors given the stepper, not on the controller's API.
        LocomotionStep ILocomotionStepper.BeginStep(float deltaTime) => BeginStep(deltaTime);

        void ILocomotionStepper.EndStep(MotorMoveResult result) => EndStep(result);

        // When ShouldMove is false (inactive, or a pivot is playing), the motor must leave the character
        // where it is and keep Rotation; CurrentVelocity keeps the last move's velocity. EndStep isn't needed.
        LocomotionStep BeginStep(float deltaTime)
        {
            if (!_isActive)
                return LocomotionStep.Stationary(_motor.Rotation);

            // Read once, so every part of the step sees the same value (and a motor that works it out on
            // demand does that once).
            _isGrounded = _motor.IsGrounded;

            HandleAnimatorWaitTimeouts(deltaTime);
            HandleFalling(deltaTime);

            var rotation = HandleRotation(deltaTime);

            if (_isPivoting)
                return LocomotionStep.Stationary(rotation);

            // Read before CalculateVerticalSpeed starts or ends a jump, so the whole step is either in the air
            // or on the ground.
            var isAirborne = IsAirborne;

            CalculateHorizontalSpeed(deltaTime, isAirborne);
            var jumpStarted = CalculateVerticalSpeed(deltaTime);

            _isAwaitingEndStep = true;
            return LocomotionStep.Moving(GetVelocity(rotation, isAirborne), rotation, jumpStarted);
        }

        void EndStep(MotorMoveResult result)
        {
            if (!_isAwaitingEndStep) return;
            _isAwaitingEndStep = false;

            _velocity = _motor.Velocity;
            ManageCollisions(result);
        }

        public void Move(Vector3 input)
        {
            input.y = 0;

            if (input.sqrMagnitude > 1f)
                input.Normalize();

            if (input.sqrMagnitude > 0.001f)
                _targetRotation = Quaternion.LookRotation(input);

            // Queued on the ground, but pivots don't play in the air.
            if (_isAwaitingStationaryPivot && IsAirborne)
            {
                _isAwaitingStationaryPivot = false;
                _stationaryPivot = null;
            }

            if (_isAwaitingStationaryPivot)
            {
                if (_input.sqrMagnitude < 0.000001f)
                {
                    _isAwaitingStationaryPivot = false;

                    // Held since it matched, so its pivot asset may have been cleared meanwhile:
                    // then there's no pivot, and the character turns normally.
                    if (_stationaryPivot.Id is int stationaryPivotId)
                        Pivot(stationaryPivotId);

                    _stationaryPivot = null;
                }
                else if (input.sqrMagnitude > _input.sqrMagnitude)
                {
                    _isAwaitingStationaryPivot = false;
                    _stationaryPivot = null;
                }
            }

            _input = input;
        }

        // With a matching jump profile, waits for its wind-up animation to launch the jump (TriggerJump);
        // otherwise jumps straight away. The profile is matched once, here: pressing again during the wind-up
        // does nothing.
        public void Jump()
        {
            if (_isJumping || _isPivoting || _isLanding || _isAwaitingJumpTrigger ||
                _timeSinceGrounded > _settings.CoyoteTime)
                return;

            _isAwaitingJumpTrigger = true;
            _jumpTriggerWaitTime = 0f;

            if (TryMatchJump(out var jumpId))
            {
                _jumpId = jumpId;
                JumpTriggerRequested?.Invoke(jumpId);
            }
            else
            {
                TriggerJump();
            }
        }

        public void TriggerJump()
        {
            if (!_isAwaitingJumpTrigger) return;
            _isAwaitingJumpTrigger = false;

            _jump = true;
        }

        // Matched on the speed and, while strafing, the direction of movement relative to the facing. A wind-up
        // only plays from the ground: already off it (coyote time), the character jumps straight away.
        bool TryMatchJump(out int jumpId)
        {
            if (_timeSinceGrounded > 0f)
            {
                jumpId = default;
                return false;
            }

            return JumpMatching.TryMatch(
                _settings.Jumps,
                _settings.StrafeJumps,
                Strafe,
                _horizontalSpeed,
                JumpMatching.GetDirection(_motor.Rotation, _momentum),
                out jumpId);
        }

        // Root motion has turned the character while the pivot played, so there's nothing left to apply.
        public void CompletePivot()
        {
            if (!_isPivoting) return;

            _isPivoting = false;

            // A pivot that didn't turn the character (no animation played it, e.g. no
            // PivotStateMachineBehaviour, or a clip without root rotation) would match again next step
            // and stop the character ever turning. Turn normally until out of every pivot's angle range.
            if (!PivotTurn.DidTurn(_pivotStartRotation, _motor.Rotation))
                _suppressPivotsUntilTurned = true;
        }

        public void CompleteLanding()
        {
            if (!_isLanding) return;
            _isLanding = false;
        }

        void CancelAnimatorWaits()
        {
            _isPivoting = false;
            _isAwaitingStationaryPivot = false;
            _stationaryPivot = null;
            _suppressPivotsUntilTurned = false;
            _isSharpTurning = false;

            _isLanding = false;

            _isAwaitingJumpTrigger = false;
            _jump = false;
        }

        // Safety net for waits nobody completes: no MovementAnimationSyncScript, nothing wired up, or an
        // animation that never finishes. MovementAnimationSyncScript gives up much sooner on an animation that
        // never starts; this covers everything else.
        void HandleAnimatorWaitTimeouts(float deltaTime)
        {
            if (_isPivoting)
            {
                _pivotWaitTime += deltaTime;

                if (_pivotWaitTime > _animatorWaitTimeout)
                {
                    WarnTimedOut(_timedOutPivotIds, _pivotId, "Pivot");
                    CompletePivot();
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
                    if (_timedOutJumpIds.Add(_jumpId))
                    {
                        _logger.LogWarning(
                            $"Jump ID {_jumpId} wasn't launched within {_animatorWaitTimeout} s - cancelling it. " +
                            $"Check the jump animation calls {nameof(MovementAnimationSyncScript)}.OnJumpLaunch.");
                    }

                    // Cancelled rather than launched: a jump this long after the button press would be unexpected.
                    _isAwaitingJumpTrigger = false;
                    JumpTriggerCancelled?.Invoke();
                }
            }
        }

        void WarnTimedOut(HashSet<int> timedOutIds, int id, string kind)
        {
            if (!timedOutIds.Add(id))
                return;

            _logger.LogWarning(
                $"{kind} ID {id} wasn't completed within {_animatorWaitTimeout} s - carrying on without it. " +
                $"Check a {nameof(MovementAnimationSyncScript)} is set up and the Animator has a transition for it.");
        }

        // Returns the rotation for this step. Unchanged while pivoting or landing, or when a pivot starts.
        Quaternion HandleRotation(float deltaTime)
        {
            var rotation = _motor.Rotation;

            if (_isPivoting || _isLanding) return rotation;

            var isAirborne = IsAirborne;
            if (isAirborne)
            {
                // Pivots are turns on the ground, so none in the air, and one queued on the ground is dropped.
                _isAwaitingStationaryPivot = false;
                _stationaryPivot = null;
                _isSharpTurning = false;

                if (!_settings.AllowAirborneRotation)
                {
                    // Not turning, so the speed isn't limited for a sharp turn. _targetRotation keeps following
                    // the input, so the character turns to it once it lands.
                    _turnAngle = 0;
                    return rotation;
                }
            }

            if (_horizontalSpeed >= 1f)
            {
                _isAwaitingStationaryPivot = false;
                _stationaryPivot = null;
            }

            if (Strafe)
            {
                _turnAngle = 0;
                _targetRotation = rotation;
                _isSharpTurning = false;
                return rotation;
            }

            _turnAngle = Vector3.SignedAngle(
                rotation * Vector3.forward,
                _input.normalized,
                Vector3.up
            );

            TrackSharpTurn(rotation, isAirborne);

            // No pivots in the air (see above): the character turns normally.
            if (!isAirborne && HandlePivots())
                return rotation;

            return Quaternion.RotateTowards(
                rotation,
                _targetRotation,
                _settings.TurnSpeed * deltaTime);
        }

        // Queues a stationary pivot, or starts a moving one. Returns true when a moving pivot starts.
        bool HandlePivots()
        {
            if (_suppressPivotsUntilTurned)
            {
                if (!IsInAnyPivotAngleRange())
                    _suppressPivotsUntilTurned = false;

                return false;
            }

            if (CheckForStationaryPivots(out var stationaryPivot))
            {
                _isAwaitingStationaryPivot = true;
                _stationaryPivot = stationaryPivot;
            }

            if (CheckForMovingPivots(out var movingPivot) && movingPivot.Id is int movingPivotId)
            {
                Pivot(movingPivotId);
                return true;
            }

            return false;
        }

        bool IsInAnyPivotAngleRange() =>
            IsInAngleRange(_settings.StationaryPivots) ||
            IsInAngleRange(_settings.MovingPivots);

        bool IsInAngleRange(IPivotProfile[] pivots)
        {
            foreach (var pivot in pivots)
            {
                if (pivot.IsInAngleRange(_turnAngle))
                    return true;
            }

            return false;
        }

        // Called before the step's speed is worked out, so _horizontalSpeed is still the speed coming into the turn.
        // Not in the air (no pivots there), nor while pivots are suppressed, which would match the same pivot again
        // once the suppression ends.
        void TrackSharpTurn(Quaternion rotation, bool isAirborne)
        {
            if (isAirborne || _suppressPivotsUntilTurned || Mathf.Abs(_turnAngle) < SharpTurnAngle)
            {
                _isSharpTurning = false;
                return;
            }

            if (_isSharpTurning) return;

            _isSharpTurning = true;
            _sharpTurnStartRotation = rotation;
            _sharpTurnStartSpeed = _horizontalSpeed;
        }

        bool CheckForStationaryPivots(out IPivotProfile matchingPivot)
        {
            foreach (var pivot in _settings.StationaryPivots)
            {
                if (pivot.Matches(_horizontalSpeed, _turnAngle))
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

            var speed = _horizontalSpeed;
            var turnAngle = _turnAngle;

            if (_isSharpTurning)
            {
                speed = _sharpTurnStartSpeed;
                turnAngle = Vector3.SignedAngle(
                    _sharpTurnStartRotation * Vector3.forward,
                    _input.normalized,
                    Vector3.up);
            }

            foreach (var pivot in _settings.MovingPivots)
            {
                if (pivot.Matches(speed, turnAngle))
                {
                    matchingPivot = pivot;
                    return true;
                }
            }

            matchingPivot = null;
            return false;
        }

        void HandleFalling(float deltaTime)
        {
            var isGrounded = _isGrounded;
            var positionY = _motor.Position.y;

            // Coyote time counts from leaving the ground, however far the character has dropped.
            // The fall itself (FallBegan, landings) still waits for FallDistance.
            _timeSinceGrounded = isGrounded
                ? 0f
                : _timeSinceGrounded + deltaTime;

            // How far below the last grounded height the character is, measured before landing resets that
            // height: what fall and jump landings match on. Like the destination's landings, it's the drop from
            // where the character left the ground to where it landed.
            _landingDrop = Mathf.Max(0f, _groundedY - positionY);

            if (isGrounded)
                _groundedY = positionY;

            _fallDistance = Mathf.Max(0f, _groundedY - positionY);

            var isFalling = !isGrounded && _fallDistance >= _settings.FallDistance;
            if (isFalling != _isFalling)
            {
                _isFalling = isFalling;
                if (_isFalling)
                {
                    FallBegan?.Invoke();
                    return;
                }

                foreach (var landing in _settings.FallLandings)
                {
                    if (landing.Matches(_horizontalSpeed, _landingDrop) && landing.Id is int landingId)
                    {
                        Land(landingId);
                        break;
                    }
                }

                FallEnded?.Invoke();
            }
        }

        void CalculateHorizontalSpeed(float deltaTime, bool isAirborne)
        {
            if (_isAwaitingJumpTrigger)
                return;

            if (isAirborne)
            {
                // Held, the speed stays as it was; walls still cap it. Kept up to date either way for landings,
                // which match on it, and for carrying on once landed.
                if (!HoldsMomentum)
                    SteerInTheAir(deltaTime);

                return;
            }

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
                acceleration * deltaTime);

            if (Mathf.Abs(_turnAngle) >= ReversalAngle)
                speed = Mathf.Min(speed, _settings.WalkSpeed);
            else if (Mathf.Abs(_turnAngle) >= SharpTurnAngle)
                speed = Mathf.Min(speed, _settings.RunSpeed);

            _horizontalSpeed = speed;
        }

        // Returns true when a jump begins this step.
        bool CalculateVerticalSpeed(float deltaTime)
        {
            var isGrounded = _isGrounded;

            if (_jump)
            {
                _jump = false;
                _isJumping = true;
                _verticalSpeed = Mathf.Sqrt(_settings.JumpHeight * -2f * _settings.Gravity);
                JumpBegan?.Invoke();
                return true;
            }

            if (_isJumping && isGrounded && _verticalSpeed <= 0f)
            {
                _isJumping = false;
                JumpEnded?.Invoke();

                if (!_isLanding)
                {
                    foreach (var landing in _settings.JumpLandings)
                    {
                        if (landing.Matches(_horizontalSpeed, _landingDrop) && landing.Id is int landingId)
                        {
                            Land(landingId);
                            break;
                        }
                    }
                }
            }

            if (!isGrounded)
            {
                // Left the ground without jumping (walked off a ledge, or lost it going downhill): the fall starts
                // at FallStartSpeed rather than from still.
                if (_wasGrounded && !_isJumping)
                    _verticalSpeed = Mathf.Min(_verticalSpeed, -_settings.FallStartSpeed);

                _wasGrounded = false;
                _verticalSpeed += _settings.Gravity * deltaTime;
                return false;
            }

            // On the ground: no speed down. Motors keep the character on the ground their own way (the
            // CharacterController motor pushes down, the others snap).
            _wasGrounded = true;
            _verticalSpeed = Mathf.Max(0f, _verticalSpeed);

            return false;
        }

        // On the ground the direction follows the input straight away (turning limits the speed instead). In the
        // air the velocity changes at the ground rates: what's across or against the input slows at Deceleration,
        // and what's along it speeds up at Acceleration (or slows at Deceleration, above the target speed). So
        // reversing slows to a stop, then speeds up the other way. Without input it all slows at Deceleration.
        void SteerInTheAir(float deltaTime)
        {
            var target = _input * GetTargetSpeed();
            var targetSpeed = target.magnitude;
            var direction = targetSpeed > 0.0001f ? target / targetSpeed : Vector3.zero;

            var along = Vector3.Dot(_momentum, direction);
            var across = _momentum - direction * along;

            across = Vector3.MoveTowards(across, Vector3.zero, _settings.Deceleration * deltaTime);

            var rate = along >= 0f && along < targetSpeed
                ? _settings.Acceleration
                : _settings.Deceleration;
            along = Mathf.MoveTowards(along, targetSpeed, rate * deltaTime);

            _momentum = across + direction * along;
            _horizontalSpeed = _momentum.magnitude;
        }

        void ManageCollisions(MotorMoveResult result)
        {
            var isAirborne = IsAirborne;
            var blocked = result.BlockedSideways && (_input.sqrMagnitude > 0.001f || isAirborne);

            if (!blocked)
                return;

            _horizontalSpeed = Mathf.Min(
                _horizontalSpeed,
                _velocity.magnitude);

            if (isAirborne)
            {
                // The velocity in the air carries on as the move turned out: stopped by a wall, or sliding along it.
                _momentum = Vector3.ProjectOnPlane(_velocity, Vector3.up);
                _horizontalSpeed = Mathf.Min(_horizontalSpeed, _momentum.magnitude);
            }
        }

        Vector3 GetVelocity(Quaternion rotation, bool isAirborne)
        {
            var velocity = GetHorizontalVelocity(rotation, isAirborne);
            _momentum = velocity;
            velocity.y = _verticalSpeed;
            return velocity;
        }

        Vector3 GetHorizontalVelocity(Quaternion rotation, bool isAirborne)
        {
            // Steered or held by CalculateHorizontalSpeed.
            if (isAirborne)
                return _momentum;

            if (HasMoveInput)
                return _input * _horizontalSpeed;

            return rotation * Vector3.forward * _horizontalSpeed;
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
            _isSharpTurning = false;
            _pivotStartRotation = _motor.Rotation;
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
