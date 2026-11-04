using Strawhenge.Navigation.Unity.Destination;

namespace Strawhenge.Navigation.Unity.Tests
{
    sealed class FakeNavMeshAgentModes : INavMeshAgentModes
    {
        public FakeNavMeshAgentModes(int defaultAreaMask, int leisurelyAreaMask)
        {
            DefaultAreaMask = defaultAreaMask;
            LeisurelyAreaMask = leisurelyAreaMask;
        }

        public int DefaultAreaMask { get; }

        public int LeisurelyAreaMask { get; }
    }
}
