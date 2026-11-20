using ILogger = Strawhenge.Common.Logging.ILogger;

namespace Strawhenge.Navigation.Unity
{
    // Test hooks. Call them before Awake (while the GameObject is inactive); Awake keeps what was set.
    public sealed partial class LocomotionScript
    {
        internal void SetMotor(CharacterMotorScript motor) => _motor = motor;

        internal void SetSettings(ILocomotionSettings settings)
        {
            _settingsOverride = settings;
            _controller?.SetSettings(new CachedLocomotionSettings(settings));
        }

        internal void SetLogger(ILogger logger) => _loggerOverride = logger;
    }
}
