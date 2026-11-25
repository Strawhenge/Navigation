using ILogger = Strawhenge.Common.Logging.ILogger;

namespace Strawhenge.Navigation.Unity
{
    // How LocomotionScript reads its settings. Values are passed through, so inspector edits apply at once.
    // The profile lists are cached, because they're read every frame and resolving them allocates.
    // See ProfileCacheVersion for when they're rebuilt.
    sealed class CachedLocomotionSettings : ILocomotionSettings
    {
        readonly ILocomotionSettings _settings;
        readonly ILogger _logger;

        int _cachedVersion;
        IPivotProfile[] _stationaryPivots;
        IPivotProfile[] _startPivots;
        IPivotProfile[] _movingPivots;
        IJumpProfile[] _jumps;
        IStrafeJumpProfile[] _strafeJumps;
        ILandingProfile[] _jumpLandings;
        ILandingProfile[] _fallLandings;

        // logger: warns about profiles that are ignored because they can't be used.
        public CachedLocomotionSettings(ILocomotionSettings settings, ILogger logger)
        {
            _settings = settings;
            _logger = logger;
        }

        // Rebuilds every settings cache, destination ones included.
        public static void Invalidate() => ProfileCacheVersion.Invalidate();

        public float WalkSpeed => _settings.WalkSpeed;

        public float RunSpeed => _settings.RunSpeed;

        public float SprintSpeed => _settings.SprintSpeed;

        public float Acceleration => _settings.Acceleration;

        public float Deceleration => _settings.Deceleration;

        public float TurnSpeed => _settings.TurnSpeed;

        public float SharpTurnAngle => _settings.SharpTurnAngle;

        public float ReversalAngle => _settings.ReversalAngle;

        public float StationaryPivotMaxSpeed => _settings.StationaryPivotMaxSpeed;

        public float StationaryPivotDecisionTime => _settings.StationaryPivotDecisionTime;

        public float JumpHeight => _settings.JumpHeight;

        public float CoyoteTime => _settings.CoyoteTime;

        public float Gravity => _settings.Gravity;

        public float FallStartSpeed => _settings.FallStartSpeed;

        public float FallDistance => _settings.FallDistance;

        public bool AllowAirborneRotation => _settings.AllowAirborneRotation;

        public AirborneMovement AirborneMovement => _settings.AirborneMovement;

        public IPivotProfile[] StationaryPivots
        {
            get
            {
                RefreshLists();
                return _stationaryPivots;
            }
        }

        public IPivotProfile[] StartPivots
        {
            get
            {
                RefreshLists();
                return _startPivots;
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
            _startPivots = ProfileSources.Filter(
                _settings.StartPivots, PivotProfileExtensions.FindProblem, nameof(StartPivots), _logger);
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
