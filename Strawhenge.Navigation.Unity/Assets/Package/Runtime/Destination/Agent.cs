using UnityEngine;
using UnityEngine.AI;

namespace Strawhenge.Navigation.Unity.Destination
{
    class Agent
    {
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

        public void Enable(bool leisurely)
        {
            NavMeshAgent.enabled = true;

            // Release first so re-enabling (or switching mode) never holds two slots.
            if (_hasPriority)
                _priorityManager.ReleasePriority(NavMeshAgent);

            if (leisurely)
            {
                _priorityManager.ReserveLowPriority(NavMeshAgent);
                _agentModes.Leisurely(NavMeshAgent);
            }
            else
            {
                _priorityManager.ReserveHighPriority(NavMeshAgent);
                _agentModes.Default(NavMeshAgent);
            }

            _hasPriority = true;
        }

        public void Disable()
        {
            NavMeshAgent.enabled = false;

            if (!_hasPriority)
                return;

            _priorityManager.ReleasePriority(NavMeshAgent);
            _hasPriority = false;
        }

        public Vector3 GetCurrentPosition() =>
            NavMeshAgent.transform.position;

        public void SetCurrentPosition(Vector3 position) =>
            NavMeshAgent.transform.position = position;
    }
}