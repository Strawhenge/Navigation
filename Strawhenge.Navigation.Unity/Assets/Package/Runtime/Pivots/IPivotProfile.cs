using System.Collections.Generic;

namespace Strawhenge.Navigation.Unity
{
    public interface IPivotProfile
    {
        // Null when the profile has no pivot asset; such profiles are ignored when settings resolve them.
        int? Id { get; }

        ValueRange SpeedRange { get; }

        IReadOnlyList<ValueRange> AngleRanges { get; }
    }
}
