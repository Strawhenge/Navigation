using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    [CreateAssetMenu(menuName = "Strawhenge/Navigation/NavMeshAgent Modes")]
    public class NavMeshAgentModesScriptableObject : ScriptableObject, INavMeshAgentModes
    {
        [SerializeField] SerializedNavMeshAgentMode _default = new();
        [SerializeField] SerializedNavMeshAgentMode _leisurely = new();

        public INavMeshAgentMode Default => _default;

        public INavMeshAgentMode Leisurely => _leisurely;
    }
}
