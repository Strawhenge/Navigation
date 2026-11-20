using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    // Moves the character with the Rigidbody and CapsuleCollider on this GameObject (the character's root).
    // Runs after LocomotionScript, so at the end of each physics step it knows whether it was moved.
    //
    // Pivots need a RootMotionRelayScript on the Animator's GameObject: the motor takes the root motion it relays
    // (MovementAnimationSyncScript turns it on for pivots) and applies it to the transform, holding the body
    // meanwhile. The Animator's own root motion would go through the body, which the motor holds still and won't
    // let turn, so a pivot's turn would be lost. Setups without pivots don't need the relay.
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class RigidbodyMotorScript : CharacterMotorScript, IRigidbodyMotorSettings
    {
        [Tooltip("What counts as ground (and steps). Leave out the character's own layers.")]
        [SerializeField] LayerMask _groundLayers = ~0;

        [Tooltip("Within this of the ground (metres, below the capsule) counts as grounded.")]
        [SerializeField] float _groundDistance = 0.05f;

        [Tooltip("While grounded, ground within this below is snapped down to, so the character follows slopes " +
                 "and steps down instead of skipping off them.")]
        [SerializeField] float _groundSnapDistance = 0.3f;

        [Tooltip("Steeper ground isn't walkable, and contacts steeper than this count as walls.")]
        [Range(0f, 89f)]
        [SerializeField] float _maxSlopeAngle = 45f;

        [Tooltip("The highest step the character walks straight up.")]
        [SerializeField] float _stepHeight = 0.3f;

        [Tooltip("Optional: on the Animator's GameObject. Needed for pivots to turn the character. " +
                 "Found in the hierarchy if not set.")]
        [SerializeField] RootMotionRelayScript _rootMotionRelay;

        ICharacterMotor _motor;
        RigidbodyMotor _rigidbodyMotor;
        bool _warnedNoRootMotionRelay;

        // Created on first use, as LocomotionScript asks for it in its Awake.
        public override ICharacterMotor Motor => _motor ??= Create();

        LayerMask IRigidbodyMotorSettings.GroundLayers => _groundLayers;

        float IRigidbodyMotorSettings.GroundDistance => _groundDistance;

        float IRigidbodyMotorSettings.GroundSnapDistance => _groundSnapDistance;

        float IRigidbodyMotorSettings.MaxSlopeAngle => _maxSlopeAngle;

        float IRigidbodyMotorSettings.StepHeight => _stepHeight;

        void FixedUpdate()
        {
            if (_rigidbodyMotor == null)
                return;

            // Only a pivot leaves a physics step without a move, so warn then rather than for every setup: those
            // without pivots don't need the relay.
            if (_rigidbodyMotor.EndFixedStep() && _rootMotionRelay == null && !_warnedNoRootMotionRelay)
            {
                _warnedNoRootMotionRelay = true;
                Debug.LogWarning(
                    $"A pivot is playing without a {nameof(RootMotionRelayScript)} on the Animator's GameObject, " +
                    "so its turn won't move the character.",
                    this);
            }
        }

        void OnCollisionEnter(Collision collision) => AddContacts(collision);

        void OnCollisionStay(Collision collision) => AddContacts(collision);

        void AddContacts(Collision collision)
        {
            if (_rigidbodyMotor == null)
                return;

            for (var i = 0; i < collision.contactCount; i++)
                _rigidbodyMotor.AddContact(collision.GetContact(i).normal);
        }

        ICharacterMotor Create()
        {
            var body = GetComponent<Rigidbody>();
            var capsule = GetComponent<CapsuleCollider>();

            if (body == null || capsule == null)
            {
                Debug.LogError("A Rigidbody and a CapsuleCollider are needed on this GameObject - the character won't move.", this);
                return NullCharacterMotor.Instance;
            }

            // Locomotion owns gravity and turning; the body only resolves collisions.
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            // Without friction the character doesn't stick to walls, and stops only when locomotion says so.
            if (capsule.sharedMaterial == null)
            {
                capsule.sharedMaterial = new PhysicMaterial("Character (frictionless)")
                {
                    dynamicFriction = 0f,
                    staticFriction = 0f,
                    frictionCombine = PhysicMaterialCombine.Minimum,
                    bounciness = 0f,
                    bounceCombine = PhysicMaterialCombine.Minimum
                };
            }

            _rigidbodyMotor = new RigidbodyMotor(body, capsule, this);

            if (_rootMotionRelay == null)
                _rootMotionRelay = GetComponentInChildren<RootMotionRelayScript>(includeInactive: true);

            if (_rootMotionRelay != null)
                _rootMotionRelay.Handler = _rigidbodyMotor;

            return _rigidbodyMotor;
        }
    }
}
