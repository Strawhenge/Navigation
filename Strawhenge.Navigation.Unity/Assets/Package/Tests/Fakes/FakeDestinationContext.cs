using Strawhenge.Navigation.Unity.Destination;

namespace Strawhenge.Navigation.Unity.Tests
{
    // A context whose CanNavigate the test sets, as a ragdoll or a stun would in a game.
    class FakeDestinationContext : IDestinationContext
    {
        public bool CanNavigate { get; set; } = true;
    }
}
