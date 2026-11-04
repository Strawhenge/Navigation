using Strawhenge.Common.Ranges;
using System.Collections.Generic;

namespace Strawhenge.Navigation.Unity.Tests
{
    sealed class FakePivotProfile : IPivotProfile
    {
        public FakePivotProfile(int id, FloatRange speedRange, params FloatRange[] angleRanges)
        {
            Id = id;
            SpeedRange = speedRange;
            AngleRanges = angleRanges;
        }

        public int? Id { get; }
        public FloatRange SpeedRange { get; }
        public IEnumerable<FloatRange> AngleRanges { get; }
    }
}
