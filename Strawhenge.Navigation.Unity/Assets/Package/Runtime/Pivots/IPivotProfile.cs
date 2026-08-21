namespace Strawhenge.Navigation.Unity
{
    public interface IPivotProfile
    {
        int Id { get; }

        Strawhenge.Common.Ranges.FloatRange SpeedRange { get; }

        System.Collections.Generic.IEnumerable<Strawhenge.Common.Ranges.FloatRange> AngleRanges { get; }
    }
}
