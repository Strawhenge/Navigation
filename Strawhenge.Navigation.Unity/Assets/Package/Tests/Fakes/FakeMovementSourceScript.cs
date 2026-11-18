namespace Strawhenge.Navigation.Unity.Tests
{
    class FakeMovementSourceScript : MovementSourceScript
    {
        public FakeMovementSource Source { get; } = new();

        public override IMovementSource MovementSource => Source;
    }
}
