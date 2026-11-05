using Strawhenge.Common.Ranges;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Strawhenge.Navigation.Unity
{
    // A snapshot of a pivot profile, taken when CachedLocomotionSettings rebuilds its lists. LocomotionScript
    // matches against it every frame without allocating: the profile classes create new ranges on every read.
    sealed class ResolvedPivotProfile : IPivotProfile
    {
        public ResolvedPivotProfile(int id, FloatRange speedRange, FloatRange[] angleRanges)
        {
            Id = id;
            SpeedRange = speedRange;
            AngleRanges = angleRanges;
        }

        public int Id { get; }

        public FloatRange SpeedRange { get; }

        public FloatRange[] AngleRanges { get; }

        int? IPivotProfile.Id => Id;

        IEnumerable<FloatRange> IPivotProfile.AngleRanges => AngleRanges;

        // Profiles without an ID (no pivot asset) are left out.
        public static ResolvedPivotProfile[] From(IPivotProfile[] profiles)
        {
            var resolved = new List<ResolvedPivotProfile>(profiles.Length);

            foreach (var profile in profiles)
            {
                if (profile.Id is int id)
                {
                    var angleRanges = profile.AngleRanges?.ToArray() ?? Array.Empty<FloatRange>();
                    resolved.Add(new ResolvedPivotProfile(id, profile.SpeedRange, angleRanges));
                }
            }

            return resolved.ToArray();
        }

        public bool Matches(float speed, float turnAngle) =>
            SpeedRange.IsInRange(speed) && IsInAngleRange(turnAngle);

        public bool IsInAngleRange(float turnAngle)
        {
            foreach (var angleRange in AngleRanges)
            {
                if (angleRange.IsInRange(turnAngle))
                    return true;
            }

            return false;
        }
    }
}
