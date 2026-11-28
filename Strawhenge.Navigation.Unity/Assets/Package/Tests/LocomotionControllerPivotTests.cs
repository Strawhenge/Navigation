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

            harness.Motor.Rotation = Quaternion.LookRotation(Vector3.back); // Root motion played the turn.
            harness.Controller.CompletePivot();
            harness.Step();

            Assert.Less(Quaternion.Angle(harness.Motor.Rotation, Quaternion.LookRotation(Vector3.back)), AngleTolerance,
                "The character should face where the pivot turned it.");
            Assert.Greater(harness.Motor.Moves.Count, moves, "The motor should move again once the pivot is complete.");
        }

        // The animation sync and Pivoting expect the velocity coming in, whatever the motor reports meanwhile.
        [Test]
        public void Keeps_the_velocity_coming_in_while_a_pivot_plays()
        {
            var harness = RunAndTurnAround();
            var pivotRequested = false;
            harness.Controller.PivotRequested += _ => pivotRequested = true;
            harness.Step();
            Assert.IsTrue(pivotRequested, "Setup failed: no pivot was requested.");
            var comingIn = harness.Controller.CurrentVelocity;
            Assert.Greater(comingIn.magnitude, 3f, "Setup failed: the character wasn't moving into the pivot.");

            harness.Motor.ForgetVelocity();
            harness.StepFor(0.5f);

            Assert.AreEqual(comingIn, harness.Controller.CurrentVelocity,
                "CurrentVelocity should stay the velocity coming in while the pivot plays.");
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

            harness.Controller.CompletePivot();
            harness.StepFor(1f); // 180 degrees at the default 360 degrees per second takes 0.5 s.

            Assert.AreEqual(1, pivotsRequested, "The pivot shouldn't be requested again straight away.");
            Assert.Less(Quaternion.Angle(harness.Motor.Rotation, Quaternion.LookRotation(Vector3.back)), AngleTolerance,
                "The character should have turned around normally.");
        }

        // Root motion turns the character while the pivot plays, so completing it mustn't turn it any further
        // (the Animator's last-frame delta used to be applied again on top). Not stepped afterwards, as turning
        // towards the input would hide an over-rotation.
        [Test]
        public void Pivot_turned_by_root_motion_is_not_turned_again_when_it_completes()
        {
            var harness = RunAndTurnAround();
            var pivotRequested = false;
            harness.Controller.PivotRequested += _ => pivotRequested = true;
            harness.Step();
            Assert.IsTrue(pivotRequested, "Setup failed: no pivot was requested.");

            harness.Motor.Rotation = Quaternion.LookRotation(Vector3.back); // Root motion played the turn.
            harness.Controller.CompletePivot();

            Assert.Less(Quaternion.Angle(harness.Motor.Rotation, Quaternion.LookRotation(Vector3.back)), AngleTolerance,
                "The character should face where root motion turned it.");
        }

        // Root motion can drift a little even when the clip doesn't turn the character: that's still no turn.
        [Test]
        public void Turns_normally_after_a_pivot_that_barely_turned_the_character()
        {
            var harness = RunAndTurnAround();
            var pivotsRequested = 0;
            harness.Controller.PivotRequested += _ => pivotsRequested++;
            harness.Step();
            Assert.AreEqual(1, pivotsRequested, "Setup failed: no pivot was requested.");

            harness.Motor.Rotation = Quaternion.Euler(0, 0.5f, 0) * harness.Motor.Rotation;
            harness.Controller.CompletePivot();
            harness.StepFor(1f);

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

        // Nothing lets the character fall while a pivot holds it still, so a pivot whose root motion carries it off
        // a ledge is given up.
        [Test]
        public void Pivot_is_cancelled_when_it_carries_the_character_off_the_ground()
        {
            var harness = RunAndTurnAround();
            var pivotRequested = false;
            var pivotCancelled = false;
            harness.Controller.PivotRequested += _ => pivotRequested = true;
            harness.Controller.PivotCancelled += () => pivotCancelled = true;
            harness.Step();
            Assert.IsTrue(pivotRequested, "Setup failed: no pivot was requested.");

            harness.Motor.GroundHeight = null; // Root motion carried it over a ledge.
            harness.Step();

            Assert.IsTrue(pivotCancelled);
        }

        // Baseline for the test above.
        [Test]
        public void Pivot_is_not_cancelled_while_the_character_stays_on_the_ground()
        {
            var harness = RunAndTurnAround();
            var pivotRequested = false;
            var pivotCancelled = false;
            harness.Controller.PivotRequested += _ => pivotRequested = true;
            harness.Controller.PivotCancelled += () => pivotCancelled = true;
            harness.Step();
            Assert.IsTrue(pivotRequested, "Setup failed: no pivot was requested.");

            harness.StepFor(0.5f);

            Assert.IsFalse(pivotCancelled);
        }

        [Test]
        public void Character_falls_straight_away_when_a_pivot_carries_it_off_the_ground()
        {
            var harness = RunAndTurnAround();
            var pivotRequested = false;
            harness.Controller.PivotRequested += _ => pivotRequested = true;
            harness.Step();
            Assert.IsTrue(pivotRequested, "Setup failed: no pivot was requested.");
            var moves = harness.Motor.Moves.Count;

            harness.Motor.GroundHeight = null;
            harness.StepFor(0.2f);

            Assert.Greater(harness.Motor.Moves.Count, moves, "The motor should move again once the pivot is given up.");
            Assert.Less(harness.Motor.Position.y, -0.1f, "The character should be falling.");
        }

        // It isn't airborne until it has dropped FallDistance, so that alone wouldn't stop the same pivot matching
        // again straight away.
        [Test]
        public void Pivot_is_not_requested_again_after_it_carried_the_character_off_the_ground()
        {
            var harness = RunAndTurnAround();
            var pivotsRequested = 0;
            harness.Controller.PivotRequested += _ => pivotsRequested++;
            harness.Step();
            Assert.AreEqual(1, pivotsRequested, "Setup failed: no pivot was requested.");

            harness.Motor.GroundHeight = null;
            harness.StepFor(0.3f);

            Assert.AreEqual(1, pivotsRequested);
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
