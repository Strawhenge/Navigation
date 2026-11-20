using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    // Moves the character with the Rigidbody and CapsuleCollider on this GameObject (the character's root).
    // Runs after LocomotionScript, so at the end of each physics step it knows whether it was moved.
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

        ICharacterMotor _motor;
        RigidbodyMotor _rigidbodyMotor;

        // Created on first use, as LocomotionScript asks for it in its Awake.
        public override ICharacterMotor Motor => _motor ??= Create();

        LayerMask IRigidbodyMotorSettings.GroundLayers => _groundLayers;

        float IRigidbodyMotorSettings.GroundDistance => _groundDistance;

        float IRigidbodyMotorSettings.GroundSnapDistance => _groundSnapDistance;

        float IRigidbodyMotorSettings.MaxSlopeAngle => _maxSlopeAngle;

        float IRigidbodyMotorSettings.StepHeight => _stepHeight;

        void FixedUpdate()
        {
            _rigidbodyMotor?.EndFixedStep();
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
            return _rigidbodyMotor;
        }
    }
}
