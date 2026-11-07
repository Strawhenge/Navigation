using Strawhenge.Navigation.Unity.Destination;

namespace Strawhenge.Navigation.Unity.Tests
{
    sealed class FakeNavMeshAgentModes : INavMeshAgentModes
    {
        // Area masks only: every area keeps its project-wide cost.
        public FakeNavMeshAgentModes(int defaultAreaMask, int leisurelyAreaMask)
            : this(new FakeNavMeshAgentMode(defaultAreaMask), new FakeNavMeshAgentMode(leisurelyAreaMask))
        {
        }

        public FakeNavMeshAgentModes(INavMeshAgentMode defaultMode, INavMeshAgentMode leisurelyMode)
        {
            Default = defaultMode;
            Leisurely = leisurelyMode;
        }

        public INavMeshAgentMode Default { get; }

        public INavMeshAgentMode Leisurely { get; }
    }
}
