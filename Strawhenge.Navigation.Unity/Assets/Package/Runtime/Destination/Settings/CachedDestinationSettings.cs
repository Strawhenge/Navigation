using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    // How DestinationController reads its settings. Values are passed through, so inspector edits apply at
    // once. The profile lists are cached, because pivots are checked every frame and resolving them
    // allocates. See ProfileCacheVersion for when they're rebuilt.
    sealed class CachedDestinationSettings : IDestinationSettings
    {
        readonly IDestinationSettings _settings;

        int _cachedVersion;
        IPivotProfile[] _stationaryPivots;
        IPivotProfile[] _movingPivots;
        ILandingProfile[] _jumpLandings;
        ILandingProfile[] _fallLandings;

        public CachedDestinationSettings(IDestinationSettings settings)
        {
            _settings = settings;
        }

        public float JumpSpeed => _settings.JumpSpeed;

        public float FallHorizontalSpeed => _settings.FallHorizontalSpeed;

        public float FallAcceleration => _settings.FallAcceleration;

        public LayerMask GroundLayers => _settings.GroundLayers;

        public float ApproximateSearchDistance => _settings.ApproximateSearchDistance;

        public bool DeferJumpTrigger => _settings.DeferJumpTrigger;

        public float AnimatorWaitTimeout => _settings.AnimatorWaitTimeout;

        public IPivotProfile[] StationaryPivots
        {
            get
            {
                RefreshLists();
                return _stationaryPivots;
            }
        }

        public IPivotProfile[] MovingPivots
        {
            get
            {
                RefreshLists();
                return _movingPivots;
            }
        }

        public ILandingProfile[] JumpLandings
        {
            get
            {
                RefreshLists();
                return _jumpLandings;
            }
        }

        public ILandingProfile[] FallLandings
        {
            get
            {
                RefreshLists();
                return _fallLandings;
            }
        }

        void RefreshLists()
        {
            if (_cachedVersion == ProfileCacheVersion.Current)
                return;

            _stationaryPivots = _settings.StationaryPivots;
            _movingPivots = _settings.MovingPivots;
            _jumpLandings = _settings.JumpLandings;
            _fallLandings = _settings.FallLandings;

            // Only once all four are built, so a failed build is retried rather than leaving lists missing.
            _cachedVersion = ProfileCacheVersion.Current;
        }
    }
}
