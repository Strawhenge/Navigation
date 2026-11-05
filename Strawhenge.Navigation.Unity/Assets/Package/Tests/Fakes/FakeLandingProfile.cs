namespace Strawhenge.Navigation.Unity.Tests
{
    sealed class FakeLandingProfile : ILandingProfile
    {
        public FakeLandingProfile(int id, ValueRange fallDistanceRange, ValueRange speedRange)
        {
            Id = id;
            FallDistanceRange = fallDistanceRange;
            SpeedRange = speedRange;
        }

        public int? Id { get; }
        public ValueRange FallDistanceRange { get; }
        public ValueRange SpeedRange { get; }
    }
}
