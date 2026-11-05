using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    [CreateAssetMenu(menuName = "Strawhenge/Navigation/Landing Profile")]
    public class LandingProfileScriptableObject : ScriptableObject, ILandingProfile
    {
        [SerializeField] LandingScriptableObject _landing;
        [SerializeField] ValueRange _fallDistanceRange;
        [SerializeField] ValueRange _speedRange;

        public int? Id => _landing != null ? _landing.Id : null;

        public ValueRange FallDistanceRange => _fallDistanceRange;

        public ValueRange SpeedRange => _speedRange;

        // Assigning or clearing the landing asset changes whether settings include this profile.
        void OnValidate() => CachedLocomotionSettings.Invalidate();
    }
}