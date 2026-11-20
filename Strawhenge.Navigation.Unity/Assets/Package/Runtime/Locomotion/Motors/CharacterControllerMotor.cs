using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public sealed class CharacterControllerMotor : ICharacterMotor
    {
        readonly CharacterController _characterController;
        readonly ICharacterControllerMotorSettings _settings;
        readonly Transform _transform;

        public CharacterControllerMotor(
            CharacterController characterController,
            ICharacterControllerMotorSettings settings)
        {
            _characterController = characterController;
            _settings = settings;
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

            var velocity = step.Velocity;

            // Locomotion asks for no speed down while grounded; push into the ground so isGrounded stays true.
            if (_characterController.isGrounded && velocity.y <= 0f)
                velocity.y = GetGroundedPush(velocity);

            var collisionFlags = _characterController.Move(velocity * deltaTime);

            return new MotorMoveResult(
                blockedSideways: (collisionFlags & CollisionFlags.Sides) != 0);
        }

        // At least enough to follow any walkable slope (and crest) at this speed: a fixed push can't keep up going
        // downhill fast, and the character would fly off. Pushing harder into the ground does nothing, as the
        // CharacterController stops there.
        float GetGroundedPush(Vector3 velocity)
        {
            var horizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            var slopePush = -horizontalSpeed * Mathf.Tan(_characterController.slopeLimit * Mathf.Deg2Rad);
            return Mathf.Min(_settings.GroundedPush, slopePush);
        }
    }
}
