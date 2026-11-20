using KinematicCharacterController;
using Strawhenge.Navigation.Unity;
using UnityEngine;

namespace Development
{
    // POC: put on the character's root with KCC's KinematicCharacterMotor (and its CapsuleCollider), and assign
    // it as LocomotionScript's motor. Add a RootMotionRelayScript to the Animator's GameObject for pivots.
    [RequireComponent(typeof(KinematicCharacterMotor))]
    public sealed class KccMotorScript : CharacterMotorScript
    {
        ICharacterMotor _motor;

        // Created on first use, as LocomotionScript asks for it in its Awake.
        public override ICharacterMotor Motor => _motor ??= new KccMotor(GetComponent<KinematicCharacterMotor>());
    }
}
