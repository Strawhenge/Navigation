using Strawhenge.Common.Unity.Serialization;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    [CreateAssetMenu(menuName = "Strawhenge/Navigation/Destination Settings")]
    public class DestinationSettingsScriptableObject : ScriptableObject, IDestinationSettings
    {
        [Header("Jumping")]
        [Tooltip("Horizontal speed while jumping across an off-mesh link.")]
        [SerializeField] float _jumpSpeed = DefaultDestinationSettings.JumpSpeed;

        [Header("Falling")]
        [Tooltip("Horizontal speed while dropping down an off-mesh link.")]
        [SerializeField] float _fallHorizontalSpeed = DefaultDestinationSettings.FallHorizontalSpeed;

        [Tooltip("Downward acceleration while dropping down an off-mesh link.")]
        [SerializeField] float _fallAcceleration = DefaultDestinationSettings.FallAcceleration;

        [Tooltip("Layers that count as ground when checking whether a drop has left its ledge. " +
                 "Leave out layers of the character's own colliders (hitboxes, ragdoll parts).")]
        [SerializeField] LayerMask _groundLayers = DefaultDestinationSettings.GroundLayers;

        [Header("Approximate Destinations")]
        [Tooltip("How far from an approximate destination to look for the nearest point on the NavMesh, " +
                 "when the destination itself isn't on it. Exact destinations are never moved.")]
        [SerializeField] float _approximateSearchDistance = DefaultDestinationSettings.ApproximateSearchDistance;

        [Header("Pivots")]
        [Tooltip("Pivots played instead of turning on the spot, before a trip sets off.")]
        [SerializeField] SerializedSource<
            IPivotProfile,
            SerializedPivotProfile,
            PivotProfileScriptableObject>[] _stationaryPivots;

        [Tooltip("Pivots played instead of turning while following a path.")]
        [SerializeField] SerializedSource<
            IPivotProfile,
            SerializedPivotProfile,
            PivotProfileScriptableObject>[] _movingPivots;

        [Header("Landings")]
        [Tooltip("Landings played after jumping across an off-mesh link.")]
        [SerializeField] SerializedSource<
            ILandingProfile,
            SerializedLandingProfile,
            LandingProfileScriptableObject>[] _jumpLandings;

        [Tooltip("Landings played after dropping down an off-mesh link.")]
        [SerializeField] SerializedSource<
            ILandingProfile,
            SerializedLandingProfile,
            LandingProfileScriptableObject>[] _fallLandings;

        [Header("Animation")]
        [Tooltip("If set, a jump waits at the start of its off-mesh link until the jump animation launches it " +
                 "(MovementAnimationSyncScript.OnJumpLaunch).")]
        [SerializeField] bool _deferJumpTrigger;

        [Tooltip("Longest wait for a pivot or landing to be completed, or a deferred jump to be launched, " +
                 "before carrying on without it. Must be longer than the longest pivot or landing animation.")]
        [SerializeField] float _animatorWaitTimeout = DefaultDestinationSettings.AnimatorWaitTimeout;

        public float JumpSpeed => _jumpSpeed;

        public float FallHorizontalSpeed => _fallHorizontalSpeed;

        public float FallAcceleration => _fallAcceleration;

        public LayerMask GroundLayers => _groundLayers;

        public float ApproximateSearchDistance => _approximateSearchDistance;

        // The profile lists are resolved on every read; DestinationController reads them through
        // CachedDestinationSettings.
        public IPivotProfile[] StationaryPivots =>
            ProfileSources.Resolve(_stationaryPivots, pivot => pivot.Id, nameof(_stationaryPivots), this);

        public IPivotProfile[] MovingPivots =>
            ProfileSources.Resolve(_movingPivots, pivot => pivot.Id, nameof(_movingPivots), this);

        public ILandingProfile[] JumpLandings =>
            ProfileSources.Resolve(_jumpLandings, landing => landing.Id, nameof(_jumpLandings), this);

        public ILandingProfile[] FallLandings =>
            ProfileSources.Resolve(_fallLandings, landing => landing.Id, nameof(_fallLandings), this);

        public bool DeferJumpTrigger => _deferJumpTrigger;

        public float AnimatorWaitTimeout => _animatorWaitTimeout;

        void OnValidate() => ProfileCacheVersion.Invalidate();
    }
}
