using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    // Moves the character for LocomotionController: a CharacterController, a Rigidbody, a third-party
    // controller... The controller decides how the character should move; the motor carries it out.
    public interface ICharacterMotor
    {
        Vector3 Position { get; }

        Quaternion Rotation { get; }

        // The velocity the last move achieved (walls included), read straight after it. Needn't be kept while the
        // character isn't moved: the controller keeps it through pivots.
        Vector3 Velocity { get; }

        // Whether the character is on the ground: as of the last move, or where something else has moved it since.
        // A pivot's root motion moves it while no steps move (often without pushing into the ground), and
        // locomotion gives up a pivot that carries the character off a ledge.
        bool IsGrounded { get; }

        // When LocomotionScript steps the controller. External: the motor steps it itself, through the
        // ILocomotionStepper it's given (see IExternallySteppedMotor).
        MotorUpdateMode UpdateMode { get; }

        // Off while locomotion is disabled, so something else (a NavMeshAgent, a ragdoll) can move the character.
        void SetActive(bool active);

        // Turns the character straight away, outside a step (a completed pivot).
        void SetRotation(Quaternion rotation);

        // Moves by step.Velocity over deltaTime and turns to step.Rotation. Only called for steps that move.
        MotorMoveResult Move(LocomotionStep step, float deltaTime);
    }
}
