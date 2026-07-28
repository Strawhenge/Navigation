namespace Strawhenge.Navigation.Unity.Destination
{
    public class NullDestinationContext : IDestinationContext
    {
        public static NullDestinationContext Instance { get; } = new();

        NullDestinationContext()
        {
        }

        public bool CanNavigate => true;
    }
}