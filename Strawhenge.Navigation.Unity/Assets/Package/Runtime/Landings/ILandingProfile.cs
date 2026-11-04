using Strawhenge.Common.Ranges;

namespace Strawhenge.Navigation.Unity
{
    public interface ILandingProfile
    {
        // Null when the profile has no landing asset; such profiles are ignored when settings resolve them.
        int? Id { get; }
        FloatRange FallDistanceRange { get; }
        FloatRange SpeedRange { get; }
    }
}