using UnityEngine.AI;

namespace Strawhenge.Navigation.Unity.Destination
{
    public interface INavMeshAgentModes
    {
        void Default(NavMeshAgent agent);

        void Leisurely(NavMeshAgent agent);
    }
}