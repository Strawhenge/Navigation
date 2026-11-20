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
