using Strawhenge.Common.Unity;
using Strawhenge.Common.Unity.Serialization;
using System;
using UnityEngine;
using ILogger = Strawhenge.Common.Logging.ILogger;

namespace Strawhenge.Navigation.Unity
{
    // Wires up and steps a LocomotionController, which holds the logic, and the motor that moves the character.
    public sealed partial class LocomotionScript : MonoBehaviour
    {
        [SerializeField] CharacterMotorScript _motor;

        // Characters set up before motors existed. Only read when no motor is set or found, to add a
        // CharacterControllerMotorScript for it. Same field name, so existing prefabs keep the reference.
        [SerializeField, HideInInspector] CharacterController _characterController;

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
        ICharacterMotor _characterMotor;
        ILocomotionSettings _settingsOverride;
        ILogger _loggerOverride;

        // Created on first use, so consumers can subscribe to events before this Awake runs.
        internal LocomotionController Controller => _controller ??= Create();

        // For the inspector: null until created, so the Editor never creates one.
        internal LocomotionController ControllerIfCreated => _controller;

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
                _controller.SetSettings(new CachedLocomotionSettings(ResolveSettings()));
        }

        void OnEnable()
        {
            Controller.Activate();
            IsActiveChanged?.Invoke(true);
        }

        // While disabled, MovementSourceManagerScript routes the Animator's completion calls (CompletePivot,
        // CompleteLanding, TriggerJump) to another movement source, so Deactivate also clears everything
        // waiting on them. It also makes the steps of an externally stepped motor do nothing.
        void OnDisable()
        {
            Controller.Deactivate();
            IsActiveChanged?.Invoke(false);
        }

        void Update()
        {
            if (_characterMotor.UpdateMode == MotorUpdateMode.Update)
                Controller.Step(Time.deltaTime);
        }

        void FixedUpdate()
        {
            if (_characterMotor.UpdateMode == MotorUpdateMode.FixedUpdate)
                Controller.Step(Time.fixedDeltaTime);
        }

        public event Action JumpTriggerRequested
        {
            add => Controller.JumpTriggerRequested += value;
            remove => Controller.JumpTriggerRequested -= value;
        }

        public event Action JumpBegan
        {
            add => Controller.JumpBegan += value;
            remove => Controller.JumpBegan -= value;
        }

        public event Action JumpEnded
        {
            add => Controller.JumpEnded += value;
            remove => Controller.JumpEnded -= value;
        }

        public event Action<int> PivotRequested
        {
            add => Controller.PivotRequested += value;
            remove => Controller.PivotRequested -= value;
        }

        public event Action FallBegan
        {
            add => Controller.FallBegan += value;
            remove => Controller.FallBegan -= value;
        }

        public event Action FallEnded
        {
            add => Controller.FallEnded += value;
            remove => Controller.FallEnded -= value;
        }

        public event Action<int> LandingRequested
        {
            add => Controller.LandingRequested += value;
            remove => Controller.LandingRequested -= value;
        }

        // Raised here rather than by the controller: it reports this component being enabled or disabled.
        public event Action<bool> IsActiveChanged;

        public Vector3 CurrentVelocity => Controller.CurrentVelocity;

        public bool IsActive => isActiveAndEnabled;

        public bool Walk
        {
            get => Controller.Walk;
            set => Controller.Walk = value;
        }

        public bool Sprint
        {
            get => Controller.Sprint;
            set => Controller.Sprint = value;
        }

        public bool Strafe
        {
            get => Controller.Strafe;
            set => Controller.Strafe = value;
        }

        public void Move(Vector3 input) => Controller.Move(input);

        public void Jump() => Controller.Jump();

        public void TriggerJump() => Controller.TriggerJump();

        public void CompletePivot(Quaternion rotationDelta) => Controller.CompletePivot(rotationDelta);

        public void CompleteLanding() => Controller.CompleteLanding();

        LocomotionController Create()
        {
            _characterMotor = ResolveMotor();

            var logger = _loggerOverride;
            if (logger == null)
                logger = _logger != null ? _logger.Logger : new UnityLogger(gameObject);

            var controller = new LocomotionController(
                _characterMotor,
                new CachedLocomotionSettings(ResolveSettings()),
                _animatorWaitTimeout,
                logger);

            // A motor that steps itself (e.g. from a third-party controller's callbacks) calls BeginStep /
            // EndStep, so neither Update nor FixedUpdate steps for it.
            if (_characterMotor is IExternallySteppedMotor externallySteppedMotor)
                externallySteppedMotor.SetStepper(controller);

            return controller;
        }

        ICharacterMotor ResolveMotor()
        {
            if (_motor == null)
            {
                _motor = GetComponentInChildren<CharacterMotorScript>(includeInactive: true);

                if (_motor != null)
                {
                    Debug.LogWarning(
                        $"'{nameof(_motor)}' not set - using the {_motor.GetType().Name} on '{_motor.name}'.",
                        this);
                }
                else
                {
                    _motor = AddMotorForCharacterController();
                }
            }

            if (_motor == null)
            {
                Debug.LogError(
                    $"'{nameof(_motor)}' not set, and no {nameof(CharacterMotorScript)} or CharacterController " +
                    "found in hierarchy - the character won't move.",
                    this);

                return NullCharacterMotor.Instance;
            }

            return _motor.Motor;
        }

        // Characters set up before motors existed have a CharacterController and no motor.
        CharacterMotorScript AddMotorForCharacterController()
        {
            if (_characterController == null)
                _characterController = GetComponentInChildren<CharacterController>(includeInactive: true);

            if (_characterController == null)
                return null;

            Debug.LogWarning(
                $"'{nameof(_motor)}' not set - adding a {nameof(CharacterControllerMotorScript)} for the " +
                "CharacterController. Add one and assign it to remove this warning.",
                this);

            var motor = _characterController.gameObject.AddComponent<CharacterControllerMotorScript>();
            motor.SetCharacterController(_characterController);
            return motor;
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
