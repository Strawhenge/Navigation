using NUnit.Framework;
using Strawhenge.Common.Ranges;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Strawhenge.Navigation.Unity.Tests
{
    // LocomotionScript must recover when it is disabled part-way through something that waits for
    // the Animator (a pivot, a landing, a deferred jump). In the full setup LocomotionToggleScript
    // disables it when a destination starts, and the Animator's completion call is then routed to the
    // destination movement source instead, so it never arrives. These tests reproduce both halves:
    // they toggle 'enabled' directly and never call CompletePivot / CompleteLanding / TriggerJump.
    public class LocomotionScriptDisableTests
    {
        const float SettleTime = 0.2f;
        const float AccelerateTime = 0.5f;
        const float MoveTime = 0.5f;
        const float MinExpectedDistance = 0.5f;
        const float Timeout = 3f;

        static readonly FloatRange Any = new(float.NegativeInfinity, float.PositiveInfinity);

        readonly List<Object> _created = new();
        LocomotionScript _locomotion;

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

        // Baseline: proves the harness itself works (ground, controller, settings), so a failure in
        // the tests below means "stuck", not "set up wrong".
        [UnityTest]
        public IEnumerator Moves_forward_when_given_input()
        {
            CreateCharacter(new FakeLocomotionSettings());
            yield return Settle();

            yield return AssertMovesForward();
        }

        // Baseline for the pending-jump test below: proves Jump() starts a jump in this harness,
        // so that test cannot pass just because the jump was never accepted.
        [UnityTest]
        public IEnumerator Jumps_when_jump_is_pressed()
        {
            CreateCharacter(new FakeLocomotionSettings());
            yield return Settle();

            var jumpBegan = false;
            _locomotion.JumpBegan += () => jumpBegan = true;

            _locomotion.Jump();
            yield return WaitUntilOrTimeout(() => jumpBegan);

            Assert.IsTrue(jumpBegan, "Jump() did not start a jump.");
        }

        [UnityTest]
        public IEnumerator Moves_after_being_disabled_mid_pivot()
        {
            CreateCharacter(new FakeLocomotionSettings
            {
                MovingPivots = new IPivotProfile[]
                {
                    new FakePivotProfile(
                        id: 1,
                        speedRange: Any,
                        new FloatRange(90, 180),
                        new FloatRange(-180, -90))
                }
            });
            yield return Settle();

            var pivotRequested = false;
            _locomotion.PivotRequested += _ => pivotRequested = true;

            // Run forward, then reverse: a 180° turn at speed matches the pivot profile.
            _locomotion.Move(Vector3.forward);
            yield return new WaitForSeconds(AccelerateTime);
            _locomotion.Move(Vector3.back);
            yield return null;

            Assert.IsTrue(pivotRequested, "Setup failed: the pivot was never requested.");

            // CompletePivot is deliberately never called.
            yield return DisableAndReEnable();

            yield return AssertMovesForward();
        }

        [UnityTest]
        public IEnumerator Moves_after_being_disabled_mid_landing()
        {
            CreateCharacter(new FakeLocomotionSettings
            {
                JumpLandings = new ILandingProfile[]
                {
                    new FakeLandingProfile(id: 1, fallDistanceRange: Any, speedRange: Any)
                }
            });
            yield return Settle();

            var landingRequested = false;
            _locomotion.LandingRequested += _ => landingRequested = true;

            _locomotion.Jump();
            yield return WaitUntilOrTimeout(() => landingRequested);

            Assert.IsTrue(landingRequested, "Setup failed: the landing was never requested.");

            // CompleteLanding is deliberately never called.
            yield return DisableAndReEnable();

            yield return AssertMovesForward();
        }

        [UnityTest]
        public IEnumerator Moves_after_being_disabled_while_awaiting_jump_trigger()
        {
            CreateCharacter(new FakeLocomotionSettings { DeferJumpTrigger = true });
            yield return Settle();

            var jumpTriggerRequested = false;
            _locomotion.JumpTriggerRequested += () => jumpTriggerRequested = true;

            _locomotion.Jump();

            Assert.IsTrue(jumpTriggerRequested, "Setup failed: the jump trigger was never requested.");

            // TriggerJump (normally an animation event) is deliberately never called.
            yield return DisableAndReEnable();

            yield return AssertMovesForward();
        }

        [UnityTest]
        public IEnumerator Does_not_jump_after_being_disabled_with_a_jump_pending()
        {
            CreateCharacter(new FakeLocomotionSettings());
            yield return Settle();

            var jumpBegan = false;
            _locomotion.JumpBegan += () => jumpBegan = true;

            // Same frame: the jump is accepted but not applied until the next Update.
            _locomotion.Jump();
            _locomotion.enabled = false;
            yield return null;
            _locomotion.enabled = true;

            yield return new WaitForSeconds(MoveTime);

            Assert.IsFalse(
                jumpBegan,
                "A jump pressed before locomotion was disabled was applied after re-enabling.");
        }

        void CreateCharacter(ILocomotionSettings settings)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(10, 1, 10); // 100 m x 100 m
            _created.Add(ground);

            // Feet 0.1 m above the ground.
            _locomotion = LocomotionTestCharacter.Create(settings, new Vector3(0, 1.1f, 0), _created);
        }

        static IEnumerator Settle()
        {
            // Let the controller drop the 0.1 m and register as grounded.
            yield return new WaitForSeconds(SettleTime);
        }

        IEnumerator DisableAndReEnable()
        {
            _locomotion.enabled = false; // What LocomotionToggleScript does when a destination starts...
            yield return null;
            _locomotion.enabled = true; // ...and when it finishes.
        }

        IEnumerator AssertMovesForward()
        {
            var start = _locomotion.transform.position;

            _locomotion.Move(Vector3.forward);
            yield return new WaitForSeconds(MoveTime);

            var horizontalDistance = Vector3
                .ProjectOnPlane(_locomotion.transform.position - start, Vector3.up)
                .magnitude;

            Assert.Greater(
                horizontalDistance,
                MinExpectedDistance,
                "Locomotion did not move: it is still blocked after being re-enabled.");
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
