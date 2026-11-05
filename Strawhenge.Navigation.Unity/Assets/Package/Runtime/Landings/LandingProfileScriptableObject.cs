using Strawhenge.Common.Ranges;
using Strawhenge.Common.Unity.Serialization;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    [CreateAssetMenu(menuName = "Strawhenge/Navigation/Landing Profile")]
    public class LandingProfileScriptableObject : ScriptableObject, ILandingProfile
    {
        [SerializeField] LandingScriptableObject _landing;
        [SerializeField] SerializedFloatRange _fallDistanceRange;
        [SerializeField] SerializedFloatRange _speedRange;

        public int? Id => _landing != null ? _landing.Id : null;

        // Assigning or clearing the landing asset changes whether settings include this profile.
        void OnValidate() => CachedLocomotionSettings.Invalidate();

        public FloatRange FallDistanceRange => _fallDistanceRange.Value;

        public FloatRange SpeedRange => _speedRange.Value;
    }
}