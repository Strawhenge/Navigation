using Strawhenge.Common.Ranges;
using Strawhenge.Common.Unity.Serialization;
using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    [Serializable]
    public class SerializedLandingProfile
    {
        [SerializeField] LandingScriptableObject _landing;
        [SerializeField] SerializedFloatRange _fallDistanceRange;
        [SerializeField] SerializedFloatRange _speedRange;

        public int Id => _landing.Id;

        public FloatRange FallDistanceRange => _fallDistanceRange.Value;

        public FloatRange SpeedRange => _speedRange.Value;
    }
}