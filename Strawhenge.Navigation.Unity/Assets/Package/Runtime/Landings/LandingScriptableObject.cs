using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    [CreateAssetMenu(menuName = "Strawhenge/Navigation/Landing")]
    public class LandingScriptableObject : ScriptableObject
    {
        [SerializeField] int _id;
        
        public int Id => _id;
    }
}