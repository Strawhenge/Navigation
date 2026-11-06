using UnityEngine;

namespace Strawhenge.Navigation.Unity.Destination
{
    public interface IDestinationSettings
    {
        // Horizontal speed while jumping across an off-mesh link.
        float JumpSpeed { get; }

        // Horizontal speed while dropping down an off-mesh link.
        float FallHorizontalSpeed { get; }

        // Downward acceleration while dropping down an off-mesh link (positive).
        float FallAcceleration { get; }

        // Layers that count as ground when checking whether a drop has left its ledge.
        // Leave out layers of the character's own colliders (hitboxes, ragdoll parts).
        LayerMask GroundLayers { get; }

        // How far from an approximate destination to look for the nearest point on the NavMesh, when the
        // destination itself isn't on it. Exact destinations are never moved.
        float ApproximateSearchDistance { get; }
    }
}
