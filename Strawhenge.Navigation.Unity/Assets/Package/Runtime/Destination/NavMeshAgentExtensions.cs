using UnityEngine;
using UnityEngine.AI;

namespace Strawhenge.Navigation.Unity.Destination
{
    static class NavMeshAgentExtensions
    {
        // Reusing the instance to avoid garbage, as it is never assigned to the agent.
        // Note: not thread safe.
        static readonly NavMeshPath Path = new();

        public static bool IsLocationAccessible(this NavMeshAgent agent, Vector3 location)
        {
            var agentEnabled = agent.enabled;
            agent.enabled = true;

            if (!agent.isOnNavMesh)
            {
                agent.enabled = agentEnabled;
                return false;
            }

            agent.CalculatePath(location, Path);
            var result = Path.status == NavMeshPathStatus.PathComplete;

            agent.enabled = agentEnabled;
            return result;
        }
    }
}