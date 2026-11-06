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
        bool _isSuspended;

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

            ReservePriority(leisurely);

            NavMeshAgent.areaMask = GetAreaMask(leisurely);
            IsLeisurely = leisurely;
        }

        public void Disable()
        {
            NavMeshAgent.enabled = false;
            _isSuspended = false;

            if (!_hasPriority)
                return;

            _priorityManager.ReleasePriority(NavMeshAgent);
            _hasPriority = false;
        }

        // DestinationScript was disabled mid-trip, on its own or with its GameObject: pause by disabling
        // the agent, which stops it, and free the avoidance priority slot. Resume undoes both. Unity drops
        // a disabled agent's path, so the current phase sets the agent up again (Phase.OnAgentReactivated).
        // Nothing to do while idle, when the agent is already disabled.
        public void Suspend()
        {
            if (!NavMeshAgent.enabled)
                return;

            NavMeshAgent.enabled = false;
            _isSuspended = true;

            if (!_hasPriority)
                return;

            _priorityManager.ReleasePriority(NavMeshAgent);
            _hasPriority = false;
        }

        // Re-enabled: enable the agent again and reserve a slot for the mode in use, unless something
        // already has.
        public void Resume()
        {
            if (!_isSuspended)
                return;

            _isSuspended = false;
            NavMeshAgent.enabled = true;

            if (!_hasPriority)
                ReservePriority(IsLeisurely);
        }

        void ReservePriority(bool leisurely)
        {
            // Release first so re-enabling (or switching mode) never holds two slots.
            if (_hasPriority)
                _priorityManager.ReleasePriority(NavMeshAgent);

            if (leisurely)
                _priorityManager.ReserveLowPriority(NavMeshAgent);
            else
                _priorityManager.ReserveHighPriority(NavMeshAgent);

            _hasPriority = true;
            _isSuspended = false;
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