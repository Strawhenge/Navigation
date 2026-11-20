using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Strawhenge.Navigation.Unity.Tests
{
    // The rigidbody motor on real physics: ground, slopes, walls and steps. The motor's defaults: grounded within
    // 0.05 m, walkable up to 45 degrees, steps up to 0.3 m.
    public class RigidbodyMotorTests
    {
        const float SettleTime = 0.3f;
        const float RunTime = 1.5f;

        readonly List<Object> _created = new();
        LocomotionScript _locomotion;
        ICharacterMotor _motor;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var obj in _created)
            {
                if (obj != null)
                    Object.Destroy(obj);
            }

            _created.Clear();

            // Destroy is deferred to the end of the frame; wait so the next test starts clean.
            yield return null;
        }

        [UnityTest]
        public IEnumerator Is_grounded_standing_on_flat_ground()
        {
            CreateGround();
            CreateCharacter(new Vector3(0, 1.1f, 0)); // Feet 0.1 m above the ground.

            yield return new WaitForSeconds(SettleTime);

            Assert.IsTrue(_motor.IsGrounded, "The character should be grounded on the floor.");
            Assert.AreEqual(1f, _motor.Position.y, 0.05f, "The character should be standing on the floor.");
        }

        [UnityTest]
        public IEnumerator Does_not_slide_down_a_slope_when_standing_still()
        {
            CreateGround();
            var slope = CreateBox(new Vector3(0, 0, 0), new Vector3(10, 1, 10));
            slope.transform.rotation = Quaternion.Euler(30, 0, 0); // Rises towards -z.
            CreateCharacter(new Vector3(0, 2f, 0)); // About 0.5 m above the slope.

            yield return new WaitForSeconds(1f);
            Assert.IsTrue(_motor.IsGrounded, "Setup failed: the character never landed on the slope.");

            var start = _motor.Position;
            yield return new WaitForSeconds(1f);

            Assert.Less(Vector3.Distance(start, _motor.Position), 0.05f, "The character slid down the slope.");
        }

        [UnityTest]
        public IEnumerator Is_stopped_by_a_wall()
        {
            CreateGround();
            CreateBox(new Vector3(0, 1.5f, 2.5f), new Vector3(4, 3, 1)); // Its near face is at z = 2.
            CreateCharacter(new Vector3(0, 1.1f, 0));
            yield return new WaitForSeconds(SettleTime);

            _locomotion.LocomotionController.Move(Vector3.forward);
            yield return new WaitForSeconds(RunTime);

            Assert.Less(_motor.Position.z, 2f - 0.4f, "The character went into the wall.");
            Assert.Greater(_motor.Position.z, 1f, "Setup failed: the character never reached the wall.");
        }

        // Baseline for the test below: the same run at a lower step gets onto it.
        [UnityTest]
        public IEnumerator Walks_up_a_step_no_higher_than_step_height()
        {
            yield return RunAtStep(stepHeight: 0.2f);

            Assert.Greater(_motor.Position.z, 3f, "The character should have walked onto the step.");
            Assert.AreEqual(1.2f, _motor.Position.y, 0.05f, "The character should be standing on the step.");
        }

        [UnityTest]
        public IEnumerator Is_stopped_by_a_step_higher_than_step_height()
        {
            yield return RunAtStep(stepHeight: 0.6f);

            Assert.Less(_motor.Position.z, 2f, "The character shouldn't have got onto the step.");
            Assert.AreEqual(1f, _motor.Position.y, 0.05f, "The character should still be on the floor.");
        }

        // Runs forward at a step whose near face is 2 m ahead, and which is deep enough not to run off the end.
        IEnumerator RunAtStep(float stepHeight)
        {
            CreateGround();
            CreateBox(new Vector3(0, stepHeight / 2, 7f), new Vector3(4, stepHeight, 10));
            CreateCharacter(new Vector3(0, 1.1f, 0));
            yield return new WaitForSeconds(SettleTime);
            Assert.IsTrue(_motor.IsGrounded, "Setup failed: the character never landed on the floor.");

            _locomotion.LocomotionController.Move(Vector3.forward);
            yield return new WaitForSeconds(RunTime);
        }

        void CreateGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(10, 1, 10); // 100 m x 100 m at y = 0
            _created.Add(ground);
        }

        GameObject CreateBox(Vector3 centre, Vector3 size)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.position = centre;
            box.transform.localScale = size;
            _created.Add(box);
            return box;
        }

        void CreateCharacter(Vector3 position)
        {
            _locomotion = LocomotionTestCharacter.Create(
                new FakeLocomotionSettings(),
                position,
                _created,
                motor: TestMotor.Rigidbody);

            _motor = _locomotion.GetComponent<CharacterMotorScript>().Motor;
        }
    }
}
