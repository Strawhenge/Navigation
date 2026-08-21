using Strawhenge.Common.Ranges;
using Strawhenge.Common.Unity.Serialization;
using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    [Serializable]
    public class SerializedLandingProfile
    {
        [SerializeField] int _id;
        [SerializeField] SerializedFloatRange _fallDistanceRange;
        [SerializeField] SerializedFloatRange _speedRange;
        
        public int Id => _id;
        
        public FloatRange FallDistanceRange => _fallDistanceRange.Value;
    
        public FloatRange SpeedRange => _speedRange.Value;
    }
}