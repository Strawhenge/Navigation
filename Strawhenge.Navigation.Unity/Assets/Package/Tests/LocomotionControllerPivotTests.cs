using NUnit.Framework;
using System.Linq;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Tests
{
    // Moving and stationary pivots, on a FakeMotor.
    public class LocomotionControllerPivotTests
    {
        const int MovingPivotId = 3;
        const int StationaryPivotId = 4;
        const float AngleTolerance = 1f;

        // A 180 degree turn while running.
        static readonly IPivotProfile MovingPivot = new FakePivotProfile(
            MovingPivotId,
            speedRange: new ValueRange(3f, 10f),
            new ValueRange(150f, 180f),
            new ValueRange(-180f, -150f));

        // A 90 degree turn to the right from standing.
        static readonly IPivotProfile StationaryPivot = new FakePivotProfile(
            StationaryPivotId,
            speedRange: new ValueRange(0f, 0.5f),
            new ValueRange(60f, 120f));

        [Test]
        public void Moving_pivot_is_requested_when_turning_around_while_running()
        {
            var harness = RunAndTurnAround();
            int? pivotId = null;
            harness.Controller.PivotRequested += id => pivotId = id;

            harness.Step();

            Assert.AreEqual(MovingPivotId, pivotId);
        }

        [Test]
        public void Pivot_holds_the_character_still_until_it_is_completed()
        {
            var harness = RunAndTurnAround();
            var pivotRequested = false;
            harness.Controller.PivotRequested += _ => pivotRequested = true;
            harness.Step();
            Assert.IsTrue(pivotRequested, "Setup failed: no pivot was requested.");

            var moves = harness.Motor.Moves.Count;
            harness.StepFor(0.5f);
            Assert.AreEqual(moves, harness.Motor.Moves.Count, "The motor was moved during the pivot.");

            harness.Controller.CompletePivot(Quaternion.Euler(0, 180, 0));
            harness.Step();

            Assert.Less(Quaternion.Angle(harness.Motor.Rotation, Quaternion.LookRotation(Vector3.back)), AngleTolerance,
                "The pivot's rotation should have been applied.");
            Assert.Greater(harness.Motor.Moves.Count, moves, "The motor should move again once the pivot is complete.");
        }

        // A pivot that didn't turn the character (no animation played it) would match again straight away and
        // stop it ever turning. Instead the character turns normally.
        [Test]
        public void Turns_normally_after_a_pivot_completes_without_turning()
        {
            var harness = RunAndTurnAround();
            var pivotsRequested = 0;
            harness.Controller.PivotRequested += _ => pivotsRequested++;
            harness.Step();
            Assert.AreEqual(1, pivotsRequested, "Setup failed: no pivot was requested.");

            harness.Controller.CompletePivot(Quaternion.identity);
            harness.StepFor(1f); // 180 degrees at the default 360 degrees per second takes 0.5 s.

            Assert.AreEqual(1, pivotsRequested, "The pivot shouldn't be requested again straight away.");
            Assert.Less(Quaternion.Angle(harness.Motor.Rotation, Quaternion.LookRotation(Vector3.back)), AngleTolerance,
                "The character should have turned around normally.");
        }

        [Test]
        public void Pivot_is_given_up_when_it_is_never_completed()
        {
            var harness = RunAndTurnAround();
            harness.Step();
            var moves = harness.Motor.Moves.Count;

            harness.StepFor(LocomotionControllerHarness.AnimatorWaitTimeout + 0.5f);

            Assert.IsTrue(harness.Logger.Warnings.Any(w => w.Contains($"Pivot ID {MovingPivotId}")),
                "A warning should say the pivot timed out.");
            Assert.Greater(harness.Motor.Moves.Count, moves, "The motor should move again after the timeout.");
        }

        [Test]
        public void Never_pivots_while_strafing()
        {
            var harness = RunAndTurnAround(strafe: true);
            var pivotRequested = false;
            harness.Controller.PivotRequested += _ => pivotRequested = true;

            harness.StepFor(0.5f);

            Assert.IsFalse(pivotRequested);
        }

        // A stationary pivot plays for a flick: a turn from standing, with the input released straight after.
        [Test]
        public void Stationary_pivot_is_requested_when_the_input_is_released_after_a_turn_from_standing()
        {
            var harness = CreateWithStationaryPivot();
            int? pivotId = null;
            harness.Controller.PivotRequested += id => pivotId = id;

            harness.Controller.Move(Vector3.right);
            harness.Step();
            harness.Controller.Move(Vector3.zero);
            harness.Step();
            harness.Controller.Move(Vector3.zero);

            Assert.AreEqual(StationaryPivotId, pivotId);
        }

        // Holding the input instead gets the character moving, which rules the stationary pivot out.
        [Test]
        public void Stationary_pivot_is_not_requested_when_the_input_is_held()
        {
            var harness = CreateWithStationaryPivot();
            var pivotRequested = false;
            harness.Controller.PivotRequested += _ => pivotRequested = true;

            harness.Controller.Move(Vector3.right);
            harness.StepFor(0.5f);
            harness.Controller.Move(Vector3.zero);
            harness.Step();
            harness.Controller.Move(Vector3.zero);
            harness.Step();

            Assert.IsFalse(pivotRequested);
        }

        static LocomotionControllerHarness RunAndTurnAround(bool strafe = false)
        {
            var harness = new LocomotionControllerHarness(new FakeLocomotionSettings
            {
                MovingPivots = new[] { MovingPivot }
            });

            harness.Controller.Strafe = strafe;
            harness.Controller.Move(Vector3.forward);
            harness.StepFor(1f);
            Assert.Greater(harness.LastRequestedSpeed, 3f, "Setup failed: the character never got up to speed.");

            harness.Controller.Move(Vector3.back);
            return harness;
        }

        static LocomotionControllerHarness CreateWithStationaryPivot() =>
            new(new FakeLocomotionSettings { StationaryPivots = new[] { StationaryPivot } });
    }
}
