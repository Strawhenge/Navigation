using UnityEngine.AI;

namespace Strawhenge.Navigation.Unity.Destination
{
    public class NullNavMeshAgentModes : INavMeshAgentModes
    {
        public static NullNavMeshAgentModes Instance { get; } = new();

        NullNavMeshAgentModes()
        {
        }

        public void Default(NavMeshAgent agent)
        {
        }

        public void Leisurely(NavMeshAgent agent)
        {
        }
    }
}