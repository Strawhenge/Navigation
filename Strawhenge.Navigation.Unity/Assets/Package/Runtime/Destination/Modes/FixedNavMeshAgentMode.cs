namespace Strawhenge.Navigation.Unity.Destination
{
    // An area mask with no cost overrides: every area keeps its project-wide cost.
    public class FixedNavMeshAgentMode : INavMeshAgentMode
    {
        public FixedNavMeshAgentMode(int areaMask)
        {
            AreaMask = areaMask;
        }

        public int AreaMask { get; }

        public bool TryGetAreaCost(int areaIndex, out float cost)
        {
            cost = 0f;
            return false;
        }
    }
}
