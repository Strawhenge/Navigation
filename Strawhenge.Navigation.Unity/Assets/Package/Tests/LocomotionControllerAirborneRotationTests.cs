using NUnit.Framework;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Tests
{
    // Turning and pivots while jumping or falling, on a FakeMotor.
    public class LocomotionControllerAirborneRotationTests
    {
        const float FallDistance = 1f;
        const float AngleTolerance = 1f;
        const int MovingPivotId = 3;
        const int StationaryPivotId = 4;

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
        public void Turns_while_jumping_when_airborne_rotation_is_allowed()
        {
            var harness = Jump(new FakeLocomotionSettings { AllowAirborneRotation = true });

            harness.Controller.Move(Vector3.right);
            harness.StepFor(0.2f);

            Assert.IsFalse(harness.Motor.IsGrounded, "Setup failed: the character landed too soon.");
            Assert.Greater(TurnedAngle(harness), 30f, "The character should turn in the air.");
        }

        [Test]
        public void Keeps_its_rotation_while_jumping_when_airborne_rotation_is_not_allowed()
        {
            var harness = Jump(new FakeLocomotionSettings { AllowAirborneRotation = false });

            harness.Controller.Move(Vector3.right);
            harness.StepFor(0.2f);

            Assert.IsFalse(harness.Motor.IsGrounded, "Setup failed: the character landed too soon.");
            Assert.Less(TurnedAngle(harness), 0.01f, "The character shouldn't turn in the air.");
        }

        [Test]
        public void Turns_to_the_input_once_it_lands_when_airborne_rotation_is_not_allowed()
        {
            var harness = Jump(new FakeLocomotionSettings { AllowAirborneRotation = false });

            harness.Controller.Move(Vector3.right);
            Assert.IsTrue(harness.StepUntil(() => harness.Motor.IsGrounded), "Setup failed: the character never landed.");
            Assert.Less(TurnedAngle(harness), 0.01f, "Setup failed: the character turned in the air.");
            harness.StepFor(0.5f);

            Assert.Less(Quaternion.Angle(harness.Motor.Rotation, Quaternion.LookRotation(Vector3.right)), AngleTolerance,
                "The character should turn to the input once it lands.");
        }

        [Test]
        public void Turns_while_falling_when_airborne_rotation_is_allowed()
        {
            var harness = Fall(new FakeLocomotionSettings { AllowAirborneRotation = true, FallDistance = FallDistance });

            harness.Controller.Move(Vector3.right);
            harness.StepFor(0.2f);

            Assert.IsFalse(harness.Motor.IsGrounded, "Setup failed: the character landed.");
            Assert.Greater(TurnedAngle(harness), 30f, "The character should turn while falling.");
        }

        [Test]
        public void Keeps_its_rotation_while_falling_when_airborne_rotation_is_not_allowed()
        {
            var harness = Fall(new FakeLocomotionSettings { AllowAirborneRotation = false, FallDistance = FallDistance });

            harness.Controller.Move(Vector3.right);
            harness.StepFor(0.2f);

            Assert.IsFalse(harness.Motor.IsGrounded, "Setup failed: the character landed.");
            Assert.Less(TurnedAngle(harness), 0.01f, "The character shouldn't turn while falling.");
        }

        // Stepping down stairs or a small drop isn't a fall, so it isn't airborne either.
        [Test]
        public void Turns_stepping_down_a_drop_shorter_than_fall_distance_when_airborne_rotation_is_not_allowed()
        {
            var harness = new LocomotionControllerHarness(
                new FakeLocomotionSettings { AllowAirborneRotation = false, FallDistance = FallDistance });
            harness.Step();
            Assert.IsTrue(harness.Motor.IsGrounded, "Setup failed: the character isn't on the ground.");

            harness.Motor.GroundHeight = -0.5f;
            harness.Controller.Move(Vector3.right);
            harness.Step(6);

            Assert.IsFalse(harness.Motor.IsGrounded, "Setup failed: the character has already reached the lower ground.");
            Assert.Greater(TurnedAngle(harness), 20f, "The character should turn stepping down a small drop.");
        }

        [Test]
        public void No_moving_pivot_while_jumping()
        {
            var harness = new LocomotionControllerHarness(new FakeLocomotionSettings { MovingPivots = new[] { MovingPivot } });
            var pivotRequested = false;
            harness.Controller.PivotRequested += _ => pivotRequested = true;
            harness.Controller.Move(Vector3.forward);
            harness.StepFor(1f);
            Assert.Greater(harness.LastRequestedSpeed, 3f, "Setup failed: the character isn't running.");
            Assert.IsFalse(pivotRequested, "Setup failed: a pivot was requested before the jump.");
            JumpNow(harness);

            harness.Controller.Move(Vector3.back);
            harness.StepFor(0.3f);

            Assert.IsFalse(harness.Motor.IsGrounded, "Setup failed: the character landed too soon.");
            Assert.IsFalse(pivotRequested, "A moving pivot shouldn't be requested in the air.");
        }

        [Test]
        public void Stationary_pivot_queued_before_a_jump_is_not_requested_in_the_air()
        {
            var harness = new LocomotionControllerHarness(
                new FakeLocomotionSettings { StationaryPivots = new[] { StationaryPivot } });
            var pivotRequested = false;
            harness.Controller.PivotRequested += _ => pivotRequested = true;

            harness.Controller.Move(Vector3.right);
            harness.Step();
            JumpNow(harness);
            harness.Controller.Move(Vector3.zero);
            harness.Step();
            harness.Controller.Move(Vector3.zero);
            harness.Step();

            Assert.IsFalse(harness.Motor.IsGrounded, "Setup failed: the character landed too soon.");
            Assert.IsFalse(pivotRequested, "A stationary pivot shouldn't be requested in the air.");
        }

        static LocomotionControllerHarness Jump(FakeLocomotionSettings settings)
        {
            var harness = new LocomotionControllerHarness(settings);
            JumpNow(harness);
            return harness;
        }

        static void JumpNow(LocomotionControllerHarness harness)
        {
            var jumpBegan = false;
            harness.Controller.JumpBegan += () => jumpBegan = true;

            harness.Controller.Jump();
            harness.Step();

            Assert.IsTrue(jumpBegan, "Setup failed: the jump didn't begin.");
        }

        // Walks off a ledge (the floor drops away) and waits for the fall to begin.
        static LocomotionControllerHarness Fall(FakeLocomotionSettings settings)
        {
            var harness = new LocomotionControllerHarness(settings);
            var fallBegan = false;
            harness.Controller.FallBegan += () => fallBegan = true;

            harness.Motor.GroundHeight = null;

            Assert.IsTrue(harness.StepUntil(() => fallBegan), "Setup failed: the fall didn't begin.");
            return harness;
        }

        static float TurnedAngle(LocomotionControllerHarness harness) =>
            Quaternion.Angle(harness.Motor.Rotation, Quaternion.identity);
    }
}
