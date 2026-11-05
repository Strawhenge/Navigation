using System;
using System.Collections.Generic;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    [CreateAssetMenu(menuName = "Strawhenge/Navigation/Pivot Profile")]
    public class PivotProfileScriptableObject : ScriptableObject, IPivotProfile
    {
        [SerializeField] PivotScriptableObject _pivot;
        [SerializeField] ValueRange _speedRange;
        [SerializeField] ValueRange[] _angleRanges;

        public int? Id => _pivot != null ? _pivot.Id : null;

        public ValueRange SpeedRange => _speedRange;

        public IReadOnlyList<ValueRange> AngleRanges => _angleRanges ?? Array.Empty<ValueRange>();

        // Assigning or clearing the pivot asset changes whether settings include this profile.
        void OnValidate() => CachedLocomotionSettings.Invalidate();
    }
}
