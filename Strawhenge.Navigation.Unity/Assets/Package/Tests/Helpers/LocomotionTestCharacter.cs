using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Strawhenge.Navigation.Unity.Tests
{
    static class LocomotionTestCharacter
    {
        // Builds a character with a motor (a CharacterController, or a Rigidbody and CapsuleCollider) and
        // LocomotionScript. Either way the capsule is 2 m tall and centred on the transform, so its feet are 1 m
        // below position. It is built inactive so the settings are in place before Awake runs. Adds it to created
        // for tear-down.
        public static LocomotionScript Create(
            ILocomotionSettings settings,
            Vector3 position,
            ICollection<Object> created,
            Quaternion? rotation = null,
            TestMotor motor = TestMotor.CharacterController)
        {
            var character = new GameObject("Character");
            character.SetActive(false);
            created.Add(character);

            character.transform.SetPositionAndRotation(position, rotation ?? Quaternion.identity);
            var locomotion = character.AddComponent<LocomotionScript>();
            locomotion.SetMotor(AddMotor(character, motor));
            locomotion.SetSettings(settings);
            locomotion.SetLogger(new FakeLogger());

            character.SetActive(true);
            return locomotion;
        }

        static CharacterMotorScript AddMotor(GameObject character, TestMotor motor)
        {
            if (motor == TestMotor.Rigidbody)
            {
                var capsule = character.AddComponent<CapsuleCollider>();
                capsule.height = 2f;
                capsule.radius = 0.5f;
                capsule.center = Vector3.zero;

                // Adds the Rigidbody it requires.
                return character.AddComponent<RigidbodyMotorScript>();
            }

            var characterController = character.AddComponent<CharacterController>();
            var characterControllerMotor = character.AddComponent<CharacterControllerMotorScript>();
            characterControllerMotor.SetCharacterController(characterController);
            return characterControllerMotor;
        }
    }
}
