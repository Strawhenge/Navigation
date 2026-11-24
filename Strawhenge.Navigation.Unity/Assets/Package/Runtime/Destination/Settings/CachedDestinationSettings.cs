using UnityEngine;
using ILogger = Strawhenge.Common.Logging.ILogger;

namespace Strawhenge.Navigation.Unity.Destination
{
    // How DestinationController reads its settings. Values are passed through, so inspector edits apply at
    // once. The profile lists are cached, because pivots are checked every frame and resolving them
    // allocates. See ProfileCacheVersion for when they're rebuilt.
    sealed class CachedDestinationSettings : IDestinationSettings
    {
        readonly IDestinationSettings _settings;
        readonly ILogger _logger;

        int _cachedVersion;
        IPivotProfile[] _stationaryPivots;
        IPivotProfile[] _movingPivots;
        IJumpProfile[] _jumps;
        IStrafeJumpProfile[] _strafeJumps;
        ILandingProfile[] _jumpLandings;
        ILandingProfile[] _fallLandings;

        // logger: warns about profiles that are ignored because they can't be used.
        public CachedDestinationSettings(IDestinationSettings settings, ILogger logger)
        {
            _settings = settings;
            _logger = logger;
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

        public IJumpProfile[] Jumps
        {
            get
            {
                RefreshLists();
                return _jumps;
            }
        }

        public IStrafeJumpProfile[] StrafeJumps
        {
            get
            {
                RefreshLists();
                return _strafeJumps;
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

            _stationaryPivots = ProfileSources.Filter(
                _settings.StationaryPivots, PivotProfileExtensions.FindProblem, nameof(StationaryPivots), _logger);
            _movingPivots = ProfileSources.Filter(
                _settings.MovingPivots, PivotProfileExtensions.FindProblem, nameof(MovingPivots), _logger);
            _jumps = ProfileSources.Filter(
                _settings.Jumps, JumpProfileExtensions.FindProblem, nameof(Jumps), _logger);
            _strafeJumps = ProfileSources.Filter(
                _settings.StrafeJumps, StrafeJumpProfileExtensions.FindProblem, nameof(StrafeJumps), _logger);
            _jumpLandings = ProfileSources.Filter(
                _settings.JumpLandings, LandingProfileExtensions.FindProblem, nameof(JumpLandings), _logger);
            _fallLandings = ProfileSources.Filter(
                _settings.FallLandings, LandingProfileExtensions.FindProblem, nameof(FallLandings), _logger);

            // Only once all of them are built, so a failed build is retried rather than leaving lists missing.
            _cachedVersion = ProfileCacheVersion.Current;
        }
    }
}
