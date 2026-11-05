namespace Strawhenge.Navigation.Unity
{
    // Matching used every frame by LocomotionScript: plain loops over the ranges, so nothing allocates.
    static class PivotProfileExtensions
    {
        public static bool Matches(this IPivotProfile pivot, float speed, float turnAngle) =>
            pivot.SpeedRange.Contains(speed) && pivot.IsInAngleRange(turnAngle);

        public static bool IsInAngleRange(this IPivotProfile pivot, float turnAngle)
        {
            var angleRanges = pivot.AngleRanges;

            for (var i = 0; i < angleRanges.Count; i++)
            {
                if (angleRanges[i].Contains(turnAngle))
                    return true;
            }

            return false;
        }
    }
}
