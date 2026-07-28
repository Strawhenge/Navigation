using System.Collections.Generic;
using System.Linq;
using UnityEngine.AI;

namespace Strawhenge.Navigation.Unity.Destination
{
    public class AgentPriorityManager
    {
        readonly NavMeshAgent[] _navMeshAgents;
        Queue<NavMeshAgent> _awaitingPriority = new Queue<NavMeshAgent>();

        public AgentPriorityManager() : this(100)
        {
        }

        public AgentPriorityManager(int max)
        {
            _navMeshAgents = new NavMeshAgent[max];
        }

        public void ReserveLowPriority(NavMeshAgent agent)
        {
            for (int i = 0; i < _navMeshAgents.Length; i++)
            {
                if (_navMeshAgents[i] == null)
                {
                    _navMeshAgents[i] = agent;
                    agent.avoidancePriority = i;

                    return;
                }
            }

            agent.avoidancePriority = 0;
            _awaitingPriority.Enqueue(agent);
        }

        public void ReserveHighPriority(NavMeshAgent agent)
        {
            for (int i = _navMeshAgents.Length - 1; i >= 0; i--)
            {
                if (_navMeshAgents[i] == null)
                {
                    _navMeshAgents[i] = agent;
                    agent.avoidancePriority = i;

                    return;
                }
            }

            agent.avoidancePriority = _navMeshAgents.Length - 1;
            _awaitingPriority.Enqueue(agent);
        }

        public void ReleasePriority(NavMeshAgent agent)
        {
            if (_awaitingPriority.Contains(agent))
            {
                var queue = _awaitingPriority.ToList();
                queue.Remove(agent);
                _awaitingPriority = new Queue<NavMeshAgent>(queue);
            }

            for (int i = 0; i < _navMeshAgents.Length; i++)
            {
                if (_navMeshAgents[i] == agent)
                {
                    _navMeshAgents[i] = null;

                    while (_awaitingPriority.Any())
                    {
                        var next = _awaitingPriority.Dequeue();

                        if (next != null)
                        {
                            next.avoidancePriority = i;
                            _navMeshAgents[i] = next;

                            return;
                        }
                    }

                    return;
                }
            }
        }

        public void Clear()
        {
            _awaitingPriority.Clear();

            for (var i = 0; i < _navMeshAgents.Length; i++)
                _navMeshAgents[i] = null;
        }
    }
}