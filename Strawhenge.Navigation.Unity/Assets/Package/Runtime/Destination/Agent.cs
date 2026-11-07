using UnityEngine;
using UnityEngine.AI;

namespace Strawhenge.Navigation.Unity.Destination
{
    class Agent
    {
        // Reused to avoid garbage; it is never assigned to the agent. Not thread safe.
        static readonly NavMeshPath AccessibilityPath = new();

        // Unity supports 32 NavMesh areas.
        const int AreaCount = 32;

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

            ApplyMode(GetMode(leisurely));
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

            // Area costs aren't saved with the agent, so they're applied again rather than trusted to
            // survive the disable.
            ApplyMode(GetMode(IsLeisurely));

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

        // The single place a mode is chosen.
        INavMeshAgentMode GetMode(bool leisurely) =>
            leisurely ? _agentModes.Leisurely : _agentModes.Default;

        public int GetAreaMask(bool leisurely) =>
            GetMode(leisurely).AreaMask;

        // Sets every area, not only the ones the mode overrides: the agent keeps the costs it's given, so
        // an area made costly by the previous mode would otherwise stay costly.
        void ApplyMode(INavMeshAgentMode mode)
        {
            NavMeshAgent.areaMask = mode.AreaMask;

            for (var area = 0; area < AreaCount; area++)
                NavMeshAgent.SetAreaCost(area, GetAreaCost(mode, area));
        }

        static float GetAreaCost(INavMeshAgentMode mode, int area) =>
            mode.TryGetAreaCost(area, out var cost) ? cost : NavMesh.GetAreaCost(area);

        // Asks the NavMesh directly, so the agent is never enabled or moved and nothing is logged.
        // Only a complete path counts as accessible, and only to the exact location: this never falls
        // back to the nearest NavMesh point, as an approximate request does.
        public bool IsLocationAccessible(Vector3 location, bool leisurely)
        {
            var filter = CreatePathQueryFilter(leisurely);

            // Start from the nearest NavMesh point within one agent height of the character.
            if (!NavMesh.SamplePosition(GetCurrentPosition(), out var start, NavMeshAgent.height, filter))
                return false;

            return NavMesh.CalculatePath(start.position, location, filter, AccessibilityPath) &&
                   AccessibilityPath.status == NavMeshPathStatus.PathComplete;
        }

        // The nearest point on the NavMesh to location, within maxDistance, using the mode's area mask.
        // A query only, like IsLocationAccessible: the agent is never enabled, moved or reconfigured.
        public bool TryGetNearestNavMeshPoint(Vector3 location, bool leisurely, float maxDistance, out Vector3 point)
        {
            if (NavMesh.SamplePosition(location, out var hit, maxDistance, CreateQueryFilter(leisurely)))
            {
                point = hit.position;
                return true;
            }

            point = location;
            return false;
        }

        NavMeshQueryFilter CreateQueryFilter(bool leisurely) => new()
        {
            agentTypeID = NavMeshAgent.agentTypeID,
            areaMask = GetAreaMask(leisurely)
        };

        // With the mode's area costs too. They don't change whether a path is complete, but the query then
        // searches the way the agent would.
        NavMeshQueryFilter CreatePathQueryFilter(bool leisurely)
        {
            var mode = GetMode(leisurely);
            var filter = CreateQueryFilter(leisurely);

            for (var area = 0; area < AreaCount; area++)
                filter.SetAreaCost(area, GetAreaCost(mode, area));

            return filter;
        }

        // Warping puts the agent back on the NavMesh with the project's area costs (the mask is kept, as
        // it's saved on the component), so the mode's costs are applied again. Warp the agent through
        // here, never NavMeshAgent.Warp directly.
        public bool Warp(Vector3 position)
        {
            var warped = NavMeshAgent.Warp(position);
            ApplyMode(GetMode(IsLeisurely));
            return warped;
        }

        public Vector3 GetCurrentPosition() =>
            NavMeshAgent.transform.position;

        public void SetCurrentPosition(Vector3 position) =>
            NavMeshAgent.transform.position = position;
    }
}