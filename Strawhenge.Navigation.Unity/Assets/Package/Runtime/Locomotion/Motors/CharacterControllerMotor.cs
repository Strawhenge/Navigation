using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    public sealed class CharacterControllerMotor : ICharacterMotor
    {
        // How far up inside the capsule the ground probe starts, so it finds ground it's already touching, and how
        // much smaller than the capsule it is, so it doesn't catch walls.
        const float ProbeCastOffset = 0.05f;
        const float ProbeRadiusScale = 0.9f;

        readonly CharacterController _characterController;
        readonly ICharacterControllerMotorSettings _settings;
        readonly Transform _transform;
        readonly RaycastHit[] _probeHits = new RaycastHit[32];

        // Where the last move left the character. Anywhere else, something else has moved it since (a pivot's root
        // motion), and isGrounded is out of date.
        Vector3 _positionAfterMove;
        bool _hasMoved;

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

        // As of the last move, unless something else has moved the character since: then the ground below it is
        // looked for. A pivot's root motion moves it without pushing into the ground (through the
        // CharacterController, so isGrounded turns false even on flat ground), and locomotion needs to know when
        // that carries it off a ledge.
        public bool IsGrounded => IsWhereTheLastMoveLeftIt
            ? _characterController.isGrounded
            : ProbeForGround();

        public MotorUpdateMode UpdateMode => MotorUpdateMode.Update;

        bool IsWhereTheLastMoveLeftIt => _hasMoved && _transform.position == _positionAfterMove;

        public void SetActive(bool active)
        {
            _characterController.enabled = active;
            _hasMoved = false;
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
            if (IsGrounded && velocity.y <= 0f)
                velocity.y = GetGroundedPush(velocity);

            var collisionFlags = _characterController.Move(velocity * deltaTime);

            _positionAfterMove = _transform.position;
            _hasMoved = true;

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

        // Ground within the step offset below the capsule (so a pivot that steps down a stair still counts), on
        // anything the character collides with apart from its own colliders (a ragdoll's).
        bool ProbeForGround()
        {
            if (!_characterController.enabled)
                return false;

            var scale = _transform.lossyScale;
            var radius = _characterController.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            var halfHeight = Mathf.Max(_characterController.height * Mathf.Abs(scale.y) * 0.5f, radius);
            var centre = _transform.TransformPoint(_characterController.center);
            var bottomSphereCentre = centre + Vector3.down * (halfHeight - radius);

            var probeRadius = radius * ProbeRadiusScale;
            var origin = bottomSphereCentre + Vector3.up * ProbeCastOffset;

            var distance = ProbeCastOffset +
                           (radius - probeRadius) +
                           _characterController.skinWidth +
                           _characterController.stepOffset;

            var hitCount = Physics.SphereCastNonAlloc(
                origin,
                probeRadius,
                Vector3.down,
                _probeHits,
                distance,
                GetCollisionMask(),
                QueryTriggerInteraction.Ignore);

            for (var i = 0; i < hitCount; i++)
            {
                var hitTransform = _probeHits[i].collider.transform;

                if (hitTransform != _transform && !hitTransform.IsChildOf(_transform))
                    return true;
            }

            return false;
        }

        // The layers the character's own layer collides with.
        int GetCollisionMask()
        {
            var layer = _transform.gameObject.layer;
            var mask = 0;

            for (var other = 0; other < 32; other++)
            {
                if (!Physics.GetIgnoreLayerCollision(layer, other))
                    mask |= 1 << other;
            }

            return mask;
        }
    }
}
