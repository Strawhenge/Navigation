using Strawhenge.Common.Unity.Serialization;
using UnityEngine;
using UnityEngine.AI;

namespace Strawhenge.Navigation.Unity.Destination
{
    [CreateAssetMenu(menuName = "Strawhenge/Navigation/NavMeshAgent Modes")]
    public class NavMeshAgentModesScriptableObject : ScriptableObject, INavMeshAgentModes
    {
        [SerializeField] NavMeshAreaMask _default;
        [SerializeField] NavMeshAreaMask _leisurely;

        public void Default(NavMeshAgent agent)
        {
            agent.areaMask = _default.Value;
        }

        public void Leisurely(NavMeshAgent agent)
        {
            agent.areaMask = _leisurely.Value;
        }
    }
}