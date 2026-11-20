using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    // What LocomotionController wants the motor to do for one step.
    public readonly struct LocomotionStep
    {
        LocomotionStep(bool shouldMove, Vector3 velocity, Quaternion rotation, bool jumpStarted)
        {
            ShouldMove = shouldMove;
            Velocity = velocity;
            Rotation = rotation;
            JumpStarted = jumpStarted;
        }

        // False while inactive or while a pivot plays: the motor leaves the character where it is, keeps
        // Rotation, and keeps reporting the velocity of its last move. EndStep isn't needed.
        public bool ShouldMove { get; }

        public Vector3 Velocity { get; }

        public Quaternion Rotation { get; }

        // True on the one step a jump begins (Velocity then rises). Motors that snap to the ground leave it.
        public bool JumpStarted { get; }

        public static LocomotionStep Moving(Vector3 velocity, Quaternion rotation, bool jumpStarted = false) =>
            new(shouldMove: true, velocity, rotation, jumpStarted);

        public static LocomotionStep Stationary(Quaternion rotation) =>
            new(shouldMove: false, Vector3.zero, rotation, jumpStarted: false);
    }
}
