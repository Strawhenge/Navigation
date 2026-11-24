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

        // Jumps played across an off-mesh link while not strafing. The first that matches waits at the start
        // of the link for its wind-up animation to launch the jump; with no match, the jump launches straight
        // away.
        IJumpProfile[] Jumps { get; }

        // Jumps played across an off-mesh link while strafing, matched on speed and the link's direction. With
        // no match, the jump launches straight away.
        IStrafeJumpProfile[] StrafeJumps { get; }

        // Landings played after jumping across an off-mesh link.
        ILandingProfile[] JumpLandings { get; }

        // Landings played after dropping down an off-mesh link.
        ILandingProfile[] FallLandings { get; }

        // Longest wait for a pivot or landing to be completed, or a jump's wind-up to launch it, before
        // carrying on without it. Must be longer than the longest pivot or landing animation.
        float AnimatorWaitTimeout { get; }
    }
}
