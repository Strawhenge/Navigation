namespace Strawhenge.Navigation.Unity
{
    // How LocomotionScript reads its settings. Values are passed through, so inspector edits apply at once.
    // The profile lists are cached as snapshots (ResolvedPivotProfile / ResolvedLandingProfile), because
    // they're matched every frame, and resolving them, or reading a profile's ranges, allocates.
    // In the Editor, OnValidate on anything that can change a list calls Invalidate(), and every cache
    // rebuilds its lists on the next read. In builds that never happens, so the lists are resolved once.
    sealed class CachedLocomotionSettings : ILocomotionSettings
    {
        // Starts at 1 so a new cache (version 0) always resolves its lists on first read.
        static int _version = 1;

        readonly ILocomotionSettings _settings;

        int _cachedVersion;
        ResolvedPivotProfile[] _stationaryPivots;
        ResolvedPivotProfile[] _movingPivots;
        ResolvedLandingProfile[] _jumpLandings;
        ResolvedLandingProfile[] _fallLandings;

        public CachedLocomotionSettings(ILocomotionSettings settings)
        {
            _settings = settings;
        }

        public static void Invalidate() => _version++;

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

        public ResolvedPivotProfile[] StationaryPivots
        {
            get
            {
                RefreshLists();
                return _stationaryPivots;
            }
        }

        public ResolvedPivotProfile[] MovingPivots
        {
            get
            {
                RefreshLists();
                return _movingPivots;
            }
        }

        public ResolvedLandingProfile[] JumpLandings
        {
            get
            {
                RefreshLists();
                return _jumpLandings;
            }
        }

        public ResolvedLandingProfile[] FallLandings
        {
            get
            {
                RefreshLists();
                return _fallLandings;
            }
        }

        IPivotProfile[] ILocomotionSettings.StationaryPivots => StationaryPivots;

        IPivotProfile[] ILocomotionSettings.MovingPivots => MovingPivots;

        ILandingProfile[] ILocomotionSettings.JumpLandings => JumpLandings;

        ILandingProfile[] ILocomotionSettings.FallLandings => FallLandings;

        void RefreshLists()
        {
            if (_cachedVersion == _version)
                return;

            _stationaryPivots = ResolvedPivotProfile.From(_settings.StationaryPivots);
            _movingPivots = ResolvedPivotProfile.From(_settings.MovingPivots);
            _jumpLandings = ResolvedLandingProfile.From(_settings.JumpLandings);
            _fallLandings = ResolvedLandingProfile.From(_settings.FallLandings);

            // Only once all four are built, so a failed build is retried rather than leaving lists missing.
            _cachedVersion = _version;
        }
    }
}
