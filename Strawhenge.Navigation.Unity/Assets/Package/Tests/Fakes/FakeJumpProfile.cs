namespace Strawhenge.Navigation.Unity.Tests
{
    sealed class FakeJumpProfile : IJumpProfile
    {
        public FakeJumpProfile(int? id, ValueRange speedRange)
        {
            Id = id;
            SpeedRange = speedRange;
        }

        public int? Id { get; }
        public ValueRange SpeedRange { get; }
    }
}
