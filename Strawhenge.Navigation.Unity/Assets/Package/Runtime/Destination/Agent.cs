using UnityEngine;
using UnityEngine.AI;

namespace Strawhenge.Navigation.Unity.Destination
{
    class Agent
    {
        // Reused to avoid garbage; it is never assigned to the agent. Not thread safe.
        static readonly NavMeshPath AccessibilityPath = new();

        readonly INavMeshAgentModes _agentModes;
        readonly AgentPriorityManager _priorityManager;
        bool _hasPriority;

        public Agent(NavMeshAgent agent, INavMeshAgentModes agentModes, AgentPriorityManager priorityManager)
        {
            _agentModes = agentModes;
            _priorityManager = priorityManager;

            NavMeshAgent = agent;
        }

        public NavMeshAgent NavMeshAgent { get; }

        // The mode last applied by Enable.
        public bool IsLeisurely { get; private set; }

        public void Enable(bool leisurely)
        {
            NavMeshAgent.enabled = true;

            // Release first so re-enabling (or switching mode) never holds two slots.
            if (_hasPriority)
                _priorityManager.ReleasePriority(NavMeshAgent);

            if (leisurely)
                _priorityManager.ReserveLowPriority(NavMeshAgent);
            else
                _priorityManager.ReserveHighPriority(NavMeshAgent);

            _hasPriority = true;

            NavMeshAgent.areaMask = GetAreaMask(leisurely);
            IsLeisurely = leisurely;
        }

        public void Disable()
        {
            NavMeshAgent.enabled = false;

            if (!_hasPriority)
                return;

            _priorityManager.ReleasePriority(NavMeshAgent);
            _hasPriority = false;
        }

        public int GetAreaMask(bool leisurely) =>
            leisurely ? _agentModes.LeisurelyAreaMask : _agentModes.DefaultAreaMask;

        // Asks the NavMesh directly, so the agent is never enabled or moved and nothing is logged.
        // Only a complete path counts as accessible.
        public bool IsLocationAccessible(Vector3 location, bool leisurely)
        {
            var filter = new NavMeshQueryFilter
            {
                agentTypeID = NavMeshAgent.agentTypeID,
                areaMask = GetAreaMask(leisurely)
            };

            // Start from the nearest NavMesh point within one agent height of the character.
            if (!NavMesh.SamplePosition(GetCurrentPosition(), out var start, NavMeshAgent.height, filter))
                return false;

            return NavMesh.CalculatePath(start.position, location, filter, AccessibilityPath) &&
                   AccessibilityPath.status == NavMeshPathStatus.PathComplete;
        }

        public Vector3 GetCurrentPosition() =>
            NavMeshAgent.transform.position;

        public void SetCurrentPosition(Vector3 position) =>
            NavMeshAgent.transform.position = position;
    }
}