using System.Collections.Generic;

namespace Strawhenge.Navigation.Unity.Tests
{
    sealed class FakePivotProfile : IPivotProfile
    {
        public FakePivotProfile(int id, ValueRange speedRange, params ValueRange[] angleRanges)
        {
            Id = id;
            SpeedRange = speedRange;
            AngleRanges = angleRanges;
        }

        public int? Id { get; }
        public ValueRange SpeedRange { get; }
        public IReadOnlyList<ValueRange> AngleRanges { get; }
    }
}
