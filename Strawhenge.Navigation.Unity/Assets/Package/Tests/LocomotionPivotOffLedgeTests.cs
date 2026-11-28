using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Strawhenge.Navigation.Unity.Tests
{
    // A pivot whose root motion carries the character off a ledge, on each motor. Nothing lets the character fall
    // while a pivot holds it still, so the pivot used to play out with the character hovering, and it only fell
    // afterwards. The root motion is played by hand, the way the Animator applies it: to the motor when it takes it
    // (IRootMotionHandler, the rigidbody motor), otherwise through the CharacterController.
    [TestFixture(TestMotor.CharacterController)]
    [TestFixture(TestMotor.Rigidbody)]
    public class LocomotionPivotOffLedgeTests
    {
        const int PivotId = 4;
        const float Timeout = 1f;

        // The ledge is a 10 m x 10 m platform centred on the origin, with its top at y = 0 and an edge at x = 5.
        // The character stands 1 m from that edge, facing along it (+z).
        static readonly Vector3 StartPosition = new(4f, 1.05f, 0f);

        // A 90 degree turn to the right from standing.
        static readonly IPivotProfile StationaryPivot = new FakePivotProfile(
            PivotId,
            speedRange: new ValueRange(0f, 0.5f),
            new ValueRange(60f, 120f));

        readonly List<Object> _created = new();
        readonly TestMotor _testMotor;
        LocomotionScript _locomotion;
        ICharacterMotor _motor;
        bool _pivotRequested;
        bool _pivotCancelled;

        public LocomotionPivotOffLedgeTests(TestMotor testMotor)
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
            _pivotRequested = false;
            _pivotCancelled = false;

            // Destroy is deferred to the end of the frame; wait so the next test starts clean.
            yield return null;
        }

        [UnityTest]
        public IEnumerator Pivot_is_cancelled_when_its_root_motion_carries_the_character_off_a_ledge()
        {
            yield return StartPivotNearTheEdge();

            PlayRootMotion(Vector3.right * 2f); // Over the edge.
            yield return WaitFor(() => _pivotCancelled);

            Assert.IsTrue(_pivotCancelled);
        }

        // Baseline for the test above. Root motion through a CharacterController doesn't push into the ground, so
        // the CharacterController doesn't count as grounded after it, even on flat ground.
        [UnityTest]
        public IEnumerator Pivot_is_not_cancelled_while_its_root_motion_keeps_the_character_on_the_ground()
        {
            yield return StartPivotNearTheEdge();

            PlayRootMotion(Vector3.left * 2f); // Away from the edge.
            yield return new WaitForSeconds(0.3f);

            Assert.IsFalse(_pivotCancelled);
        }

        [UnityTest]
        public IEnumerator Character_falls_straight_away_when_a_pivot_carries_it_off_a_ledge()
        {
            yield return StartPivotNearTheEdge();
            var startHeight = _motor.Position.y;

            PlayRootMotion(Vector3.right * 2f);
            yield return new WaitForSeconds(0.5f);

            Assert.Less(_motor.Position.y, startHeight - 0.5f, "The character should be falling.");
        }

        IEnumerator StartPivotNearTheEdge()
        {
            CreateBox(new Vector3(0, -0.5f, 0), new Vector3(10, 1, 10));

            _locomotion = LocomotionTestCharacter.Create(
                new FakeLocomotionSettings { StationaryPivots = new[] { StationaryPivot } },
                StartPosition,
                _created,
                motor: _testMotor);

            _motor = _locomotion.GetComponent<CharacterMotorScript>().Motor;

            var controller = _locomotion.LocomotionController;
            controller.PivotRequested += _ => _pivotRequested = true;
            controller.PivotCancelled += () => _pivotCancelled = true;

            yield return WaitFor(() => _motor.IsGrounded);
            Assert.IsTrue(_motor.IsGrounded, "Setup failed: the character never stood on the platform.");

            // A flick to the right: a stationary pivot.
            controller.Move(Vector3.right);
            yield return new WaitForFixedUpdate();
            yield return null;
            controller.Move(Vector3.zero);

            yield return WaitFor(() => _pivotRequested);
            Assert.IsTrue(_pivotRequested, "Setup failed: no pivot was requested.");
        }

        void PlayRootMotion(Vector3 deltaPosition)
        {
            if (_motor is IRootMotionHandler handler && handler.TryApplyRootMotion(deltaPosition, Quaternion.identity))
                return;

            _locomotion.GetComponent<CharacterController>().Move(deltaPosition);
        }

        void CreateBox(Vector3 centre, Vector3 size)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.position = centre;
            box.transform.localScale = size;
            _created.Add(box);
        }

        static IEnumerator WaitFor(Func<bool> condition)
        {
            for (var time = 0f; time < Timeout && !condition(); time += Time.deltaTime)
                yield return null;
        }
    }
}
