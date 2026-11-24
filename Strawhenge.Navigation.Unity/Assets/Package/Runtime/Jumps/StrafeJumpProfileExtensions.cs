namespace Strawhenge.Navigation.Unity
{
    // Matching used when a jump starts: plain loops over the ranges, so nothing allocates.
    static class StrafeJumpProfileExtensions
    {
        internal const string DirectionRangesTooltip =
            "Signed angles in degrees from the character's facing to the direction of the jump: " +
            "0 forward, 90 right, -90 left, 180 / -180 back. Standing still counts as 0, so a standing jump " +
            "needs -180 to 180. Empty never matches.";

        public static bool Matches(this IStrafeJumpProfile jump, float speed, float direction) =>
            jump.SpeedRange.Contains(speed) && jump.IsInDirectionRange(direction);

        public static bool IsInDirectionRange(this IStrafeJumpProfile jump, float direction)
        {
            var directionRanges = jump.DirectionRanges;

            for (var i = 0; i < directionRanges.Count; i++)
            {
                if (directionRanges[i].Contains(direction))
                    return true;
            }

            return false;
        }

        // Why settings can't use the profile, or null if they can.
        public static string FindProblem(this IStrafeJumpProfile jump)
        {
            if (jump.Id == null)
                return "has no jump asset assigned";

            if (jump.DirectionRanges.Count == 0)
                return "has no direction ranges, so it can never match";

            return null;
        }
    }
}
