using System.Collections.Generic;

namespace Strawhenge.Navigation.Unity
{
    // A jump played while strafing. Matched on speed and on the direction of the jump relative to the
    // character's facing, as a strafing character can move any way.
    public interface IStrafeJumpProfile
    {
        // Null when the profile has no jump asset; such profiles are ignored when settings resolve them.
        int? Id { get; }

        ValueRange SpeedRange { get; }

        // Signed angles in degrees from the character's facing to the direction of the jump: 0 forward,
        // 90 right, -90 left, ±180 back. Standing still counts as 0. Empty never matches; such profiles are
        // ignored when settings resolve them.
        IReadOnlyList<ValueRange> DirectionRanges { get; }
    }
}
