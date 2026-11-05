using Strawhenge.Common.Unity.Serialization;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    [CreateAssetMenu(menuName = "Strawhenge/Navigation/Locomotion Settings")]
    public class LocomotionSettingsScriptableObject : ScriptableObject, ILocomotionSettings
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

        public float WalkSpeed => _walkSpeed;

        public float RunSpeed => _runSpeed;

        public float SprintSpeed => _sprintSpeed;

        public float Acceleration => _acceleration;

        public float Deceleration => _deceleration;

        public float TurnSpeed => _turnSpeed;

        // The profile lists are resolved on every read; LocomotionScript reads them through CachedLocomotionSettings.
        public IPivotProfile[] StationaryPivots =>
            ProfileSources.Resolve(_stationaryPivots, pivot => pivot.Id, nameof(_stationaryPivots), this);

        public IPivotProfile[] MovingPivots =>
            ProfileSources.Resolve(_movingPivots, pivot => pivot.Id, nameof(_movingPivots), this);

        public float JumpHeight => _jumpHeight;

        public float CoyoteTime => _coyoteTime;

        public bool DeferJumpTrigger => _deferJumpTrigger;

        public ILandingProfile[] JumpLandings =>
            ProfileSources.Resolve(_jumpLandings, landing => landing.Id, nameof(_jumpLandings), this);

        public float Gravity => _gravity;

        public float GroundedGravity => _groundedGravity;

        public float FallDistance => _fallDistance;

        public ILandingProfile[] FallLandings =>
            ProfileSources.Resolve(_fallLandings, landing => landing.Id, nameof(_fallLandings), this);

        void OnValidate() => CachedLocomotionSettings.Invalidate();
    }
}