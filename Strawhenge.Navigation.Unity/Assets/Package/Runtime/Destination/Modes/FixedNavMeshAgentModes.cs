namespace Strawhenge.Navigation.Unity.Destination
{
    // Uses one area mask for both modes, e.g. the NavMeshAgent's own inspector setting
    // when no modes asset is assigned.
    public class FixedNavMeshAgentModes : INavMeshAgentModes
    {
        public FixedNavMeshAgentModes(int areaMask)
        {
            DefaultAreaMask = areaMask;
            LeisurelyAreaMask = areaMask;
        }

        public int DefaultAreaMask { get; }

        public int LeisurelyAreaMask { get; }
    }
}
