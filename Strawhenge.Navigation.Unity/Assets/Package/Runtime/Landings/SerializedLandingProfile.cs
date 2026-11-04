using Strawhenge.Common.Ranges;
using Strawhenge.Common.Unity.Serialization;
using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    [Serializable]
    public class SerializedLandingProfile : ILandingProfile
    {
        [SerializeField] LandingScriptableObject _landing;
        [SerializeField] SerializedFloatRange _fallDistanceRange;
        [SerializeField] SerializedFloatRange _speedRange;

        public int? Id => _landing != null ? _landing.Id : null;

        public FloatRange FallDistanceRange => _fallDistanceRange.Value;

        public FloatRange SpeedRange => _speedRange.Value;
    }
}