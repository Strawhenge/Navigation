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

        // Pivots played instead of turning on the spot, before a trip sets off.
        IPivotProfile[] StationaryPivots { get; }

        // Pivots played instead of turning while following a path.
        IPivotProfile[] MovingPivots { get; }

        // Landings played after jumping across an off-mesh link.
        ILandingProfile[] JumpLandings { get; }

        // Landings played after dropping down an off-mesh link.
        ILandingProfile[] FallLandings { get; }

        // If set, a jump waits at the start of its off-mesh link until the jump animation launches it
        // (MovementAnimationSyncScript.OnJumpLaunch).
        bool DeferJumpTrigger { get; }

        // Longest wait for a pivot or landing to be completed, or a deferred jump to be launched, before
        // carrying on without it. Must be longer than the longest pivot or landing animation.
        float AnimatorWaitTimeout { get; }
    }
}
