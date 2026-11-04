using Strawhenge.Common.Unity.Serialization;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    [CreateAssetMenu(menuName = "Strawhenge/Navigation/NavMeshAgent Modes")]
    public class NavMeshAgentModesScriptableObject : ScriptableObject, INavMeshAgentModes
    {
        [SerializeField] NavMeshAreaMask _default;
        [SerializeField] NavMeshAreaMask _leisurely;

        public int DefaultAreaMask => _default.Value;

        public int LeisurelyAreaMask => _leisurely.Value;
    }
}
