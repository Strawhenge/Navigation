using Strawhenge.Common.Ranges;
using Strawhenge.Common.Unity.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    [Serializable]
    public class SerializedPivotProfile : IPivotProfile
    {
        [SerializeField] PivotScriptableObject _pivot;
        [SerializeField] SerializedFloatRange _speedRange;
        [SerializeField] SerializedFloatRange[] _angleRanges;

        public int Id => _pivot.Id;

        public FloatRange SpeedRange => _speedRange.Value;

        public IEnumerable<FloatRange> AngleRanges => _angleRanges.Select(angleRange => angleRange.Value);
    }
}