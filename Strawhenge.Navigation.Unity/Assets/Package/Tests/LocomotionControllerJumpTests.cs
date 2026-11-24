using NUnit.Framework;
using System.Linq;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Tests
{
    // Jumps, coyote time, jump profiles and their wind-ups, and jump landings, on a FakeMotor.
    public class LocomotionControllerJumpTests
    {
        const float CoyoteTime = 0.2f;
        const float WithinCoyoteTime = 0.1f;
        const float AfterCoyoteTime = 0.35f;
        const int LandingId = 7;
        const int JumpId = 3;
        const int OtherJumpId = 4;

        static readonly ValueRange Any = new(float.NegativeInfinity, float.PositiveInfinity);
        static readonly ValueRange Standing = new(0f, 0.5f);
        static readonly ValueRange Running = new(3f, 6f);
        static readonly ValueRange AnyDirection = new(-180f, 180f);
        static readonly ValueRange Right = new(45f, 135f);
        static readonly ValueRange Left = new(-135f, -45f);

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
        public void Flags_a_jump_with_a_wind_up_on_the_step_after_its_trigger()
        {
            var harness = WithJumps(new FakeJumpProfile(JumpId, Any));

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
        public void Jumps_straight_away_without_jump_profiles()
        {
            var harness = new LocomotionControllerHarness();
            var triggerRequested = false;
            var jumpBegan = false;
            harness.Controller.JumpTriggerRequested += _ => triggerRequested = true;
            harness.Controller.JumpBegan += () => jumpBegan = true;

            harness.Controller.Jump();
            harness.Step();

            Assert.IsTrue(jumpBegan, "The jump should begin straight away.");
            Assert.IsFalse(triggerRequested, "No wind-up should be requested without a matching profile.");
        }

        [Test]
        public void Matching_jump_waits_for_its_wind_up()
        {
            var harness = WithJumps(new FakeJumpProfile(JumpId, Standing));
            int? requestedId = null;
            var jumpBegan = false;
            harness.Controller.JumpTriggerRequested += id => requestedId = id;
            harness.Controller.JumpBegan += () => jumpBegan = true;

            harness.Controller.Jump();
            harness.StepFor(0.5f);

            Assert.AreEqual(JumpId, requestedId, "The matching profile's wind-up should be requested.");
            Assert.IsFalse(jumpBegan, "The jump shouldn't begin before its wind-up launches it.");

            harness.Controller.TriggerJump();
            harness.Step();

            Assert.IsTrue(jumpBegan, "The jump should begin once triggered.");
        }

        [Test]
        public void Jumps_straight_away_when_the_speed_matches_no_profile()
        {
            var harness = WithJumps(new FakeJumpProfile(JumpId, Running));
            var triggerRequested = false;
            var jumpBegan = false;
            harness.Controller.JumpTriggerRequested += _ => triggerRequested = true;
            harness.Controller.JumpBegan += () => jumpBegan = true;

            harness.Controller.Jump(); // Standing still: below the running profile.
            harness.Step();

            Assert.IsTrue(jumpBegan, "The jump should begin straight away.");
            Assert.IsFalse(triggerRequested, "A profile for running shouldn't match standing still.");
        }

        [Test]
        public void Running_jump_matches_on_speed()
        {
            var harness = WithJumps(new FakeJumpProfile(OtherJumpId, Standing), new FakeJumpProfile(JumpId, Running));
            int? requestedId = null;
            harness.Controller.JumpTriggerRequested += id => requestedId = id;

            harness.Controller.Move(Vector3.forward);
            harness.StepFor(1f);
            Assert.That(harness.LastRequestedSpeed, Is.InRange(3f, 6f), "Setup failed: the character isn't running.");

            harness.Controller.Jump();

            Assert.AreEqual(JumpId, requestedId);
        }

        [Test]
        public void First_matching_jump_wins()
        {
            var harness = WithJumps(new FakeJumpProfile(JumpId, Any), new FakeJumpProfile(OtherJumpId, Any));
            int? requestedId = null;
            harness.Controller.JumpTriggerRequested += id => requestedId = id;

            harness.Controller.Jump();

            Assert.AreEqual(JumpId, requestedId);
        }

        // Baseline for the test below: the same profile in the strafe list does match.
        [Test]
        public void Strafing_uses_strafe_jumps()
        {
            var harness = WithJumps(
                strafeJumps: new IStrafeJumpProfile[] { new FakeStrafeJumpProfile(JumpId, Any, AnyDirection) });
            int? requestedId = null;
            harness.Controller.JumpTriggerRequested += id => requestedId = id;

            harness.Controller.Strafe = true;
            harness.Controller.Jump();

            Assert.AreEqual(JumpId, requestedId);
        }

        [Test]
        public void Strafing_ignores_jumps_for_not_strafing()
        {
            var harness = WithJumps(new FakeJumpProfile(JumpId, Any));
            var triggerRequested = false;
            var jumpBegan = false;
            harness.Controller.JumpTriggerRequested += _ => triggerRequested = true;
            harness.Controller.JumpBegan += () => jumpBegan = true;

            harness.Controller.Strafe = true;
            harness.Controller.Jump();
            harness.Step();

            Assert.IsFalse(triggerRequested, "A jump for not strafing shouldn't match while strafing.");
            Assert.IsTrue(jumpBegan, "With no strafe jump, the character should jump straight away.");
        }

        [Test]
        public void Not_strafing_ignores_strafe_jumps()
        {
            var harness = WithJumps(
                strafeJumps: new IStrafeJumpProfile[] { new FakeStrafeJumpProfile(JumpId, Any, AnyDirection) });
            var triggerRequested = false;
            harness.Controller.JumpTriggerRequested += _ => triggerRequested = true;

            harness.Controller.Jump();

            Assert.IsFalse(triggerRequested, "A strafe jump shouldn't match while not strafing.");
        }

        [TestCase(1f, JumpId)]
        [TestCase(-1f, OtherJumpId)]
        public void Strafe_jump_matches_on_direction(float sideways, int expectedId)
        {
            var harness = WithJumps(strafeJumps: new IStrafeJumpProfile[]
            {
                new FakeStrafeJumpProfile(JumpId, Running, Right),
                new FakeStrafeJumpProfile(OtherJumpId, Running, Left)
            });
            int? requestedId = null;
            harness.Controller.JumpTriggerRequested += id => requestedId = id;

            harness.Controller.Strafe = true;
            harness.Controller.Move(new Vector3(sideways, 0f, 0f)); // Facing forward the whole time.
            harness.StepFor(1f);
            Assert.That(harness.LastRequestedSpeed, Is.InRange(3f, 6f), "Setup failed: the character isn't running.");

            harness.Controller.Jump();

            Assert.AreEqual(expectedId, requestedId);
        }

        [Test]
        public void Standing_strafe_jump_matches_with_a_full_direction_range()
        {
            var harness = WithJumps(strafeJumps: new IStrafeJumpProfile[]
            {
                new FakeStrafeJumpProfile(OtherJumpId, new ValueRange(0f, 6f), Left),
                new FakeStrafeJumpProfile(JumpId, Standing, AnyDirection)
            });
            int? requestedId = null;
            harness.Controller.JumpTriggerRequested += id => requestedId = id;

            harness.Controller.Strafe = true;
            harness.Controller.Jump();

            Assert.AreEqual(JumpId, requestedId,
                "Standing still counts as forward, so a left-only jump shouldn't match.");
        }

        [Test]
        public void Pressing_jump_again_during_the_wind_up_does_nothing()
        {
            var harness = WithJumps(new FakeJumpProfile(JumpId, Any));
            var requests = 0;
            harness.Controller.JumpTriggerRequested += _ => requests++;

            harness.Controller.Jump();
            harness.Step();
            harness.Controller.Jump();

            Assert.AreEqual(1, requests);
        }

        [Test]
        public void Strafing_changed_during_the_wind_up_keeps_the_jump()
        {
            var harness = WithJumps(new FakeJumpProfile(JumpId, Any));
            var jumpBegan = false;
            harness.Controller.JumpBegan += () => jumpBegan = true;

            harness.Controller.Jump();
            harness.Step();
            harness.Controller.Strafe = true;
            harness.Step();
            harness.Controller.TriggerJump();
            harness.Step();

            Assert.IsTrue(jumpBegan, "The jump matched at the press should still launch.");
        }

        // Baseline: Can_jump_within_coyote_time_of_leaving_the_ground and Matching_jump_waits_for_its_wind_up.
        [Test]
        public void Coyote_jump_skips_the_wind_up()
        {
            var harness = new LocomotionControllerHarness(new FakeLocomotionSettings
            {
                CoyoteTime = CoyoteTime,
                Jumps = new IJumpProfile[] { new FakeJumpProfile(JumpId, Any) }
            });
            var triggerRequested = false;
            var jumpBegan = false;
            harness.Controller.JumpTriggerRequested += _ => triggerRequested = true;
            harness.Controller.JumpBegan += () => jumpBegan = true;

            harness.Step();
            harness.Motor.GroundHeight = null;
            harness.StepFor(WithinCoyoteTime);
            Assert.IsFalse(harness.Motor.IsGrounded, "Setup failed: the character is still on the ground.");

            harness.Controller.Jump();
            harness.Step();

            Assert.IsFalse(triggerRequested, "Already off the ground, no wind-up should be requested.");
            Assert.IsTrue(jumpBegan, "The coyote jump should begin straight away.");
        }

        [Test]
        public void Jump_is_cancelled_when_its_wind_up_never_launches_it()
        {
            var harness = WithJumps(new FakeJumpProfile(JumpId, Any));
            var jumpBegan = false;
            var cancelled = 0;
            harness.Controller.JumpBegan += () => jumpBegan = true;
            harness.Controller.JumpTriggerCancelled += () => cancelled++;

            harness.Controller.Jump();
            harness.StepFor(LocomotionControllerHarness.AnimatorWaitTimeout + 0.1f);

            Assert.AreEqual(1, cancelled, "The cancelled wind-up should be reported.");
            Assert.IsTrue(harness.Logger.Warnings.Any(w => w.Contains($"Jump ID {JumpId}")),
                "A warning should say the jump timed out.");

            harness.Controller.TriggerJump();
            harness.Step();

            Assert.IsFalse(jumpBegan, "A trigger after the timeout shouldn't launch the cancelled jump.");
        }

        [Test]
        public void Timed_out_jump_warns_once_per_jump_id()
        {
            var harness = WithJumps(new FakeJumpProfile(JumpId, Any));

            harness.Controller.Jump();
            harness.StepFor(LocomotionControllerHarness.AnimatorWaitTimeout + 0.1f);
            harness.Controller.Jump();
            harness.StepFor(LocomotionControllerHarness.AnimatorWaitTimeout + 0.1f);

            Assert.AreEqual(1, harness.Logger.Warnings.Count(w => w.Contains($"Jump ID {JumpId}")));
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

        static LocomotionControllerHarness WithJumps(params IJumpProfile[] jumps) =>
            new(new FakeLocomotionSettings { Jumps = jumps });

        static LocomotionControllerHarness WithJumps(IStrafeJumpProfile[] strafeJumps) =>
            new(new FakeLocomotionSettings { StrafeJumps = strafeJumps });

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
