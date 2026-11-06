namespace Strawhenge.Navigation.Unity
{
    // Shared by every settings cache (CachedLocomotionSettings, CachedDestinationSettings). In the Editor,
    // OnValidate on anything that can change a profile list calls Invalidate(), and every cache rebuilds
    // its lists on the next read. In builds that never happens, so the lists are resolved once.
    static class ProfileCacheVersion
    {
        // Starts at 1 so a new cache (version 0) always resolves its lists on first read.
        public static int Current { get; private set; } = 1;

        public static void Invalidate() => Current++;
    }
}
