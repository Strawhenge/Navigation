// TODO: Move this into Strawhenge Common in place of FloatRange / SerializedFloatRange, then use it from there.
// Common's FloatRange is a class (with a constructor that throws when max < min), so every
// SerializedFloatRange.Value read allocates. That made per-frame matching allocate here, and will elsewhere.

using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    // An inclusive min-max range, serialized in place. A struct, so reading or checking it never allocates.
    // A range with max below min contains nothing (the inspector warns about it).
    [Serializable]
    public struct ValueRange
    {
        internal const string MinFieldName = nameof(_min);
        internal const string MaxFieldName = nameof(_max);

        [SerializeField] float _min;
        [SerializeField] float _max;

        public ValueRange(float min, float max)
        {
            _min = min;
            _max = max;
        }

        public float Min => _min;

        public float Max => _max;

        public bool Contains(float value) => value >= _min && value <= _max;

        public override string ToString() => $"[{_min}, {_max}]";
    }
}
