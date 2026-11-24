using System.Collections.Generic;

namespace Strawhenge.Navigation.Unity.Tests
{
    sealed class FakeStrafeJumpProfile : IStrafeJumpProfile
    {
        public FakeStrafeJumpProfile(int? id, ValueRange speedRange, params ValueRange[] directionRanges)
        {
            Id = id;
            SpeedRange = speedRange;
            DirectionRanges = directionRanges;
        }

        public int? Id { get; }
        public ValueRange SpeedRange { get; }
        public IReadOnlyList<ValueRange> DirectionRanges { get; }
    }
}
