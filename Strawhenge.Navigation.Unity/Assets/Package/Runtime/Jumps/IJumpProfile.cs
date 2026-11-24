namespace Strawhenge.Navigation.Unity
{
    // A jump played while not strafing. A non-strafing character faces the way it's going, so there's no
    // direction to match on.
    public interface IJumpProfile
    {
        // Null when the profile has no jump asset; such profiles are ignored by the settings caches.
        int? Id { get; }

        ValueRange SpeedRange { get; }
    }
}
