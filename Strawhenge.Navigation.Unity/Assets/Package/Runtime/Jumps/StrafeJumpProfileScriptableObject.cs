using System;
using System.Collections.Generic;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    [CreateAssetMenu(menuName = "Strawhenge/Navigation/Strafe Jump Profile")]
    public class StrafeJumpProfileScriptableObject : ScriptableObject, IStrafeJumpProfile
    {
        [SerializeField] JumpScriptableObject _jump;
        [SerializeField] ValueRange _speedRange;

        [Tooltip(StrafeJumpProfileExtensions.DirectionRangesTooltip)]
        [SerializeField] ValueRange[] _directionRanges;

        public int? Id => _jump != null ? _jump.Id : null;

        public ValueRange SpeedRange => _speedRange;

        public IReadOnlyList<ValueRange> DirectionRanges => _directionRanges ?? Array.Empty<ValueRange>();

        // Assigning or clearing the jump asset, or the direction ranges, changes whether settings include
        // this profile.
        void OnValidate() => CachedLocomotionSettings.Invalidate();
    }
}
