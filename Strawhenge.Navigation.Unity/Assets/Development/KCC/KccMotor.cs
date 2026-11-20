using KinematicCharacterController;
using Strawhenge.Navigation.Unity;
using UnityEngine;

namespace Development
{
    // POC: the Kinematic Character Controller asset as a locomotion motor. KCC runs its own physics-step loop and
    // calls back for rotation and velocity, so this is an externally stepped motor: each KCC update is one
    // locomotion step (BeginStep before it, EndStep after). Root motion (pivots) is applied straight to KCC's
    // state each frame, past its interpolation.
    public sealed class KccMotor : IExternallySteppedMotor, IRootMotionHandler, ICharacterController
    {
        readonly KinematicCharacterMotor _motor;
        readonly Transform _root;

        ILocomotionStepper _stepper;
        LocomotionStep _step;
        bool _isActive;
        bool _blockedSideways;

        public KccMotor(KinematicCharacterMotor motor)
        {
            _motor = motor;
            _root = motor.transform;
            _motor.CharacterController = this;
        }

        public Vector3 Position => _motor.TransientPosition;

        public Quaternion Rotation => _motor.TransientRotation;

        // KCC's velocity after the last step (collisions included). The controller keeps it through pivots.
        public Vector3 Velocity => _motor.BaseVelocity;

        public bool IsGrounded => _motor.GroundingStatus.IsStableOnGround;

        public MotorUpdateMode UpdateMode => MotorUpdateMode.External;

        public void SetStepper(ILocomotionStepper stepper) => _stepper = stepper;

        // Off: KCC stops simulating (and writing the transform), and its capsule is off, so a NavMeshAgent or
        // ragdoll can move the character. On: picks up wherever the character was moved meanwhile.
        public void SetActive(bool active)
        {
            _isActive = active;
            _blockedSideways = false;
            _motor.BaseVelocity = Vector3.zero;

            if (active)
            {
                _motor.Capsule.enabled = true;
                _motor.SetPositionAndRotation(_root.position, _root.rotation);
                _motor.enabled = true;
            }
            else
            {
                _motor.enabled = false;
                _motor.Capsule.enabled = false;
            }
        }

        public void SetRotation(Quaternion rotation) => _motor.SetRotation(rotation);

        // Only externally stepped: KCC calls UpdateRotation / UpdateVelocity instead.
        public MotorMoveResult Move(LocomotionStep step, float deltaTime) => default;

        // A frame of the Animator's root motion (a pivot's turn and steps). Set straight on KCC's state, bypassing
        // its interpolation, which would otherwise overwrite the transform in LateUpdate.
        public bool TryApplyRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
        {
            if (!_isActive)
                return false;

            _motor.SetPositionAndRotation(
                _motor.TransientPosition + new Vector3(deltaPosition.x, 0f, deltaPosition.z),
                deltaRotation * _motor.TransientRotation);

            return true;
        }

        void ICharacterController.BeforeCharacterUpdate(float deltaTime)
        {
            _blockedSideways = false;
            _step = _stepper != null
                ? _stepper.BeginStep(deltaTime)
                : LocomotionStep.Stationary(_motor.TransientRotation);

            // KCC must be told to leave the ground for a jump, or its ground snapping pulls the character back down.
            if (_step.ShouldMove && _step.JumpStarted)
                _motor.ForceUnground();
        }

        void ICharacterController.UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            currentRotation = _step.Rotation;
        }

        void ICharacterController.UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (!_step.ShouldMove)
            {
                currentVelocity = Vector3.zero;
                return;
            }

            var velocity = _step.Velocity;
            var grounding = _motor.GroundingStatus;

            // On the ground and not jumping: along it at the same speed. KCC snaps to the ground itself.
            if (grounding.IsStableOnGround && !_motor.MustUnground())
            {
                var horizontal = new Vector3(velocity.x, 0f, velocity.z);
                currentVelocity = horizontal.sqrMagnitude > 0f
                    ? _motor.GetDirectionTangentToSurface(horizontal, grounding.GroundNormal) * horizontal.magnitude
                    : Vector3.zero;

                return;
            }

            currentVelocity = velocity;
        }

        void ICharacterController.PostGroundingUpdate(float deltaTime)
        {
        }

        void ICharacterController.AfterCharacterUpdate(float deltaTime)
        {
            if (!_step.ShouldMove)
                return;

            _stepper?.EndStep(new MotorMoveResult(_blockedSideways));
        }

        // The character's own colliders (the ragdoll's) aren't obstacles.
        bool ICharacterController.IsColliderValidForCollisions(Collider coll) => !coll.transform.IsChildOf(_root);

        void ICharacterController.OnGroundHit(
            Collider hitCollider,
            Vector3 hitNormal,
            Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
        }

        // An unstable hit that's mostly horizontal is a wall.
        void ICharacterController.OnMovementHit(
            Collider hitCollider,
            Vector3 hitNormal,
            Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
            if (!hitStabilityReport.IsStable && Mathf.Abs(Vector3.Dot(hitNormal, _motor.CharacterUp)) < 0.7f)
                _blockedSideways = true;
        }

        void ICharacterController.ProcessHitStabilityReport(
            Collider hitCollider,
            Vector3 hitNormal,
            Vector3 hitPoint,
            Vector3 atCharacterPosition,
            Quaternion atCharacterRotation,
            ref HitStabilityReport hitStabilityReport)
        {
        }

        void ICharacterController.OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }
    }
}
