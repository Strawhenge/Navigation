using Strawhenge.Common.Unity.Serialization;
using System;
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

        [Tooltip("Jumps played while not strafing. The first that matches plays its wind-up animation, which " +
                 "launches the jump; with no match, the character jumps straight away.")]
        [SerializeField] SerializedSource<
            IJumpProfile,
            SerializedJumpProfile,
            JumpProfileScriptableObject>[] _jumps;

        [Tooltip("Jumps played while strafing, matched on speed and direction. With no match, the character " +
                 "jumps straight away.")]
        [SerializeField] SerializedSource<
            IStrafeJumpProfile,
            SerializedStrafeJumpProfile,
            StrafeJumpProfileScriptableObject>[] _strafeJumps;

        [SerializeField] SerializedSource<
            ILandingProfile,
            SerializedLandingProfile,
            LandingProfileScriptableObject>[] _jumpLandings;

        [Header("Gravity")]
        [SerializeField] float _gravity = DefaultLocomotionSettings.Gravity;

        [Tooltip("The downward speed a fall starts at when leaving the ground without jumping (walking off a ledge).")]
        [SerializeField] float _fallStartSpeed = DefaultLocomotionSettings.FallStartSpeed;

        [Header("Falling")]
        [SerializeField] float _fallDistance = DefaultLocomotionSettings.FallDistance;

        [SerializeField] SerializedSource<
            ILandingProfile,
            SerializedLandingProfile,
            LandingProfileScriptableObject>[] _fallLandings;

        [Header("Airborne")]
        [Tooltip("Whether the character can turn while jumping or falling. When off, it turns to the input once it lands.")]
        [SerializeField] bool _allowAirborneRotation = DefaultLocomotionSettings.AllowAirborneRotation;

        [Tooltip("Control over horizontal movement while jumping or falling. Controlled: as on the ground. " +
                 "Controlled Keeps Momentum: as on the ground, but carries on when the input is released. " +
                 "Momentum: none, carries on at the velocity it left the ground with.")]
        [SerializeField] AirborneMovement _airborneMovement = DefaultLocomotionSettings.AirborneMovement;

        public float WalkSpeed => _walkSpeed;

        public float RunSpeed => _runSpeed;

        public float SprintSpeed => _sprintSpeed;

        public float Acceleration => _acceleration;

        public float Deceleration => _deceleration;

        public float TurnSpeed => _turnSpeed;

        // The profile lists are resolved on every read; LocomotionScript reads them through CachedLocomotionSettings.
        public IPivotProfile[] StationaryPivots =>
            ProfileSources.Resolve(
                _stationaryPivots, PivotProfileExtensions.FindProblem, nameof(_stationaryPivots), null);

        public IPivotProfile[] MovingPivots =>
            ProfileSources.Resolve(_movingPivots, PivotProfileExtensions.FindProblem, nameof(_movingPivots), null);

        public float JumpHeight => _jumpHeight;

        public float CoyoteTime => _coyoteTime;

        public bool DeferJumpTrigger => _deferJumpTrigger;

        public IJumpProfile[] Jumps =>
            ProfileSources.Resolve(_jumps, JumpProfileExtensions.FindProblem, nameof(_jumps), null);

        public IStrafeJumpProfile[] StrafeJumps =>
            ProfileSources.Resolve(
                _strafeJumps, StrafeJumpProfileExtensions.FindProblem, nameof(_strafeJumps), null);

        public ILandingProfile[] JumpLandings =>
            ProfileSources.Resolve(_jumpLandings, LandingProfileExtensions.FindProblem, nameof(_jumpLandings), null);

        public float Gravity => _gravity;

        public float FallStartSpeed => _fallStartSpeed;

        public float FallDistance => _fallDistance;

        public ILandingProfile[] FallLandings =>
            ProfileSources.Resolve(_fallLandings, LandingProfileExtensions.FindProblem, nameof(_fallLandings), null);

        public bool AllowAirborneRotation => _allowAirborneRotation;

        public AirborneMovement AirborneMovement => _airborneMovement;
    }
}