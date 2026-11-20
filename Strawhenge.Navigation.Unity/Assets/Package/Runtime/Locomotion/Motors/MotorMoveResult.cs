namespace Strawhenge.Navigation.Unity
{
    public readonly struct MotorMoveResult
    {
        public MotorMoveResult(bool blockedSideways)
        {
            BlockedSideways = blockedSideways;
        }

        // Something to the side (a wall, not the ground or a ceiling) stopped part of the move.
        public bool BlockedSideways { get; }
    }
}
