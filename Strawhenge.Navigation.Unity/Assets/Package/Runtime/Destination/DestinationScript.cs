using Strawhenge.Common.Unity.Helpers;
using UnityEngine;
using UnityEngine.AI;

namespace Strawhenge.Navigation.Unity.Destination
{
    public sealed class DestinationScript : MonoBehaviour
    {
        [SerializeField] NavMeshAgent _navMeshAgent;
        [SerializeField] NavMeshAgentModesScriptableObject _modes;
        [SerializeField] DestinationContextScript _context;

        DestinationController _destinationController;

        public DestinationController DestinationController => _destinationController ??= Create();

        void Awake()
        {
            _destinationController ??= Create();
        }

        void Update()
        {
            DestinationController.Update(Time.deltaTime);
        }

        DestinationController Create()
        {
            ComponentRefHelper.EnsureRootHierarchyComponent(ref _navMeshAgent, nameof(_navMeshAgent), this);

            IDestinationContext context = _context != null
                ? _context
                : NullDestinationContext.Instance;

            INavMeshAgentModes modes = _modes;
            if (modes == null)
            {
                Debug.LogWarning($"Missing '{nameof(_modes)}'.", this);
                modes = NullNavMeshAgentModes.Instance;
            }

            var agent = new Agent(
                _navMeshAgent,
                modes,
                SingleAgentPriorityManager.Instance);

            return new DestinationController(context, agent);
        }
    }
}