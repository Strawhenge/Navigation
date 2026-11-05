using Strawhenge.Common.Ranges;
using System.Collections.Generic;

namespace Strawhenge.Navigation.Unity
{
    // A snapshot of a landing profile, taken when CachedLocomotionSettings rebuilds its lists, so matching
    // against it allocates nothing: the profile classes create new ranges on every read.
    sealed class ResolvedLandingProfile : ILandingProfile
    {
        public ResolvedLandingProfile(int id, FloatRange fallDistanceRange, FloatRange speedRange)
        {
            Id = id;
            FallDistanceRange = fallDistanceRange;
            SpeedRange = speedRange;
        }

        public int Id { get; }

        public FloatRange FallDistanceRange { get; }

        public FloatRange SpeedRange { get; }

        int? ILandingProfile.Id => Id;

        // Profiles without an ID (no landing asset) are left out.
        public static ResolvedLandingProfile[] From(ILandingProfile[] profiles)
        {
            var resolved = new List<ResolvedLandingProfile>(profiles.Length);

            foreach (var profile in profiles)
            {
                if (profile.Id is int id)
                    resolved.Add(new ResolvedLandingProfile(id, profile.FallDistanceRange, profile.SpeedRange));
            }

            return resolved.ToArray();
        }

        public bool Matches(float speed, float fallDistance) =>
            SpeedRange.IsInRange(speed) && FallDistanceRange.IsInRange(fallDistance);
    }
}
