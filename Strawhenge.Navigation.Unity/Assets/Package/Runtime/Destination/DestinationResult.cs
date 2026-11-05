namespace Strawhenge.Navigation.Unity.Destination
{
    // Results are immutable, so each is a single shared instance and calling back allocates nothing.
    public class DestinationResult
    {
        public static DestinationResult Arrived { get; } = new()
        {
            IsAtDestination = true
        };

        public static DestinationResult Inaccessible { get; } = new()
        {
            IsDestinationInaccessible = true
        };

        public static DestinationResult Cancelled { get; } = new()
        {
            IsCancelled = true
        };

        public static DestinationResult CancelledByNewDestination { get; } = new()
        {
            IsCancelled = true,
            HasNewDestination = true
        };

        DestinationResult()
        {
        }

        public bool IsAtDestination { get; private set; }

        public bool IsDestinationInaccessible { get; private set; }

        public bool IsCancelled { get; private set; }

        public bool HasNewDestination { get; private set; }
    }
}
