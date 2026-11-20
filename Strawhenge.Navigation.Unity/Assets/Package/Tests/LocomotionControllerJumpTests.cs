using NUnit.Framework;
using System.Linq;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Tests
{
    // Jumps, coyote time, deferred jump triggers and jump landings, on a FakeMotor.
    public class LocomotionControllerJumpTests
    {
        const float CoyoteTime = 0.2f;
        const float WithinCoyoteTime = 0.1f;
        const float AfterCoyoteTime = 0.35f;
        const int LandingId = 7;

        static readonly ValueRange Any = new(float.NegativeInfinity, float.PositiveInfinity);

        [Test]
        public void Jumps_and_lands()
        {
            var harness = new LocomotionControllerHarness();
            var jumpBegan = 0;
            var jumpEnded = 0;
            harness.Controller.JumpBegan += () => jumpBegan++;
            harness.Controller.JumpEnded += () => jumpEnded++;

            harness.Controller.Jump();
            harness.Step();

            Assert.AreEqual(1, jumpBegan, "The jump should have begun.");
            Assert.IsTrue(harness.StepUntil(() => jumpEnded > 0), "The jump should have ended on landing.");
            Assert.IsTrue(harness.Motor.IsGrounded, "The character should be back on the ground.");
        }

        // Motors that snap to the ground rely on it to let go, so it must be on the step the jump begins, and only
        // that one.
        [Test]
        public void Flags_the_step_a_jump_begins_and_no_other()
        {
            var harness = new LocomotionControllerHarness();

            harness.Step();
            Assert.IsFalse(harness.Motor.LastMove.JumpStarted, "A step without a jump shouldn't be flagged.");

            harness.Controller.Jump();
            harness.Step();
            Assert.IsTrue(harness.Motor.LastMove.JumpStarted, "The step the jump begins should be flagged.");

            harness.Step();
            Assert.IsFalse(harness.Motor.LastMove.JumpStarted, "The steps after the jump began shouldn't be flagged.");
        }

        [Test]
        public void Flags_a_deferred_jump_on_the_step_after_its_trigger()
        {
            var harness = new LocomotionControllerHarness(new FakeLocomotionSettings { DeferJumpTrigger = true });

            harness.Controller.Jump();
            harness.Step(5);
            Assert.IsFalse(harness.Motor.Moves.Any(m => m.JumpStarted),
                "No step should be flagged while waiting for the trigger.");

            harness.Controller.TriggerJump();
            harness.Step();

            Assert.IsTrue(harness.Motor.LastMove.JumpStarted, "The step after the trigger should be flagged.");
        }

        [Test]
        public void Jumps_to_the_jump_height()
        {
            var harness = new LocomotionControllerHarness(new FakeLocomotionSettings { JumpHeight = 1.5f });
            var jumpEnded = false;
            harness.Controller.JumpEnded += () => jumpEnded = true;
            var highest = 0f;

            harness.Controller.Jump();
            harness.StepUntil(() =>
            {
                highest = Mathf.Max(highest, harness.Motor.Position.y);
                return jumpEnded;
            });

            Assert.AreEqual(1.5f, highest, 0.1f);
        }

        [Test]
        public void Cannot_jump_again_in_the_air()
        {
            var harness = new LocomotionControllerHarness();
            var jumpBegan = 0;
            harness.Controller.JumpBegan += () => jumpBegan++;

            harness.Controller.Jump();
            harness.StepFor(0.2f);
            Assert.AreEqual(1, jumpBegan, "Setup failed: the first jump never began.");

            harness.Controller.Jump();
            harness.StepFor(0.2f);

            Assert.AreEqual(1, jumpBegan, "A second jump in the air shouldn't begin.");
        }

        // Baseline: proves a jump off a ledge works in this harness, so the test below can't pass just because
        // jumping never works.
        [Test]
        public void Can_jump_within_coyote_time_of_leaving_the_ground()
        {
            Assert.IsTrue(JumpAfterLeavingTheGround(WithinCoyoteTime));
        }

        [Test]
        public void Cannot_jump_after_coyote_time_has_passed()
        {
            Assert.IsFalse(JumpAfterLeavingTheGround(AfterCoyoteTime));
        }

        [Test]
        public void Deferred_jump_waits_for_the_trigger()
        {
            var harness = new LocomotionControllerHarness(new FakeLocomotionSettings { DeferJumpTrigger = true });
            var triggerRequested = false;
            var jumpBegan = false;
            harness.Controller.JumpTriggerRequested += () => triggerRequested = true;
            harness.Controller.JumpBegan += () => jumpBegan = true;

            harness.Controller.Jump();
            harness.StepFor(0.5f);

            Assert.IsTrue(triggerRequested, "A deferred jump should ask for its trigger.");
            Assert.IsFalse(jumpBegan, "A deferred jump shouldn't begin before its trigger.");

            harness.Controller.TriggerJump();
            harness.Step();

            Assert.IsTrue(jumpBegan, "The jump should begin once triggered.");
        }

        [Test]
        public void Deferred_jump_is_cancelled_when_its_trigger_never_arrives()
        {
            var harness = new LocomotionControllerHarness(new FakeLocomotionSettings { DeferJumpTrigger = true });
            var jumpBegan = false;
            harness.Controller.JumpBegan += () => jumpBegan = true;

            harness.Controller.Jump();
            harness.StepFor(LocomotionControllerHarness.AnimatorWaitTimeout + 0.1f);

            Assert.IsTrue(harness.Logger.Warnings.Any(w => w.Contains("Deferred jump")),
                "A warning should say the deferred jump timed out.");

            harness.Controller.TriggerJump();
            harness.Step();

            Assert.IsFalse(jumpBegan, "A trigger after the timeout shouldn't launch the cancelled jump.");
        }

        [Test]
        public void Jump_landing_is_requested_when_a_profile_matches()
        {
            var harness = new LocomotionControllerHarness(new FakeLocomotionSettings
            {
                JumpLandings = new ILandingProfile[] { new FakeLandingProfile(LandingId, Any, Any) }
            });
            int? landingId = null;
            harness.Controller.LandingRequested += id => landingId = id;

            harness.Controller.Jump();
            Assert.IsTrue(harness.StepUntil(() => landingId != null), "No landing was requested.");

            Assert.AreEqual(LandingId, landingId);
        }

        // Landing 0.6 m below where the jump started (less than FallDistance, so not a fall): the profile asks for
        // a drop of 0.4 to 0.9 m.
        [Test]
        public void Jump_landing_matches_on_the_drop_from_where_the_jump_started()
        {
            var harness = new LocomotionControllerHarness(new FakeLocomotionSettings
            {
                FallDistance = 1f,
                JumpLandings = new ILandingProfile[]
                {
                    new FakeLandingProfile(LandingId, fallDistanceRange: new ValueRange(0.4f, 0.9f), speedRange: Any)
                }
            });
            int? landingId = null;
            var jumpEnded = false;
            harness.Controller.LandingRequested += id => landingId = id;
            harness.Controller.JumpEnded += () => jumpEnded = true;

            harness.Controller.Jump();
            harness.Step();
            harness.Motor.GroundHeight = -0.6f; // Lands on lower ground.
            Assert.IsTrue(harness.StepUntil(() => jumpEnded), "Setup failed: the jump never ended.");

            Assert.AreEqual(LandingId, landingId, "Landing 0.6 m below the take-off should match the profile.");
        }

        // Steps off a ledge (the floor disappears), waits, then presses jump. Returns whether the jump began.
        static bool JumpAfterLeavingTheGround(float wait)
        {
            var harness = new LocomotionControllerHarness(new FakeLocomotionSettings { CoyoteTime = CoyoteTime });
            var jumpBegan = false;
            harness.Controller.JumpBegan += () => jumpBegan = true;

            harness.Step();
            harness.Motor.GroundHeight = null;
            harness.StepFor(wait);
            Assert.IsFalse(harness.Motor.IsGrounded, "Setup failed: the character is still on the ground.");

            harness.Controller.Jump();
            harness.Step();

            return jumpBegan;
        }
    }
}
