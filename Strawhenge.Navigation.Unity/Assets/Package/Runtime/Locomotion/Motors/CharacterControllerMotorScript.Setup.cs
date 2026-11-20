using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    // Test hooks. Call them before Motor is first read (before LocomotionScript's Awake).
    public sealed partial class CharacterControllerMotorScript
    {
        internal void SetCharacterController(CharacterController characterController) =>
            _characterController = characterController;
    }
}
