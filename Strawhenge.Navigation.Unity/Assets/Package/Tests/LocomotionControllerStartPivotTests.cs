using NUnit.Framework;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Tests
{
    // From standing, towards a direction a stationary or start pivot covers, the character holds still for
    // StationaryPivotDecisionTime to tell a tap from a hold: released, the stationary pivot plays; still held, the
    // start pivot, or with none, it turns and moves off normally.
    public class LocomotionControllerStartPivotTests
    {
        const int StationaryPivotId = 4;
        const int StartPivotId = 5;
        const float AngleTolerance = 1f;

        // Turns of 60 to 120 degrees to the right from standing.
        static readonly IPivotProfile StationaryPivot = new FakePivotProfile(
            StationaryPivotId,
            speedRange: new ValueRange(0f, 0.5f),
            new ValueRange(60f, 120f));

        static readonly IPivotProfile StartPivot = new FakePivotProfile(
            StartPivotId,
            speedRange: new ValueRange(0f, 0.5f),
            new ValueRange(60f, 120f));

        [Test]
        public void Stationary_pivot_is_requested_for_a_tap_given_and_released_between_two_steps()
        {
            var harness = Create(stationary: true);
            int? pivotId = null;
            harness.Controller.PivotRequested += id => pivotId = id;

            harness.Controller.Move(Vector3.right);
            harness.Controller.Move(Vector3.zero);
            harness.Step();

            Assert.AreEqual(StationaryPivotId, pivotId);
        }

        // A stick ramps up over a few frames, and comes back to the centre through smaller inputs.
        [Test]
        public void Stationary_pivot_is_requested_for_a_stick_flick()
        {
            var harness = Create(stationary: true);
            int? pivotId = null;
            harness.Controller.PivotRequested += id => pivotId = id;

            foreach (var stick in new[] { 0.3f, 0.7f, 1f, 1f, 0.5f, 0f })
            {
                harness.Controller.Move(Vector3.right * stick);
                harness.Step();
            }

            Assert.AreEqual(StationaryPivotId, pivotId);
        }

        [Test]
        public void Holds_still_without_turning_while_deciding()
        {
            var harness = Create(stationary: true);
            var pivotRequested = false;
            harness.Controller.PivotRequested += _ => pivotRequested = true;

            harness.Controller.Move(Vector3.right);
            harness.StepFor(DefaultLocomotionSettings.StationaryPivotDecisionTime * 0.5f);

            Assert.IsFalse(pivotRequested, "Setup failed: a pivot was requested before the decision.");
            Assert.AreEqual(0f, harness.LastRequestedSpeed, 0.001f, "The character shouldn't move while deciding.");
            Assert.Less(Quaternion.Angle(harness.Motor.Rotation, Quaternion.identity), AngleTolerance,
                "The character shouldn't turn while deciding.");
        }

        [Test]
        public void Start_pivot_is_requested_when_the_input_is_held()
        {
            var harness = Create(stationary: true, start: true);
            int? pivotId = null;
            harness.Controller.PivotRequested += id => pivotId = id;

            harness.Controller.Move(Vector3.right);
            harness.StepFor(DefaultLocomotionSettings.StationaryPivotDecisionTime + 0.05f);

            Assert.AreEqual(StartPivotId, pivotId);
        }

        // Without a start pivot, holding never waits for the stationary pivot's animation: just the decision.
        [Test]
        public void Turns_and_moves_off_after_the_decision_when_held_without_a_start_pivot()
        {
            var harness = Create(stationary: true);
            var pivotRequested = false;
            harness.Controller.PivotRequested += _ => pivotRequested = true;

            harness.Controller.Move(Vector3.right);
            harness.StepFor(DefaultLocomotionSettings.StationaryPivotDecisionTime + 0.1f);

            Assert.IsFalse(pivotRequested, "No pivot should be requested.");
            Assert.Greater(harness.LastRequestedSpeed, 0.5f, "The character should be moving off.");

            harness.StepFor(0.5f);
            Assert.Less(Quaternion.Angle(harness.Motor.Rotation, Quaternion.LookRotation(Vector3.right)), AngleTolerance,
                "The character should have turned to the input normally.");
        }

        [Test]
        public void Turns_normally_for_a_tap_without_a_stationary_pivot()
        {
            var harness = Create(start: true);
            var pivotRequested = false;
            harness.Controller.PivotRequested += _ => pivotRequested = true;

            harness.Controller.Move(Vector3.right);
            harness.Step();
            harness.Controller.Move(Vector3.zero);
            harness.StepFor(0.5f);

            Assert.IsFalse(pivotRequested, "No pivot should be requested.");
            Assert.Less(Quaternion.Angle(harness.Motor.Rotation, Quaternion.LookRotation(Vector3.right)), AngleTolerance,
                "The character should have turned to the input normally.");
        }

        [Test]
        public void Moves_off_straight_away_towards_a_direction_no_pivot_covers()
        {
            var harness = Create(stationary: true, start: true);

            harness.Controller.Move(Quaternion.Euler(0f, 20f, 0f) * Vector3.forward);
            harness.Step();

            Assert.Greater(harness.LastRequestedSpeed, 0f);
        }

        [Test]
        public void Decision_takes_the_stationary_pivot_decision_time()
        {
            var harness = Create(stationary: true, start: true, decisionTime: 0.5f);
            var pivotRequested = false;
            harness.Controller.PivotRequested += _ => pivotRequested = true;

            harness.Controller.Move(Vector3.right);
            harness.StepFor(0.4f);
            Assert.IsFalse(pivotRequested, "The start pivot shouldn't be requested before the decision time.");
            Assert.AreEqual(0f, harness.LastRequestedSpeed, 0.001f, "The character should still be held still.");

            harness.StepFor(0.15f);
            Assert.IsTrue(pivotRequested, "The start pivot should be requested after the decision time.");
        }

        [Test]
        public void Jumping_while_deciding_jumps()
        {
            var harness = Create(stationary: true, start: true);
            var jumpBegan = false;
            var pivotRequested = false;
            harness.Controller.JumpBegan += () => jumpBegan = true;
            harness.Controller.PivotRequested += _ => pivotRequested = true;

            harness.Controller.Move(Vector3.right);
            harness.Step();
            harness.Controller.Jump();
            harness.StepFor(0.3f);

            Assert.IsTrue(jumpBegan, "The character should have jumped.");
            Assert.IsFalse(pivotRequested, "No pivot should be requested.");
        }

        static LocomotionControllerHarness Create(
            bool stationary = false,
            bool start = false,
            float decisionTime = DefaultLocomotionSettings.StationaryPivotDecisionTime) =>
            new(new FakeLocomotionSettings
            {
                StationaryPivots = stationary ? new[] { StationaryPivot } : new IPivotProfile[0],
                StartPivots = start ? new[] { StartPivot } : new IPivotProfile[0],
                StationaryPivotDecisionTime = decisionTime
            });
    }
}
