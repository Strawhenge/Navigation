namespace Strawhenge.Navigation.Unity
{
    public interface ILandingProfile
    {
        // Null when the profile has no landing asset; such profiles are ignored by the settings caches.
        int? Id { get; }

        ValueRange FallDistanceRange { get; }

        ValueRange SpeedRange { get; }
    }
}