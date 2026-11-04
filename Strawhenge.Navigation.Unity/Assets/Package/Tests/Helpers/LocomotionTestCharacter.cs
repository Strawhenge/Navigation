using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Strawhenge.Navigation.Unity.Tests
{
    static class LocomotionTestCharacter
    {
        // Builds a character with a CharacterController and LocomotionScript. The default controller is
        // 2 m tall and centred on the transform, so its feet are 1 m below position. It is built
        // inactive so the settings are in place before Awake runs. Adds it to created for tear-down.
        public static LocomotionScript Create(
            ILocomotionSettings settings,
            Vector3 position,
            ICollection<Object> created,
            Quaternion? rotation = null)
        {
            var character = new GameObject("Character");
            character.SetActive(false);
            created.Add(character);

            character.transform.SetPositionAndRotation(position, rotation ?? Quaternion.identity);
            var characterController = character.AddComponent<CharacterController>();

            var locomotion = character.AddComponent<LocomotionScript>();
            locomotion.SetCharacterController(characterController);
            locomotion.SetSettings(settings);

            character.SetActive(true);
            return locomotion;
        }
    }
}
