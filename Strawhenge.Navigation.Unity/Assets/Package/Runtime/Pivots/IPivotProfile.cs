using System.Collections.Generic;

namespace Strawhenge.Navigation.Unity
{
    public interface IPivotProfile
    {
        // Null when the profile has no pivot asset; such profiles are ignored by the settings caches.
        int? Id { get; }

        ValueRange SpeedRange { get; }

        IReadOnlyList<ValueRange> AngleRanges { get; }
    }
}
