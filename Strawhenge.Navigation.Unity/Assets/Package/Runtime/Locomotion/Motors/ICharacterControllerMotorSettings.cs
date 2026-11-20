namespace Strawhenge.Navigation.Unity
{
    public interface ICharacterControllerMotorSettings
    {
        // Vertical speed (negative, metres per second) applied while grounded and not rising. CharacterController
        // is only grounded after a move that pushes into the ground, and locomotion asks for none while grounded.
        float GroundedPush { get; }
    }
}
