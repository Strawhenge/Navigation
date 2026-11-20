using System.Collections.Generic;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Tests
{
    // A motor without physics, for testing LocomotionController on its own. It moves exactly as told, onto a
    // flat floor at GroundHeight (no floor when null), and is grounded while standing on it. Records every move.
    sealed class FakeMotor : ICharacterMotor
    {
        const float GroundTolerance = 0.001f;

        readonly List<LocomotionStep> _moves = new();

        public Vector3 Position { get; set; }

        public Quaternion Rotation { get; set; } = Quaternion.identity;

        // Measured from each move, like CharacterController.velocity. Kept between moves.
        public Vector3 Velocity { get; private set; }

        public bool IsGrounded => GroundHeight is float ground && Position.y <= ground + GroundTolerance;

        public MotorUpdateMode UpdateMode => MotorUpdateMode.Update;

        public float? GroundHeight { get; set; } = 0f;

        // Reported as the result of every move, as when walking into a wall.
        public bool BlockedSideways { get; set; }

        // The fraction of each move's horizontal movement that happens: 0 is stopped dead by a wall.
        public float HorizontalFreedom { get; set; } = 1f;

        public bool IsActive { get; private set; }

        public IReadOnlyList<LocomotionStep> Moves => _moves;

        public LocomotionStep LastMove => _moves[_moves.Count - 1];

        public void SetActive(bool active) => IsActive = active;

        public void SetRotation(Quaternion rotation) => Rotation = rotation;

        public MotorMoveResult Move(LocomotionStep step, float deltaTime)
        {
            _moves.Add(step);
            Rotation = step.Rotation;

            var displacement = step.Velocity * deltaTime;
            displacement.x *= HorizontalFreedom;
            displacement.z *= HorizontalFreedom;

            var start = Position;
            var end = start + displacement;

            if (GroundHeight is float ground && end.y < ground)
                end.y = ground;

            Position = end;
            Velocity = (end - start) / deltaTime;

            return new MotorMoveResult(BlockedSideways);
        }
    }
}
