namespace Strawhenge.Navigation.Unity.Destination
{
    // How the NavMeshAgent finds its path in one mode: the areas it may use and what they cost.
    // Off-mesh links belong to an area too, so area costs also price jumps and drops.
    public interface INavMeshAgentMode
    {
        int AreaMask { get; }

        // False leaves the area at its project-wide cost (Navigation > Areas).
        bool TryGetAreaCost(int areaIndex, out float cost);
    }
}
