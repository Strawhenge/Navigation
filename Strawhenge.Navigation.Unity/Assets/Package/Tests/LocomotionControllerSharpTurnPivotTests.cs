using NUnit.Framework;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Tests
{
    // A stick sweeps through the angles in between on its way round, where a keyboard jumps straight to the new
    // direction. The pivot should still be the one for the speed and turn the sweep began with.
    public class LocomotionControllerSharpTurnPivotTests
    {
        const int WalkingPivotId = 1;
        const int RunningPivotId = 2;

        // Like the dev scene's Walking 180 and Running 180, listed in the same order.
        static readonly IPivotProfile WalkingPivot = new FakePivotProfile(
            WalkingPivotId,
            speedRange: new ValueRange(1f, 2f),
            new ValueRange(-190f, -150f),
            new ValueRange(150f, 190f));

        static readonly IPivotProfile RunningPivot = new FakePivotProfile(
            RunningPivotId,
            speedRange: new ValueRange(2f, float.PositiveInfinity),
            new ValueRange(-190f, -170f),
            new ValueRange(170f, 190f));

        // Baseline: a keyboard reverses in one step.
        [Test]
        public void Running_pivot_is_requested_when_reversing_straight_away_while_sprinting()
        {
            var harness = CreateMovingForward(sprint: true);
            int? pivotId = null;
            harness.Controller.PivotRequested += id => pivotId = id;

            harness.Controller.Move(Vector3.back);
            harness.Step();

            Assert.AreEqual(RunningPivotId, pivotId);
        }

        [TestCase(1f, TestName = "Running_pivot_is_requested_when_sweeping_round_to_the_right_while_sprinting")]
        [TestCase(-1f, TestName = "Running_pivot_is_requested_when_sweeping_round_to_the_left_while_sprinting")]
        public void Running_pivot_is_requested_when_sweeping_round_while_sprinting(float side)
        {
            var harness = CreateMovingForward(sprint: true);
            int? pivotId = null;
            harness.Controller.PivotRequested += id => pivotId = id;

            Sweep(harness, side, 60f, 130f, 150f, 180f);

            Assert.AreEqual(RunningPivotId, pivotId);
        }

        [Test]
        public void Walking_pivot_is_requested_when_sweeping_round_while_walking()
        {
            var harness = CreateMovingForward(walk: true);
            int? pivotId = null;
            harness.Controller.PivotRequested += id => pivotId = id;

            Sweep(harness, 1f, 60f, 130f, 150f, 180f);

            Assert.AreEqual(WalkingPivotId, pivotId);
        }

        // A sweep that comes back is forgotten: the next sharp turn is matched afresh, from where the character
        // then faces. A remembered turn would measure the reversal from the old facing, outside the running
        // pivot's angles.
        [Test]
        public void Sweep_that_comes_back_is_forgotten_before_the_next_sharp_turn()
        {
            var harness = CreateMovingForward(sprint: true);
            int? pivotId = null;
            harness.Controller.PivotRequested += id => pivotId = id;

            Sweep(harness, 1f, 60f, 130f, 0f);
            harness.StepFor(1f);
            Assert.IsNull(pivotId, "Setup failed: the sweep that came back requested a pivot.");
            Assert.Greater(harness.LastRequestedSpeed, 2f, "Setup failed: the character never got back up to speed.");

            harness.Controller.Move(-ForwardOf(harness));
            harness.Step();

            Assert.AreEqual(RunningPivotId, pivotId);
        }

        // Turning sharply without turning around still turns normally, with the speed limited for the turn.
        [Test]
        public void Sweep_that_stops_short_of_a_reversal_requests_no_pivot()
        {
            var harness = CreateMovingForward(sprint: true);
            var pivotRequested = false;
            harness.Controller.PivotRequested += _ => pivotRequested = true;

            Sweep(harness, 1f, 60f, 100f);
            Assert.LessOrEqual(harness.LastRequestedSpeed, DefaultLocomotionSettings.RunSpeed + 0.001f,
                "The speed should still be limited for a sharp turn.");

            harness.StepFor(0.5f);

            Assert.IsFalse(pivotRequested);
        }

        static LocomotionControllerHarness CreateMovingForward(bool sprint = false, bool walk = false)
        {
            var harness = new LocomotionControllerHarness(new FakeLocomotionSettings
            {
                MovingPivots = new[] { WalkingPivot, RunningPivot }
            });

            harness.Controller.Sprint = sprint;
            harness.Controller.Walk = walk;
            harness.Controller.Move(Vector3.forward);
            harness.StepFor(1.5f);

            var expectedSpeed = sprint
                ? DefaultLocomotionSettings.SprintSpeed
                : walk
                    ? DefaultLocomotionSettings.WalkSpeed
                    : DefaultLocomotionSettings.RunSpeed;

            Assert.AreEqual(expectedSpeed, harness.LastRequestedSpeed, 0.01f,
                "Setup failed: the character never got up to speed.");

            return harness;
        }

        // One step per angle, measured from straight ahead (world forward), to the right for side 1 and the left
        // for side -1.
        static void Sweep(LocomotionControllerHarness harness, float side, params float[] angles)
        {
            foreach (var angle in angles)
            {
                harness.Controller.Move(Quaternion.Euler(0f, angle * side, 0f) * Vector3.forward);
                harness.Step();
            }
        }

        static Vector3 ForwardOf(LocomotionControllerHarness harness) => harness.Motor.Rotation * Vector3.forward;
    }
}
