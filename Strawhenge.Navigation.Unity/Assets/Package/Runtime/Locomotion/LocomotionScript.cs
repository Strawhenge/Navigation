using Strawhenge.Common.Unity;
using Strawhenge.Common.Unity.Helpers;
using Strawhenge.Common.Unity.Serialization;
using UnityEngine;
using ILogger = Strawhenge.Common.Logging.ILogger;

namespace Strawhenge.Navigation.Unity
{
    // Wires up and steps a LocomotionController, which holds the logic, and the motor that moves the character.
    public sealed partial class LocomotionScript : MonoBehaviour
    {
        [SerializeField] CharacterMotorScript _motor;

        [SerializeField] SerializedSource<
            ILocomotionSettings,
            SerializedLocomotionSettings,
            LocomotionSettingsScriptableObject> _settings;

        [Tooltip("Longest wait for a pivot or landing to be completed, or a deferred jump to be launched, " +
                 "before carrying on without it. Must be longer than the longest pivot or landing animation.")]
        [SerializeField] float _animatorWaitTimeout = 2f;

        // Optional: without one, the controller logs through a UnityLogger on this GameObject.
        [SerializeField] LoggerScript _logger;

        LocomotionController _controller;
        MotorUpdateMode _updateMode;

        // The controller's logger, also given to its settings caches. Set when the controller is created.
        ILogger _controllerLogger;

        // Where consumers make requests and subscribe to events. Created on first use, so they can do that
        // before this Awake runs.
        public LocomotionController LocomotionController => _controller ??= Create();

        void Awake()
        {
            _controller ??= Create();
        }

        // Inline settings edits, and swapping the settings asset, apply in play mode. Settings given to
        // SetSettings are kept.
        void OnValidate()
        {
            CachedLocomotionSettings.Invalidate();

            if (Application.isPlaying && _controller != null && _settingsOverride == null)
                _controller.SetSettings(new CachedLocomotionSettings(ResolveSettings(), _controllerLogger));
        }

        void OnEnable()
        {
            LocomotionController.Activate();
        }

        // While disabled, MovementSourceManagerScript routes the Animator's completion calls (CompletePivot,
        // CompleteLanding, TriggerJump) to another movement source, so Deactivate also clears everything
        // waiting on them. It also makes the steps of an externally stepped motor do nothing.
        void OnDisable()
        {
            LocomotionController.Deactivate();
        }

        // _controller is always created by now (in Awake at the latest).
        void Update()
        {
            if (_updateMode == MotorUpdateMode.Update)
                _controller.Step(Time.deltaTime);
        }

        void FixedUpdate()
        {
            if (_updateMode == MotorUpdateMode.FixedUpdate)
                _controller.Step(Time.fixedDeltaTime);
        }

        LocomotionController Create()
        {
            var motor = ResolveMotor();
            _updateMode = motor.UpdateMode;

            _controllerLogger = _loggerOverride ?? (_logger != null
                ? _logger.Logger
                : new UnityLogger(gameObject));

            var controller = new LocomotionController(
                motor,
                new CachedLocomotionSettings(ResolveSettings(), _controllerLogger),
                _animatorWaitTimeout,
                _controllerLogger);

            // A motor that steps itself (e.g. from a third-party controller's callbacks) calls BeginStep /
            // EndStep, so neither Update nor FixedUpdate steps for it.
            if (motor is IExternallySteppedMotor externallySteppedMotor)
                externallySteppedMotor.SetStepper(controller);

            return controller;
        }

        // Without a motor the character stays where it is.
        ICharacterMotor ResolveMotor()
        {
            ComponentRefHelper.EnsureHierarchyComponent(ref _motor, nameof(_motor), this);

            return _motor != null
                ? _motor.Motor
                : NullCharacterMotor.Instance;
        }

        ILocomotionSettings ResolveSettings()
        {
            if (_settingsOverride != null)
                return _settingsOverride;

            if (_settings != null && _settings.TryGetValue(out var settings))
                return settings;

            Debug.LogWarning($"'{nameof(_settings)}' not set - using defaults.", this);
            return DefaultLocomotionSettings.Instance;
        }
    }
}
