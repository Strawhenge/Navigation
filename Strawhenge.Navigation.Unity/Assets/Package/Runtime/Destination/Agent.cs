using UnityEngine;
using UnityEngine.AI;

namespace Strawhenge.Navigation.Unity.Destination
{
    class Agent
    {
        readonly INavMeshAgentModes _agentModes;
        readonly AgentPriorityManager _priorityManager;

        public Agent(NavMeshAgent agent, INavMeshAgentModes agentModes, AgentPriorityManager priorityManager)
        {
            _agentModes = agentModes;
            _priorityManager = priorityManager;

            NavMeshAgent = agent;
            Rigidbody = agent.GetComponent<Rigidbody>();
        }

        public NavMeshAgent NavMeshAgent { get; }

        public Rigidbody Rigidbody { get; }

        public void Enable(bool leisurely)
        {
            NavMeshAgent.enabled = true;

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
        }

        public void Disable()
        {
            NavMeshAgent.enabled = false;
            _priorityManager.ReleasePriority(NavMeshAgent);
        }
    }
}