namespace Strawhenge.Navigation.Unity.Destination
{
    public class DestinationResult
    {
        public static DestinationResult Arrived => new DestinationResult
        {
            IsAtDestination = true
        };

        public static DestinationResult Inaccessible => new DestinationResult
        {
            IsDestinationInaccessible = true
        };

        public static DestinationResult Cancelled => new DestinationResult
        {
            IsCancelled = true
        };

        public static DestinationResult CancelledByNewDestination => new DestinationResult
        {
            IsCancelled = true,
            HasNewDestination = true
        };

        public bool IsAtDestination { get; private set; }

        public bool IsDestinationInaccessible { get; private set; }

        public bool IsCancelled { get; private set; }

        public bool HasNewDestination { get; private set; }
    }
}
