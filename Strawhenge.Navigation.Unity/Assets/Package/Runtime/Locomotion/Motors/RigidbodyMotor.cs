using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    // Moves a dynamic Rigidbody with an upright CapsuleCollider by setting its velocity each physics step. The
    // locomotion controller owns gravity (the Rigidbody doesn't use its own), so jump height and coyote time mean
    // the same as with the CharacterController motor. Grounding is a sphere cast below the capsule; while grounded
    // the character is moved along the ground, snapped down onto it, and lifted up steps up to StepHeight.
    //
    // Root motion (from a RootMotionRelayScript; only played during pivots, while the body isn't moved) is applied
    // to the transform each frame,
    // with the body held kinematic and uninterpolated meanwhile, exactly as the CharacterController motor gets it.
    // Driving it through the dynamic, interpolated body instead had the Animator (per frame) and physics (per
    // step) fighting over the rotation, and the turn shook.
    public sealed class RigidbodyMotor : ICharacterMotor, IRootMotionHandler
    {
        // The ground probe starts this far up inside the capsule, so it can't start overlapping the ground.
        const float CastOffset = 0.1f;
        const float ProbeRadiusScale = 0.95f;

        // Closer to the ground than this isn't snapped: the capsule is resting on it. Snapping that close would
        // push into a slope every step, and a frictionless body pushed into a slope slides down it.
        const float SnapTolerance = 0.02f;

        // Steps are looked for this far beyond the coming move, so the character is lifted before it hits them.
        const float StepLookAhead = 0.05f;
        const float StepProbeInset = 0.05f;
        const float StepClearance = 0.01f;

        const int MaxHits = 8;

        readonly Rigidbody _rigidbody;
        readonly CapsuleCollider _capsule;
        readonly IRigidbodyMotorSettings _settings;
        readonly Transform _root;
        readonly RigidbodyInterpolation _interpolation;
        readonly RaycastHit[] _hits = new RaycastHit[MaxHits];
        readonly Collider[] _overlaps = new Collider[MaxHits];

        bool _isActive;
        bool _wasGrounded;
        bool _hasMovedThisStep;
        bool _blockedSideways;
        Vector3 _velocity;

        // Held for root motion: kinematic and uninterpolated, with the transform leading.
        bool _isHeld;

        public RigidbodyMotor(Rigidbody rigidbody, CapsuleCollider capsule, IRigidbodyMotorSettings settings)
        {
            _rigidbody = rigidbody;
            _capsule = capsule;
            _settings = settings;
            _root = rigidbody.transform;
            _interpolation = rigidbody.interpolation;
        }

        public Vector3 Position => _isHeld ? _root.position : _rigidbody.position;

        public Quaternion Rotation => _isHeld ? _root.rotation : _rigidbody.rotation;

        // The velocity the body managed in the physics step before the last move (the body's own velocity is the
        // one just set until physics runs).
        public Vector3 Velocity => _velocity;

        public bool IsGrounded => _isActive && CheckGround(out _, out _);

        public MotorUpdateMode UpdateMode => MotorUpdateMode.FixedUpdate;

        // Off: kinematic with the capsule off, so a NavMeshAgent or ragdoll can move the character. On: picks up
        // wherever the character was moved meanwhile.
        public void SetActive(bool active)
        {
            _isActive = active;
            _wasGrounded = false;
            _hasMovedThisStep = false;
            _blockedSideways = false;
            _velocity = Vector3.zero;
            _isHeld = false;

            if (active)
            {
                _rigidbody.position = _root.position;
                _rigidbody.rotation = _root.rotation;
                _capsule.enabled = true;
                _rigidbody.isKinematic = false;
                _rigidbody.interpolation = _interpolation;
                _rigidbody.velocity = Vector3.zero;
            }
            else
            {
                if (!_rigidbody.isKinematic)
                    _rigidbody.velocity = Vector3.zero;

                _rigidbody.isKinematic = true;
                _capsule.enabled = false;

                // An interpolated body keeps writing its own pose to the transform, which would undo whatever
                // moves the character meanwhile.
                _rigidbody.interpolation = RigidbodyInterpolation.None;
            }
        }

        public void SetRotation(Quaternion rotation)
        {
            _rigidbody.rotation = rotation;
            _root.rotation = rotation;
        }

        public MotorMoveResult Move(LocomotionStep step, float deltaTime)
        {
            if (!_isActive)
                return default;

            _hasMovedThisStep = true;

            // Moves own the body: root motion only plays while it isn't moved (pivots).
            if (_isHeld)
                Release();

            // What the last physics step did, which is what the controller reacts to: the velocity the body
            // managed, and whether a wall stopped it.
            _velocity = _rigidbody.velocity;
            var blockedSideways = _blockedSideways;
            _blockedSideways = false;

            if (step.Rotation != _rigidbody.rotation)
                _rigidbody.MoveRotation(step.Rotation);

            var velocity = step.Velocity;
            var horizontal = new Vector3(velocity.x, 0f, velocity.z);

            // A jump leaves the ground, so the snap doesn't pull the character back. After this step it's no longer
            // grounded, so only GroundDistance (not the snap distance) counts, and the jump has already left that.
            var groundNormal = Vector3.up;
            var groundGap = 0f;
            var isGrounded = !step.JumpStarted && CheckGround(out groundNormal, out groundGap);

            if (isGrounded)
            {
                if (horizontal.sqrMagnitude > 0f && TryStepUp(horizontal, deltaTime))
                {
                    blockedSideways = false;
                    groundGap = 0f;
                }

                // Along the ground at the same speed, so slopes neither launch the character nor slow it, and down
                // onto it: the controller's own push down (GroundedGravity) isn't needed.
                var alongGround = Vector3.ProjectOnPlane(horizontal, groundNormal);
                if (alongGround.sqrMagnitude > 0f)
                    alongGround = alongGround.normalized * horizontal.magnitude;

                var snap = groundGap > SnapTolerance ? groundGap / deltaTime : 0f;
                velocity = alongGround + Vector3.down * snap;
            }

            _wasGrounded = isGrounded;
            _rigidbody.velocity = velocity;

            return new MotorMoveResult(blockedSideways);
        }

        // Called each physics step after LocomotionScript's. A step without a move (a pivot playing) leaves the
        // character where locomotion has it, as LocomotionStep.ShouldMove asks: a dynamic body is stopped, and a
        // held one is left to its root motion.
        internal void EndFixedStep()
        {
            if (_isActive && !_hasMovedThisStep && !_isHeld)
            {
                _rigidbody.velocity = Vector3.zero;
                _blockedSideways = false;
            }

            _hasMovedThisStep = false;
        }

        // A frame of the Animator's root motion (a pivot's turn and steps), applied to the transform straight away.
        // The first holds the body until the next move. Not taken while off.
        public bool TryApplyRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
        {
            if (!_isActive)
                return false;

            if (!_isHeld)
                Hold();

            _root.SetPositionAndRotation(
                _root.position + new Vector3(deltaPosition.x, 0f, deltaPosition.z),
                deltaRotation * _root.rotation);

            return true;
        }

        void Hold()
        {
            _isHeld = true;
            _blockedSideways = false;

            _rigidbody.velocity = Vector3.zero;
            _rigidbody.interpolation = RigidbodyInterpolation.None;
            _rigidbody.isKinematic = true;

            // From the pose on screen (interpolated, up to a step behind the body's), so the pivot starts where the
            // character is seen.
            _rigidbody.position = _root.position;
            _rigidbody.rotation = _root.rotation;
        }

        void Release()
        {
            _isHeld = false;

            _rigidbody.position = _root.position;
            _rigidbody.rotation = _root.rotation;
            _rigidbody.isKinematic = false;
            _rigidbody.interpolation = _interpolation;
            _rigidbody.velocity = Vector3.zero;
        }

        // From the body's collisions: a contact steeper than walkable, and not a ceiling, is a wall.
        internal void AddContact(Vector3 normal)
        {
            var angle = Vector3.Angle(normal, Vector3.up);

            if (angle > _settings.MaxSlopeAngle && angle < 180f - _settings.MaxSlopeAngle)
                _blockedSideways = true;
        }

        bool CheckGround(out Vector3 normal, out float gap)
        {
            var maxGap = _wasGrounded
                ? Mathf.Max(_settings.GroundDistance, _settings.GroundSnapDistance)
                : _settings.GroundDistance;

            return FindGround(maxGap, out normal, out gap);
        }

        // The closest walkable ground within maxGap below the capsule, and how far below it is.
        bool FindGround(float maxGap, out Vector3 normal, out float gap)
        {
            normal = Vector3.up;
            gap = 0f;

            var capsuleRadius = _capsule.radius;
            var probeRadius = capsuleRadius * ProbeRadiusScale;

            // From the probe sphere's bottom to the capsule's bottom.
            var probeStart = CastOffset + (capsuleRadius - probeRadius);
            var origin = BottomSphereCentre(Position) + Vector3.up * CastOffset;

            var hitCount = Physics.SphereCastNonAlloc(
                origin,
                probeRadius,
                Vector3.down,
                _hits,
                probeStart + maxGap,
                _settings.GroundLayers,
                QueryTriggerInteraction.Ignore);

            var found = false;
            var closest = float.PositiveInfinity;

            for (var i = 0; i < hitCount; i++)
            {
                var hit = _hits[i];

                if (hit.distance <= 0f || IsOwn(hit.collider) || hit.distance >= closest)
                    continue;

                if (Vector3.Angle(hit.normal, Vector3.up) > _settings.MaxSlopeAngle)
                    continue;

                found = true;
                closest = hit.distance;
                normal = hit.normal;
            }

            if (found)
                gap = Mathf.Max(0f, closest - probeStart);

            return found;
        }

        // Lifts the character onto a step it's about to walk into, if the step is no higher than StepHeight,
        // walkable on top and has room for the capsule.
        bool TryStepUp(Vector3 horizontal, float deltaTime)
        {
            var direction = horizontal.normalized;
            var distance = horizontal.magnitude * deltaTime + StepLookAhead;

            if (!_rigidbody.SweepTest(direction, out var obstacle, distance, QueryTriggerInteraction.Ignore))
                return false;

            // A walkable slope: walked up as normal.
            if (Vector3.Angle(obstacle.normal, Vector3.up) <= _settings.MaxSlopeAngle)
                return false;

            var bottomY = BottomSphereCentre(_rigidbody.position).y - _capsule.radius;
            var stepHeight = _settings.StepHeight;

            var rayOrigin = obstacle.point + direction * StepProbeInset;
            rayOrigin.y = bottomY + stepHeight + CastOffset;

            if (!Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out var top,
                    stepHeight + CastOffset,
                    _settings.GroundLayers,
                    QueryTriggerInteraction.Ignore))
                return false;

            var rise = top.point.y - bottomY;

            if (rise <= 0f || rise > stepHeight || Vector3.Angle(top.normal, Vector3.up) > _settings.MaxSlopeAngle)
                return false;

            var raised = _rigidbody.position + Vector3.up * (rise + StepClearance);

            if (Overlaps(raised))
                return false;

            _rigidbody.position = raised;
            return true;
        }

        bool Overlaps(Vector3 position)
        {
            var bottom = BottomSphereCentre(position);
            var top = bottom + _rigidbody.rotation * Vector3.up * Mathf.Max(0f, _capsule.height - 2f * _capsule.radius);

            var count = Physics.OverlapCapsuleNonAlloc(
                bottom,
                top,
                _capsule.radius * ProbeRadiusScale,
                _overlaps,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore);

            for (var i = 0; i < count; i++)
            {
                if (!IsOwn(_overlaps[i]))
                    return true;
            }

            return false;
        }

        // The centre of the capsule's lower sphere, for an upright capsule at position.
        Vector3 BottomSphereCentre(Vector3 position)
        {
            var halfHeight = Mathf.Max(_capsule.height * 0.5f, _capsule.radius);
            return position + _rigidbody.rotation * (_capsule.center + Vector3.down * (halfHeight - _capsule.radius));
        }

        bool IsOwn(Collider collider) =>
            collider.attachedRigidbody == _rigidbody || collider.transform.IsChildOf(_root);
    }
}
