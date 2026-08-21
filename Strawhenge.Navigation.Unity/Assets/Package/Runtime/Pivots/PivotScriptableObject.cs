using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    // TODO: Remove this from asset menu and generate via editor tool.
    [CreateAssetMenu(menuName = "Strawhenge/Navigation/Pivot")]
    public class PivotScriptableObject : ScriptableObject
    {
        [SerializeField] int _id;

        public int Id => _id;
    }
}