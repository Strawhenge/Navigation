namespace Strawhenge.Navigation.Unity.Destination
{
    // Uses one area mask for both modes, e.g. the NavMeshAgent's own inspector setting
    // when no modes asset is assigned. Area costs stay at their project-wide values.
    public class FixedNavMeshAgentModes : INavMeshAgentModes
    {
        public FixedNavMeshAgentModes(int areaMask)
        {
            var mode = new FixedNavMeshAgentMode(areaMask);
            Default = mode;
            Leisurely = mode;
        }

        public INavMeshAgentMode Default { get; }

        public INavMeshAgentMode Leisurely { get; }
    }
}
