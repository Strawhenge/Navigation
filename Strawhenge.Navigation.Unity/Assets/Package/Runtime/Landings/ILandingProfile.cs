using Strawhenge.Common.Ranges;

namespace Strawhenge.Navigation.Unity
{
    public interface ILandingProfile
    {
        int Id { get; }
        FloatRange FallDistanceRange { get; }
        FloatRange SpeedRange { get; }
    }
}