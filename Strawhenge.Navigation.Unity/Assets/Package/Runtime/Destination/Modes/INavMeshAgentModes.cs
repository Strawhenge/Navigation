namespace Strawhenge.Navigation.Unity.Destination
{
    public interface INavMeshAgentModes
    {
        INavMeshAgentMode Default { get; }

        INavMeshAgentMode Leisurely { get; }
    }
}
