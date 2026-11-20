using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Strawhenge.Navigation.Unity.Tests
{
    // Running down a ramp keeps the character on it. The CharacterController motor used to push down at a fixed
    // speed while grounded, too slow to follow a steep ramp at a sprint, so the character flew off and landed
    // further down.
    [TestFixture(TestMotor.CharacterController)]
    [TestFixture(TestMotor.Rigidbody)]
    public class LocomotionSlopeTests
    {
        const float RampAngle = 30f;
        const float RampLength = 8f;
        const float PlatformHeight = 4f; // RampLength * sin(RampAngle): the ramp ends on the ground.
        const float SettleTime = 0.3f;
        const float Timeout = 10f;

        // Briefly losing the ground is fine (the Rigidbody motor checks it once per physics step). Flying off the
        // ramp at a sprint lasts about half a second.
        const float MaxTimeUngrounded = 0.1f;

        // Skipped at each end of the ramp: the crest and the foot.
        const float EndMargin = 0.5f;

        static readonly float RampBottomX = RampLength * Mathf.Cos(RampAngle * Mathf.Deg2Rad);

        readonly List<Object> _created = new();
        readonly TestMotor _testMotor;
        LocomotionScript _locomotion;
        ICharacterMotor _motor;

        public LocomotionSlopeTests(TestMotor testMotor)
        {
            _testMotor = testMotor;
        }

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

        // Baseline for the test below: the setup can tell the character stays on the ramp.
        [UnityTest]
        public IEnumerator Stays_grounded_walking_down_a_ramp()
        {
            yield return RunDownRamp(startX: -1.5f, walk: true, sprint: false);
        }

        [UnityTest]
        public IEnumerator Stays_grounded_sprinting_down_a_ramp()
        {
            // Starts far enough back to reach full speed before the crest.
            yield return RunDownRamp(startX: -8f, walk: false, sprint: true);
        }

        IEnumerator RunDownRamp(float startX, bool walk, bool sprint)
        {
            CreateRamp();
            CreateCharacter(new Vector3(startX, PlatformHeight + 1.05f, 0));
            yield return new WaitForSeconds(SettleTime);
            Assert.IsTrue(_motor.IsGrounded, "Setup failed: the character never landed on the platform.");

            var controller = _locomotion.LocomotionController;
            controller.Walk = walk;
            controller.Sprint = sprint;
            controller.Move(Vector3.right);

            var elapsed = 0f;
            var timeUngrounded = 0f;
            var longestTimeUngrounded = 0f;

            while (_motor.Position.x < RampBottomX - EndMargin)
            {
                Assert.Less(elapsed, Timeout, "Setup failed: the character never reached the bottom of the ramp.");
                yield return null;
                elapsed += Time.deltaTime;

                if (_motor.Position.x < EndMargin)
                    continue;

                if (_motor.IsGrounded)
                {
                    timeUngrounded = 0f;
                    continue;
                }

                timeUngrounded += Time.deltaTime;
                longestTimeUngrounded = Mathf.Max(longestTimeUngrounded, timeUngrounded);
            }

            Assert.Less(longestTimeUngrounded, MaxTimeUngrounded,
                $"The character left the ramp for {longestTimeUngrounded:0.00} s.");
        }

        // A platform whose top is at PlatformHeight and which ends at x = 0, then a ramp down towards +x onto the
        // ground. 4 m wide, centred on z = 0.
        void CreateRamp()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(10, 1, 10); // 100 m x 100 m at y = 0
            _created.Add(ground);

            CreateBox(new Vector3(-5, PlatformHeight - 0.5f, 0), new Vector3(10, 1, 4), Quaternion.identity);

            // Tilted down towards +x, with the middle of its top face halfway down the slope.
            var rotation = Quaternion.Euler(0, 0, -RampAngle);
            var down = rotation * Vector3.right;
            var normal = rotation * Vector3.up;
            var topCentre = new Vector3(0, PlatformHeight, 0) + down * (RampLength / 2);
            CreateBox(topCentre - normal * 0.5f, new Vector3(RampLength, 1, 4), rotation);
        }

        void CreateBox(Vector3 centre, Vector3 size, Quaternion rotation)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.SetPositionAndRotation(centre, rotation);
            box.transform.localScale = size;
            _created.Add(box);
        }

        void CreateCharacter(Vector3 position)
        {
            _locomotion = LocomotionTestCharacter.Create(
                new FakeLocomotionSettings(),
                position,
                _created,
                motor: _testMotor);

            _motor = _locomotion.GetComponent<CharacterMotorScript>().Motor;
        }
    }
}
