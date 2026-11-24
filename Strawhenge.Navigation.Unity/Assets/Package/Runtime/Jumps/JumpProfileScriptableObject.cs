using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    [CreateAssetMenu(menuName = "Strawhenge/Navigation/Jump Profile")]
    public class JumpProfileScriptableObject : ScriptableObject, IJumpProfile
    {
        [SerializeField] JumpScriptableObject _jump;
        [SerializeField] ValueRange _speedRange;

        public int? Id => _jump != null ? _jump.Id : null;

        public ValueRange SpeedRange => _speedRange;

        // Assigning or clearing the jump asset changes whether settings include this profile.
        void OnValidate() => CachedLocomotionSettings.Invalidate();
    }
}
