namespace Strawhenge.Navigation.Unity
{
    // How LocomotionScript reads its settings. Values are passed through, so inspector edits apply at once.
    // The profile lists are cached, because they're read every frame and resolving them allocates.
    // See ProfileCacheVersion for when they're rebuilt.
    sealed class CachedLocomotionSettings : ILocomotionSettings
    {
        readonly ILocomotionSettings _settings;

        int _cachedVersion;
        IPivotProfile[] _stationaryPivots;
        IPivotProfile[] _movingPivots;
        ILandingProfile[] _jumpLandings;
        ILandingProfile[] _fallLandings;

        public CachedLocomotionSettings(ILocomotionSettings settings)
        {
            _settings = settings;
        }

        // Rebuilds every settings cache, destination ones included.
        public static void Invalidate() => ProfileCacheVersion.Invalidate();

        public float WalkSpeed => _settings.WalkSpeed;

        public float RunSpeed => _settings.RunSpeed;

        public float SprintSpeed => _settings.SprintSpeed;

        public float Acceleration => _settings.Acceleration;

        public float Deceleration => _settings.Deceleration;

        public float TurnSpeed => _settings.TurnSpeed;

        public float JumpHeight => _settings.JumpHeight;

        public float CoyoteTime => _settings.CoyoteTime;

        public bool DeferJumpTrigger => _settings.DeferJumpTrigger;

        public float Gravity => _settings.Gravity;

        public float GroundedGravity => _settings.GroundedGravity;

        public float FallDistance => _settings.FallDistance;

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
