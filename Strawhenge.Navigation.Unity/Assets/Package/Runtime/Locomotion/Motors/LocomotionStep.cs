using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    // What LocomotionController wants the motor to do for one step.
    public readonly struct LocomotionStep
    {
        LocomotionStep(bool shouldMove, Vector3 velocity, Quaternion rotation)
        {
            ShouldMove = shouldMove;
            Velocity = velocity;
            Rotation = rotation;
        }

        // False while inactive or while a pivot plays: the motor leaves the character where it is, keeps
        // Rotation, and keeps reporting the velocity of its last move. EndStep isn't needed.
        public bool ShouldMove { get; }

        public Vector3 Velocity { get; }

        public Quaternion Rotation { get; }

        public static LocomotionStep Moving(Vector3 velocity, Quaternion rotation) =>
            new(shouldMove: true, velocity, rotation);

        public static LocomotionStep Stationary(Quaternion rotation) =>
            new(shouldMove: false, Vector3.zero, rotation);
    }
}
