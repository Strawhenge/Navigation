namespace Strawhenge.Navigation.Unity
{
    // Test hooks. Call them before Awake (while the GameObject is inactive); Awake keeps what was set.
    public partial class MovementAnimationSyncScript
    {
        internal void SetMovementSourceManager(MovementSourceManagerScript movementSourceManager) =>
            _movementSourceManager = movementSourceManager;
    }
}
