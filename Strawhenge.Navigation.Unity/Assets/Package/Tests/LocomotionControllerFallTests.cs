using NUnit.Framework;
using System.Linq;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Tests
{
    // Falls and fall landings, on a FakeMotor. Each test steps off a ledge by lowering the floor.
    public class LocomotionControllerFallTests
    {
        const float FallDistance = 1f;
        const int LandingId = 7;

        static readonly ValueRange Any = new(float.NegativeInfinity, float.PositiveInfinity);

        [Test]
        public void Fall_begins_once_the_character_has_dropped_fall_distance()
        {
            var harness = new LocomotionControllerHarness(new FakeLocomotionSettings { FallDistance = FallDistance });
            var fallBegan = false;
            harness.Controller.FallBegan += () => fallBegan = true;

            harness.Motor.GroundHeight = -10f;

            Assert.IsTrue(harness.StepUntil(() => fallBegan), "The fall should have begun.");
            Assert.LessOrEqual(harness.Motor.Position.y, -FallDistance, "The fall began before dropping FallDistance.");
        }

        [Test]
        public void Walking_off_a_ledge_starts_the_fall_at_fall_start_speed()
        {
            var harness = new LocomotionControllerHarness(new FakeLocomotionSettings { FallStartSpeed = 2f });
            harness.Step();
            Assert.IsTrue(harness.Motor.IsGrounded, "Setup failed: the character isn't on the ground.");

            harness.Motor.GroundHeight = null;
            harness.Step();

            var expected = -2f + DefaultLocomotionSettings.Gravity * LocomotionControllerHarness.FrameTime;
            Assert.AreEqual(expected, harness.Motor.LastMove.Velocity.y, 0.001f);
        }

        [Test]
        public void A_drop_shorter_than_fall_distance_is_not_a_fall()
        {
            var harness = new LocomotionControllerHarness(new FakeLocomotionSettings { FallDistance = FallDistance });
            var fallBegan = false;
            harness.Controller.FallBegan += () => fallBegan = true;

            harness.Motor.GroundHeight = -0.5f;
            Assert.IsTrue(harness.StepUntil(() => harness.Motor.IsGrounded && harness.Motor.Position.y < -0.4f),
                "Setup failed: the character never reached the lower ground.");
            harness.StepFor(0.5f);

            Assert.IsFalse(fallBegan, "A 0.5 m drop with a FallDistance of 1 m shouldn't be a fall.");
        }

        [Test]
        public void Fall_ends_and_a_landing_is_requested_when_a_profile_matches()
        {
            var harness = Create(landingFallDistance: new ValueRange(2f, 5f));
            var fallEnded = false;
            int? landingId = null;
            harness.Controller.FallEnded += () => fallEnded = true;
            harness.Controller.LandingRequested += id => landingId = id;

            harness.Motor.GroundHeight = -3f;

            Assert.IsTrue(harness.StepUntil(() => fallEnded), "The fall should have ended.");
            Assert.AreEqual(LandingId, landingId, "A 3 m fall should match the 2 to 5 m landing.");
        }

        [Test]
        public void No_landing_is_requested_when_no_profile_matches()
        {
            var harness = Create(landingFallDistance: new ValueRange(5f, 10f));
            var fallEnded = false;
            var landingRequested = false;
            harness.Controller.FallEnded += () => fallEnded = true;
            harness.Controller.LandingRequested += _ => landingRequested = true;

            harness.Motor.GroundHeight = -3f;

            Assert.IsTrue(harness.StepUntil(() => fallEnded), "The fall should have ended.");
            Assert.IsFalse(landingRequested, "A 3 m fall shouldn't match the 5 to 10 m landing.");
        }

        [Test]
        public void Landing_holds_the_character_still_until_it_is_completed()
        {
            var harness = FallAndLand();

            harness.Controller.Move(Vector3.forward);
            harness.StepFor(0.5f);

            Assert.AreEqual(0f, harness.Motor.Position.z, 0.001f, "The character moved during the landing.");

            harness.Controller.CompleteLanding();
            harness.StepFor(0.5f);

            Assert.Greater(harness.Motor.Position.z, 0.1f, "The character should move once the landing is complete.");
        }

        [Test]
        public void Landing_is_given_up_when_it_is_never_completed()
        {
            var harness = FallAndLand();

            harness.Controller.Move(Vector3.forward);
            harness.StepFor(LocomotionControllerHarness.AnimatorWaitTimeout + 0.5f);

            Assert.IsTrue(harness.Logger.Warnings.Any(w => w.Contains($"Landing ID {LandingId}")),
                "A warning should say the landing timed out.");
            Assert.Greater(harness.Motor.Position.z, 0.1f, "The character should move again after the timeout.");
        }

        // Lifted without jumping (teleported, knocked up, carried by a lift), the character's fall counts from the
        // highest it has been since it left the ground. It used to count from the ground it last stood on, so a
        // fall that ended back at that height was never a fall at all.
        [Test]
        public void Fall_begins_after_being_lifted_off_the_ground_without_jumping()
        {
            var harness = new LocomotionControllerHarness(new FakeLocomotionSettings { FallDistance = FallDistance });
            var fallBegan = false;
            harness.Controller.FallBegan += () => fallBegan = true;
            harness.Step();
            Assert.IsTrue(harness.Motor.IsGrounded, "Setup failed: the character isn't on the ground.");

            harness.Motor.Position = new Vector3(0f, 10f, 0f);

            Assert.IsTrue(harness.StepUntil(() => fallBegan), "The fall should have begun.");
            Assert.LessOrEqual(harness.Motor.Position.y, 10f - FallDistance, "The fall began before dropping FallDistance.");
        }

        [Test]
        public void Landing_after_being_lifted_matches_on_the_drop_from_where_it_was_lifted_to()
        {
            var harness = Create(landingFallDistance: new ValueRange(4f, 6f));
            int? landingId = null;
            harness.Controller.LandingRequested += id => landingId = id;
            harness.Step();
            Assert.IsTrue(harness.Motor.IsGrounded, "Setup failed: the character isn't on the ground.");

            harness.Motor.Position = new Vector3(0f, 5f, 0f);
            harness.StepFor(2f);

            Assert.IsTrue(harness.Motor.IsGrounded, "Setup failed: the character never landed.");
            Assert.AreEqual(LandingId, landingId, "A 5 m fall should match the 4 to 6 m landing.");
        }

        // The known gap this closes: the reference height started at 0, whatever the character's height.
        [Test]
        public void Fall_begins_for_a_character_that_starts_in_the_air()
        {
            var harness = new LocomotionControllerHarness(new FakeLocomotionSettings { FallDistance = FallDistance });
            var fallBegan = false;
            harness.Controller.FallBegan += () => fallBegan = true;

            harness.Motor.Position = new Vector3(0f, 5f, 0f);

            Assert.IsTrue(harness.StepUntil(() => fallBegan), "The fall should have begun.");
        }

        // Moved while locomotion was off (by the destination controller, say), the character's last grounded height
        // is out of date: its fall counts from where locomotion took over again.
        [Test]
        public void Landing_after_being_moved_while_inactive_matches_on_the_drop_from_where_locomotion_took_over()
        {
            var harness = Create(landingFallDistance: new ValueRange(4f, 6f));
            int? landingId = null;
            harness.Controller.LandingRequested += id => landingId = id;
            harness.Step();
            Assert.IsTrue(harness.Motor.IsGrounded, "Setup failed: the character isn't on the ground.");

            harness.Controller.Deactivate();
            harness.Motor.GroundHeight = -10f;
            harness.Motor.Position = new Vector3(0f, -5f, 0f);
            harness.Controller.Activate();
            harness.StepFor(2f);

            Assert.IsTrue(harness.Motor.IsGrounded, "Setup failed: the character never landed.");
            Assert.AreEqual(LandingId, landingId, "A 5 m fall should match the 4 to 6 m landing, not count 10 m.");
        }

        // A jump's fall still counts from where it took off, not from the top of the jump: otherwise an ordinary jump
        // on flat ground would count as a fall.
        [Test]
        public void Jump_on_flat_ground_is_not_a_fall()
        {
            var harness = new LocomotionControllerHarness(new FakeLocomotionSettings
            {
                FallDistance = 0.5f,
                JumpHeight = 1f,
                FallLandings = new ILandingProfile[] { new FakeLandingProfile(LandingId, Any, Any) }
            });

            var jumpBegan = false;
            var jumpEnded = false;
            var fallBegan = false;
            var landingRequested = false;
            harness.Controller.JumpBegan += () => jumpBegan = true;
            harness.Controller.JumpEnded += () => jumpEnded = true;
            harness.Controller.FallBegan += () => fallBegan = true;
            harness.Controller.LandingRequested += _ => landingRequested = true;
            harness.Step();

            harness.Controller.Jump();
            Assert.IsTrue(harness.StepUntil(() => jumpBegan), "Setup failed: the jump never began.");
            Assert.IsTrue(harness.StepUntil(() => jumpEnded), "Setup failed: the jump never ended.");
            harness.Step();

            Assert.IsFalse(fallBegan, "A jump on flat ground shouldn't count as a fall.");
            Assert.IsFalse(landingRequested, "A jump on flat ground shouldn't request a fall landing.");
        }

        static LocomotionControllerHarness Create(ValueRange landingFallDistance) =>
            new(new FakeLocomotionSettings
            {
                FallDistance = FallDistance,
                FallLandings = new ILandingProfile[] { new FakeLandingProfile(LandingId, landingFallDistance, Any) }
            });

        static LocomotionControllerHarness FallAndLand()
        {
            var harness = Create(landingFallDistance: Any);
            var landingRequested = false;
            harness.Controller.LandingRequested += _ => landingRequested = true;

            harness.Motor.GroundHeight = -3f;
            Assert.IsTrue(harness.StepUntil(() => landingRequested), "Setup failed: no landing was requested.");

            return harness;
        }
    }
}
