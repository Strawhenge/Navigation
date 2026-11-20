using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public interface IRigidbodyMotorSettings
    {
        // What counts as ground (and steps). Leave out the character's own layers if they aren't ignored otherwise.
        LayerMask GroundLayers { get; }

        // Within this of the ground (metres, below the capsule) counts as grounded.
        float GroundDistance { get; }

        // While grounded, ground within this below is snapped down to, so the character follows slopes and steps
        // down instead of skipping off them.
        float GroundSnapDistance { get; }

        // Steeper ground isn't walkable, and contacts steeper than this count as walls.
        float MaxSlopeAngle { get; }

        // The highest step the character walks straight up.
        float StepHeight { get; }
    }
}
