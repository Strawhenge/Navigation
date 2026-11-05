using System;
using System.Collections.Generic;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    [Serializable]
    public class SerializedPivotProfile : IPivotProfile
    {
        [SerializeField] PivotScriptableObject _pivot;
        [SerializeField] ValueRange _speedRange;
        [SerializeField] ValueRange[] _angleRanges;

        // Not _pivot?.Id: '?.' skips Unity's null check, so a missing asset could count as assigned.
        public int? Id => _pivot != null ? _pivot.Id : null;

        public ValueRange SpeedRange => _speedRange;

        public IReadOnlyList<ValueRange> AngleRanges => _angleRanges ?? Array.Empty<ValueRange>();
    }
}