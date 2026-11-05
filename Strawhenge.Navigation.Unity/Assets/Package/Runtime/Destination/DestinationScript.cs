using Strawhenge.Common.Unity.Helpers;
using Strawhenge.Common.Unity.Serialization;
using UnityEngine;
using UnityEngine.AI;

namespace Strawhenge.Navigation.Unity.Destination
{
    public sealed class DestinationScript : MonoBehaviour
    {
        [SerializeField] NavMeshAgent _navMeshAgent;
        [SerializeField] NavMeshAgentModesScriptableObject _modes;
        [SerializeField] DestinationContextScript _context;

        [SerializeField] SerializedSource<
            IDestinationSettings,
            SerializedDestinationSettings,
            DestinationSettingsScriptableObject> _settings;

        DestinationController _destinationController;
        INavMeshAgentModes _modesOverride;
        IDestinationSettings _settingsOverride;

        public DestinationController DestinationController => _destinationController ??= Create();

        void Awake()
        {
            _destinationController ??= Create();
        }

        // Disabling this component, or deactivating its GameObject, mid-trip pauses the trip: the agent is
        // disabled and its avoidance priority slot freed. Enabling it again takes a slot back, sets the
        // agent up again and carries on with the trip.
        void OnEnable()
        {
            DestinationController.Resume();
        }

        void OnDisable()
        {
            DestinationController.Suspend();
        }

        void Update()
        {
            DestinationController.Update(Time.deltaTime);
        }

        // Test hooks. Call them before Awake (while the GameObject is inactive): the controller is built
        // once, so later calls have no effect.
        internal void SetNavMeshAgent(NavMeshAgent navMeshAgent) => _navMeshAgent = navMeshAgent;

        internal void SetModes(INavMeshAgentModes modes) => _modesOverride = modes;

        internal void SetSettings(IDestinationSettings settings) => _settingsOverride = settings;

        // Read live rather than copied, so edits to the settings apply straight away.
        IDestinationSettings ResolveSettings()
        {
            if (_settingsOverride != null)
                return _settingsOverride;

            if (_settings != null && _settings.TryGetValue(out var settings))
                return settings;

            Debug.LogWarning($"'{nameof(_settings)}' not set - using defaults.", this);
            return DefaultDestinationSettings.Instance;
        }

        DestinationController Create()
        {
            ComponentRefHelper.EnsureRootHierarchyComponent(ref _navMeshAgent, nameof(_navMeshAgent), this);

            IDestinationContext context = _context != null
                ? _context
                : NullDestinationContext.Instance;

            INavMeshAgentModes modes = _modesOverride;
            if (modes == null && _modes != null)
                modes = _modes;

            if (modes == null)
            {
                Debug.LogWarning($"Missing '{nameof(_modes)}'.", this);
                modes = new FixedNavMeshAgentModes(_navMeshAgent.areaMask);
            }

            var agent = new Agent(
                _navMeshAgent,
                modes,
                SingleAgentPriorityManager.Instance);

            return new DestinationController(context, agent, ResolveSettings());
        }
    }
}