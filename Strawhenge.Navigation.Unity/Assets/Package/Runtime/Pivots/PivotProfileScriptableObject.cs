using System.Linq;
using Strawhenge.Common.Unity.Serialization;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    [CreateAssetMenu(menuName = "Strawhenge/Navigation/Pivot Profile")]
    public class PivotProfileScriptableObject : ScriptableObject, IPivotProfile
    {
        [SerializeField] PivotScriptableObject _pivot;
        [SerializeField] SerializedFloatRange _speedRange;
        [SerializeField] SerializedFloatRange[] _angleRanges;

        public int Id => _pivot != null ? _pivot.Id : 0;

        public Strawhenge.Common.Ranges.FloatRange SpeedRange => _speedRange.Value;

        public System.Collections.Generic.IEnumerable<Strawhenge.Common.Ranges.FloatRange> AngleRanges => _angleRanges == null
            ? System.Array.Empty<Strawhenge.Common.Ranges.FloatRange>()
            : _angleRanges.Select(ar => ar.Value);
    }
}

