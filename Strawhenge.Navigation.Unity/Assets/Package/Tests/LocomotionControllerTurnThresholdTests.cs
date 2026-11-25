using NUnit.Framework;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Tests
{
    // SharpTurnAngle, ReversalAngle and StationaryPivotMaxSpeed come from the settings.
    public class LocomotionControllerTurnThresholdTests
    {
        const float SpeedTolerance = 0.01f;
        const int StationaryPivotId = 4;

        // A 90 degree turn to the right, up to walking speed.
        static readonly IPivotProfile StationaryPivot = new FakePivotProfile(
            StationaryPivotId,
            speedRange: new ValueRange(0f, 2f),
            new ValueRange(60f, 120f));

        [Test]
        public void Turn_past_the_default_sharp_turn_angle_limits_the_speed_to_run_speed()
        {
            var harness = SprintForward(new FakeLocomotionSettings());

            Turn(harness, 50f);

            Assert.AreEqual(DefaultLocomotionSettings.RunSpeed, harness.LastRequestedSpeed, SpeedTolerance);
        }

        [Test]
        public void Turn_short_of_a_larger_sharp_turn_angle_keeps_the_speed()
        {
            var harness = SprintForward(new FakeLocomotionSettings { SharpTurnAngle = 60f });

            Turn(harness, 50f);

            Assert.AreEqual(DefaultLocomotionSettings.SprintSpeed, harness.LastRequestedSpeed, SpeedTolerance);
        }

        [Test]
        public void Turn_past_the_default_reversal_angle_limits_the_speed_to_walk_speed()
        {
            var harness = SprintForward(new FakeLocomotionSettings());

            Turn(harness, 130f);

            Assert.AreEqual(DefaultLocomotionSettings.WalkSpeed, harness.LastRequestedSpeed, SpeedTolerance);
        }

        [Test]
        public void Turn_short_of_a_larger_reversal_angle_limits_the_speed_to_run_speed_only()
        {
            var harness = SprintForward(new FakeLocomotionSettings { ReversalAngle = 150f });

            Turn(harness, 130f);

            Assert.AreEqual(DefaultLocomotionSettings.RunSpeed, harness.LastRequestedSpeed, SpeedTolerance);
        }

        // Walking (1.5 m/s) is past the default (1 m/s), so a tap to the side just turns.
        [Test]
        public void Stationary_pivot_is_not_requested_while_walking_past_the_default_stationary_pivot_max_speed()
        {
            var harness = WalkThenTapRight(new FakeLocomotionSettings());
            var pivotRequested = false;
            harness.Controller.PivotRequested += _ => pivotRequested = true;

            harness.Step();

            Assert.IsFalse(pivotRequested);
        }

        [Test]
        public void Stationary_pivot_is_requested_while_walking_below_a_larger_stationary_pivot_max_speed()
        {
            var harness = WalkThenTapRight(new FakeLocomotionSettings { StationaryPivotMaxSpeed = 2f });
            int? pivotId = null;
            harness.Controller.PivotRequested += id => pivotId = id;

            harness.Step();

            Assert.AreEqual(StationaryPivotId, pivotId);
        }

        // Walks forward, then taps to the right (given and released before the next step).
        static LocomotionControllerHarness WalkThenTapRight(FakeLocomotionSettings settings)
        {
            settings.StationaryPivots = new[] { StationaryPivot };
            var harness = new LocomotionControllerHarness(settings);

            harness.Controller.Walk = true;
            harness.Controller.Move(Vector3.forward);
            harness.StepFor(1f);
            Assert.AreEqual(DefaultLocomotionSettings.WalkSpeed, harness.LastRequestedSpeed, SpeedTolerance,
                "Setup failed: the character isn't walking.");

            harness.Controller.Move(Vector3.right);
            harness.Controller.Move(Vector3.zero);
            return harness;
        }

        static LocomotionControllerHarness SprintForward(FakeLocomotionSettings settings)
        {
            var harness = new LocomotionControllerHarness(settings);

            harness.Controller.Sprint = true;
            harness.Controller.Move(Vector3.forward);
            harness.StepFor(1.5f);
            Assert.AreEqual(DefaultLocomotionSettings.SprintSpeed, harness.LastRequestedSpeed, SpeedTolerance,
                "Setup failed: the character never got up to sprint speed.");

            return harness;
        }

        // One step turning the input by angle (degrees, to the right) from straight ahead.
        static void Turn(LocomotionControllerHarness harness, float angle)
        {
            harness.Controller.Move(Quaternion.Euler(0f, angle, 0f) * Vector3.forward);
            harness.Step();
        }
    }
}
