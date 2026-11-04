namespace Strawhenge.Navigation.Unity.Destination
{
    static class SingleAgentPriorityManager
    {
        // Shared by every DestinationScript. Never cleared: destroyed agents compare equal to null,
        // so their slots are already treated as free and reused.
        public static AgentPriorityManager Instance { get; } = new();
    }
}