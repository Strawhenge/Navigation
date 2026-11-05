using UnityEngine.AI;

namespace Strawhenge.Navigation.Unity.Destination
{
    // Test hooks. Call them before Awake (while the GameObject is inactive): the controller is built
    // once, so later calls have no effect.
    public sealed partial class DestinationScript
    {
        internal void SetNavMeshAgent(NavMeshAgent navMeshAgent) => _navMeshAgent = navMeshAgent;

        internal void SetModes(INavMeshAgentModes modes) => _modesOverride = modes;

        internal void SetSettings(IDestinationSettings settings) => _settingsOverride = settings;
    }
}
