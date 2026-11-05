using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    [Serializable]
    public class SerializedLandingProfile : ILandingProfile
    {
        [SerializeField] LandingScriptableObject _landing;
        [SerializeField] ValueRange _fallDistanceRange;
        [SerializeField] ValueRange _speedRange;

        public int? Id => _landing != null ? _landing.Id : null;

        public ValueRange FallDistanceRange => _fallDistanceRange;

        public ValueRange SpeedRange => _speedRange;
    }
}