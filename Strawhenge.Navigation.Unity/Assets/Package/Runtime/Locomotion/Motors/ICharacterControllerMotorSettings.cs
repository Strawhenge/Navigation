namespace Strawhenge.Navigation.Unity
{
    public interface ICharacterControllerMotorSettings
    {
        // The least vertical speed (negative, metres per second) applied while grounded and not rising.
        // CharacterController is only grounded after a move that pushes into the ground, and locomotion asks for
        // none while grounded. Moving faster pushes harder, enough to follow any slope up to its slopeLimit.
        float GroundedPush { get; }
    }
}
