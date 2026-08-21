using Strawhenge.Common.Unity.Serialization;
using System;
using System.Linq;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    [Serializable]
    public class SerializedLocomotionSettings : ILocomotionSettings
    {
        [Header("Speed")]
        [SerializeField] float _walkSpeed = DefaultLocomotionSettings.WalkSpeed;
        [SerializeField] float _runSpeed = DefaultLocomotionSettings.RunSpeed;
        [SerializeField] float _sprintSpeed = DefaultLocomotionSettings.SprintSpeed;

        [Header("Acceleration")]
        [SerializeField] float _acceleration = DefaultLocomotionSettings.Acceleration;
        [SerializeField] float _deceleration = DefaultLocomotionSettings.Deceleration;

        [Header("Turning")]
        [SerializeField] float _turnSpeed = DefaultLocomotionSettings.TurnSpeed;

        [SerializeField] SerializedSource<
            IPivotProfile,
            SerializedPivotProfile,
            PivotProfileScriptableObject>[] _stationaryPivots;

        [SerializeField] SerializedSource<
            IPivotProfile,
            SerializedPivotProfile,
            PivotProfileScriptableObject>[] _movingPivots;

        [Header("Jumping")]
        [SerializeField] float _jumpHeight = DefaultLocomotionSettings.JumpHeight;
        [SerializeField] float _coyoteTime = DefaultLocomotionSettings.CoyoteTime;
        [SerializeField] bool _deferJumpTrigger;

        [SerializeField] SerializedSource<
            ILandingProfile,
            SerializedLandingProfile,
            LandingProfileScriptableObject>[] _jumpLandings;

        [Header("Gravity")]
        [SerializeField] float _gravity = DefaultLocomotionSettings.Gravity;
        [SerializeField] float _groundedGravity = DefaultLocomotionSettings.GroundedGravity;

        [Header("Falling")]
        [SerializeField] float _fallDistance = DefaultLocomotionSettings.FallDistance;

        [SerializeField] SerializedSource<
            ILandingProfile,
            SerializedLandingProfile,
            LandingProfileScriptableObject>[] _fallLandings;

        IPivotProfile[] _stationaryPivotProfiles;
        IPivotProfile[] _movingPivotProfiles;
        ILandingProfile[] _jumpLandingProfiles;
        ILandingProfile[] _fallLandingProfiles;

        public float WalkSpeed => _walkSpeed;

        public float RunSpeed => _runSpeed;

        public float SprintSpeed => _sprintSpeed;

        public float Acceleration => _acceleration;

        public float Deceleration => _deceleration;

        public float TurnSpeed => _turnSpeed;

        public IPivotProfile[] StationaryPivots =>
            _stationaryPivotProfiles ??= _stationaryPivots
                .Select(pivot => pivot.TryGetValue(out var value) ? value : null)
                .Where(pivot => pivot != null)
                .ToArray();

        public IPivotProfile[] MovingPivots => _movingPivotProfiles ??= _movingPivots
            .Select(pivot => pivot.TryGetValue(out var value) ? value : null)
            .Where(pivot => pivot != null)
            .ToArray();

        public float JumpHeight => _jumpHeight;

        public float CoyoteTime => _coyoteTime;

        public bool DeferJumpTrigger => _deferJumpTrigger;

        public ILandingProfile[] JumpLandings =>
            _jumpLandingProfiles ??= _jumpLandings
                .Select(landing => landing.TryGetValue(out var value) ? value : null)
                .Where(landing => landing != null)
                .ToArray();

        public float Gravity => _gravity;

        public float GroundedGravity => _groundedGravity;

        public float FallDistance => _fallDistance;

        public ILandingProfile[] FallLandings =>
            _fallLandingProfiles ??= _fallLandings
                .Select(landing => landing.TryGetValue(out var value) ? value : null)
                .Where(landing => landing != null)
                .ToArray();
    }
}