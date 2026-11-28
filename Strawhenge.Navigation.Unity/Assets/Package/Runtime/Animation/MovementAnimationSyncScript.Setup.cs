using ILogger = Strawhenge.Common.Logging.ILogger;

namespace Strawhenge.Navigation.Unity
{
    // Test hooks. Call them before Awake (while the GameObject is inactive); Awake keeps what was set.
    public partial class MovementAnimationSyncScript
    {
        ILogger _loggerOverride;

        internal void SetLogger(ILogger logger) => _loggerOverride = logger;

        internal void SetMovementSourceManager(MovementSourceManagerScript movementSourceManager) =>
            _movementSourceManager = movementSourceManager;
    }
}
