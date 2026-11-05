namespace Strawhenge.Navigation.Unity
{
    // Matching used by LocomotionScript when landing; nothing allocates.
    static class LandingProfileExtensions
    {
        public static bool Matches(this ILandingProfile landing, float speed, float fallDistance) =>
            landing.SpeedRange.Contains(speed) && landing.FallDistanceRange.Contains(fallDistance);
    }
}
