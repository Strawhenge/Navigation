using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Strawhenge.Navigation.Unity.Tests
{
    // Coyote time: a jump is allowed for CoyoteTime after leaving the ground, then not until landing.
    // It used to count only once the character had dropped FallDistance, so a jump was allowed
    // throughout the first metre of a fall, and throughout any drop shorter than FallDistance.
    public class LocomotionCoyoteTimeTests
    {
        const float CoyoteTime = 0.2f;
        const float WithinCoyoteTime = 0.1f;
        const float AfterCoyoteTime = 0.35f;
        const float JumpObservationTime = 0.1f;
        const float SettleTime = 0.2f;
        const float Timeout = 3f;

        // The ledge is a 10 m x 10 m platform centred on the origin; the character runs off at x = 5.
        const float LedgeEdgeX = 5f;

        readonly List<Object> _created = new();
        LocomotionScript _locomotion;
        CharacterController _characterController;

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

        // Baseline: proves a jump off a ledge works in this harness, so the tests below can't pass
        // just because jumping never works.
        [UnityTest]
        public IEnumerator Can_jump_just_after_running_off_a_ledge()
        {
            yield return RunOffLedge(ledgeHeight: 5, new FakeLocomotionSettings { CoyoteTime = CoyoteTime });
            yield return new WaitForSeconds(WithinCoyoteTime);

            yield return AssertJump(expected: true, "A jump within coyote time of leaving the ledge should start.");
        }

        // About 1.3 m into the fall: the old timer had only just started at FallDistance (1 m).
        [UnityTest]
        public IEnumerator Cannot_jump_after_coyote_time_has_passed()
        {
            yield return RunOffLedge(ledgeHeight: 5, new FakeLocomotionSettings { CoyoteTime = CoyoteTime });
            yield return new WaitForSeconds(AfterCoyoteTime);

            yield return AssertJump(expected: false, "A jump after coyote time has passed should not start.");
        }

        // A 2 m drop with FallDistance 3 m: the old timer never started, however late the jump.
        [UnityTest]
        public IEnumerator Cannot_jump_late_in_a_drop_shorter_than_fall_distance()
        {
            yield return RunOffLedge(
                ledgeHeight: 2,
                new FakeLocomotionSettings { CoyoteTime = CoyoteTime, FallDistance = 3 });
            yield return new WaitForSeconds(AfterCoyoteTime);

            yield return AssertJump(expected: false, "A jump late in a short drop should not start.");
        }

        IEnumerator RunOffLedge(float ledgeHeight, ILocomotionSettings settings)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(10, 1, 10); // 100 m x 100 m at y = 0
            _created.Add(ground);

            var ledge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ledge.transform.localScale = new Vector3(LedgeEdgeX * 2, ledgeHeight, LedgeEdgeX * 2);
            ledge.transform.position = new Vector3(0, ledgeHeight / 2, 0);
            _created.Add(ledge);

            // On the ledge (feet 0.1 m above it), facing the edge.
            _locomotion = LocomotionTestCharacter.Create(
                settings,
                new Vector3(2, ledgeHeight + 1.1f, 0),
                _created,
                Quaternion.LookRotation(Vector3.right));
            _characterController = _locomotion.GetComponent<CharacterController>();

            yield return new WaitForSeconds(SettleTime);
            Assert.IsTrue(_characterController.isGrounded, "Setup failed: the character never landed on the ledge.");

            // Off the ledge = past its edge and not grounded, so a grounded flicker on top can't count.
            _locomotion.Move(Vector3.right);
            yield return WaitUntilOrTimeout(() =>
                _locomotion.transform.position.x > LedgeEdgeX && !_characterController.isGrounded);

            Assert.IsFalse(_characterController.isGrounded, "Setup failed: the character never ran off the ledge.");
        }

        IEnumerator AssertJump(bool expected, string message)
        {
            Assert.IsFalse(_characterController.isGrounded, "Setup failed: the character had already landed.");

            var jumpBegan = false;
            _locomotion.JumpBegan += () => jumpBegan = true;

            _locomotion.Jump();
            yield return new WaitForSeconds(JumpObservationTime);

            Assert.AreEqual(expected, jumpBegan, message);
        }

        static IEnumerator WaitUntilOrTimeout(Func<bool> condition)
        {
            var elapsed = 0f;

            while (!condition() && elapsed < Timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }
}
