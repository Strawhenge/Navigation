using System;
using System.Collections.Generic;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    [Serializable]
    public class SerializedStrafeJumpProfile : IStrafeJumpProfile
    {
        [SerializeField] JumpScriptableObject _jump;
        [SerializeField] ValueRange _speedRange;

        [Tooltip(StrafeJumpProfileExtensions.DirectionRangesTooltip)]
        [SerializeField] ValueRange[] _directionRanges;

        // Not _jump?.Id: '?.' skips Unity's null check, so a missing asset could count as assigned.
        public int? Id => _jump != null ? _jump.Id : null;

        public ValueRange SpeedRange => _speedRange;

        public IReadOnlyList<ValueRange> DirectionRanges => _directionRanges ?? Array.Empty<ValueRange>();
    }
}
