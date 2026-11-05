using UnityEngine;

namespace Strawhenge.Navigation.Unity
{
    // Test hooks. Call them before Awake (while the GameObject is inactive); Awake keeps what was set.
    public sealed partial class LocomotionScript
    {
        internal void SetCharacterController(CharacterController characterController) =>
            _characterController = characterController;

        internal void SetSettings(ILocomotionSettings settings)
        {
            _settingsSetFromCode = true;
            _locomotionSettings = new CachedLocomotionSettings(settings);
        }
    }
}
