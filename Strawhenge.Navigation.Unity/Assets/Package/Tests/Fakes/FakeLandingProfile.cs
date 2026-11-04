using Strawhenge.Common.Ranges;

namespace Strawhenge.Navigation.Unity.Tests
{
    sealed class FakeLandingProfile : ILandingProfile
    {
        public FakeLandingProfile(int id, FloatRange fallDistanceRange, FloatRange speedRange)
        {
            Id = id;
            FallDistanceRange = fallDistanceRange;
            SpeedRange = speedRange;
        }

        public int Id { get; }
        public FloatRange FallDistanceRange { get; }
        public FloatRange SpeedRange { get; }
    }
}
