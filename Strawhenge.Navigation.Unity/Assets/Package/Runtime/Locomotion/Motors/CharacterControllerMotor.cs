using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public sealed class CharacterControllerMotor : ICharacterMotor
    {
        readonly CharacterController _characterController;
        readonly Transform _transform;

        public CharacterControllerMotor(CharacterController characterController)
        {
            _characterController = characterController;
            _transform = characterController.transform;
        }

        public Vector3 Position => _transform.position;

        public Quaternion Rotation => _transform.rotation;

        // The CharacterController keeps the velocity of its last Move.
        public Vector3 Velocity => _characterController.velocity;

        public bool IsGrounded => _characterController.isGrounded;

        public MotorUpdateMode UpdateMode => MotorUpdateMode.Update;

        public void SetActive(bool active)
        {
            _characterController.enabled = active;
        }

        public void SetRotation(Quaternion rotation)
        {
            _transform.rotation = rotation;
        }

        public MotorMoveResult Move(LocomotionStep step, float deltaTime)
        {
            // Only when it changed: each write notifies the whole hierarchy and syncs its colliders.
            if (step.Rotation != _transform.rotation)
                _transform.rotation = step.Rotation;

            var collisionFlags = _characterController.Move(step.Velocity * deltaTime);

            return new MotorMoveResult(
                blockedSideways: (collisionFlags & CollisionFlags.Sides) != 0);
        }
    }
}
