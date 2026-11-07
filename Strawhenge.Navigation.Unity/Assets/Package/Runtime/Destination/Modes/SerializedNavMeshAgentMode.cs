using Strawhenge.Common.Unity.Serialization;
using System;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    [Serializable]
    public class SerializedNavMeshAgentMode : INavMeshAgentMode
    {
        [SerializeField] NavMeshAreaMask _areaMask = new();
        [SerializeField] SerializedAreaCost[] _areaCosts = Array.Empty<SerializedAreaCost>();

        public int AreaMask => _areaMask == null ? 0 : _areaMask.Value;

        // When several entries include the area, the last one wins.
        public bool TryGetAreaCost(int areaIndex, out float cost)
        {
            if (_areaCosts != null)
            {
                var areaBit = 1 << areaIndex;

                for (var i = _areaCosts.Length - 1; i >= 0; i--)
                {
                    var entry = _areaCosts[i];
                    if (entry == null || (entry.AreaMask & areaBit) == 0)
                        continue;

                    cost = entry.Cost;
                    return true;
                }
            }

            cost = 0f;
            return false;
        }
    }
}
