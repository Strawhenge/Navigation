using NUnit.Framework;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Tests
{
    // SharpTurnAngle, ReversalAngle and StationaryPivotMaxSpeed come from the settings.
    public class LocomotionControllerTurnThresholdTests
    {
        const float SpeedTolerance = 0.01f;
        const int StationaryPivotId = 4;

        // A 90 degree turn to the right from standing, as in LocomotionControllerPivotTests.
        static readonly IPivotProfile StationaryPivot = new FakePivotProfile(
            StationaryPivotId,
            speedRange: new ValueRange(0f, 0.5f),
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

        // With the default (1 m/s), holding the input drops the queued stationary pivot
        // (LocomotionControllerPivotTests.Stationary_pivot_is_not_requested_when_the_input_is_held).
        [Test]
        public void Stationary_pivot_is_kept_while_slower_than_a_larger_stationary_pivot_max_speed()
        {
            var harness = new LocomotionControllerHarness(new FakeLocomotionSettings
            {
                StationaryPivots = new[] { StationaryPivot },
                StationaryPivotMaxSpeed = 10f
            });
            int? pivotId = null;
            harness.Controller.PivotRequested += id => pivotId = id;

            harness.Controller.Move(Vector3.right);
            harness.StepFor(0.5f);
            Assert.Greater(harness.LastRequestedSpeed, 1f, "Setup failed: the character didn't get past 1 m/s.");
            Assert.Less(harness.LastRequestedSpeed, 10f, "Setup failed: the character got past the max speed.");

            harness.Controller.Move(Vector3.zero);
            harness.Step();
            harness.Controller.Move(Vector3.zero);

            Assert.AreEqual(StationaryPivotId, pivotId);
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
