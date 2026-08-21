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

        public int Id => _landing.Id;

        public FloatRange FallDistanceRange => _fallDistanceRange.Value;

        public FloatRange SpeedRange => _speedRange.Value;
    }
}