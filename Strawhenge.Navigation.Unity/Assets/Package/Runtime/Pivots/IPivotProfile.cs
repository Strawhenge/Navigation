namespace Strawhenge.Navigation.Unity
{
    public interface IPivotProfile
    {
        // Null when the profile has no pivot asset; such profiles are ignored when settings resolve them.
        int? Id { get; }

        Strawhenge.Common.Ranges.FloatRange SpeedRange { get; }

        System.Collections.Generic.IEnumerable<Strawhenge.Common.Ranges.FloatRange> AngleRanges { get; }
    }
}
