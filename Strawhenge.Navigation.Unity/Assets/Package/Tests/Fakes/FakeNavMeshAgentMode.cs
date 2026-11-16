using Strawhenge.Navigation.Unity.Destination;
using System.Collections.Generic;

namespace Strawhenge.Navigation.Unity.Tests
{
    sealed class FakeNavMeshAgentMode : INavMeshAgentMode
    {
        readonly Dictionary<int, float> _areaCosts = new();

        public FakeNavMeshAgentMode(int areaMask)
        {
            AreaMask = areaMask;
        }

        public int AreaMask { get; }

        public FakeNavMeshAgentMode WithAreaCost(int areaIndex, float cost)
        {
            _areaCosts[areaIndex] = cost;
            return this;
        }

        public bool TryGetAreaCost(int areaIndex, out float cost) =>
            _areaCosts.TryGetValue(areaIndex, out cost);
    }
}
