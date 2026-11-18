namespace Strawhenge.Navigation.Unity
{
    // Test hooks. Call them before Awake (while the GameObject is inactive).
    public sealed partial class MovementSourceManagerScript
    {
        internal void SetMovementSources(params MovementSourceScript[] movementSourceScripts) =>
            _movementSourceScripts = movementSourceScripts;
    }
}
