using NUnit.Framework;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Tests
{
    // Horizontal movement while jumping or falling (AirborneMovement), on a FakeMotor.
    public class LocomotionControllerAirborneMovementTests
    {
        const float FallDistance = 1f;
        const float VelocityTolerance = 0.01f;

        [TestCase(AirborneMovement.Controlled)]
        [TestCase(AirborneMovement.ControlledKeepsMomentum)]
        public void Steers_in_the_air_when_controlled(AirborneMovement movement)
        {
            var harness = RunAndJump(new FakeLocomotionSettings { AirborneMovement = movement });

            harness.Controller.Move(Vector3.right);
            harness.StepFor(0.2f);

            Assert.IsFalse(harness.Motor.IsGrounded, "Setup failed: the character landed too soon.");
            Assert.Greater(Vector3.Dot(LastHorizontalVelocity(harness).normalized, Vector3.right), 0.99f,
                "The character should move the way it's steered.");
        }

        // Not straight to the input at the same speed: slows to a stop at Deceleration, then speeds up the other
        // way at Acceleration.
        [TestCase(AirborneMovement.Controlled)]
        [TestCase(AirborneMovement.ControlledKeepsMomentum)]
        public void Slows_down_then_speeds_up_turning_around_in_the_air_when_controlled(AirborneMovement movement)
        {
            var harness = RunAndJump(new FakeLocomotionSettings { AirborneMovement = movement });

            harness.Controller.Move(Vector3.back);
            harness.Step();

            var slowing = harness.Motor.LastMove.Velocity.z;
            Assert.Greater(slowing, 0f, "The character should still be going forward, slowing down.");
            Assert.Less(slowing, DefaultLocomotionSettings.RunSpeed, "The character should be slowing down.");

            harness.StepFor(0.3f); // 5 m/s to a stop at 30 m/s/s takes about 0.17 s.

            Assert.IsFalse(harness.Motor.IsGrounded, "Setup failed: the character landed too soon.");
            var turnedAround = harness.Motor.LastMove.Velocity.z;
            Assert.Less(turnedAround, 0f, "The character should be going back by now.");
            Assert.Greater(turnedAround, -DefaultLocomotionSettings.RunSpeed / 2f,
                "The character should still be speeding up going back.");
        }

        [Test]
        public void Slows_to_a_stop_in_the_air_when_the_input_is_released_with_controlled()
        {
            var harness = RunAndJump(new FakeLocomotionSettings { AirborneMovement = AirborneMovement.Controlled });

            harness.Controller.Move(Vector3.zero);
            harness.StepFor(0.3f);

            Assert.IsFalse(harness.Motor.IsGrounded, "Setup failed: the character landed too soon.");
            Assert.AreEqual(0f, harness.LastRequestedSpeed, VelocityTolerance, "The character should slow to a stop.");
        }

        // Without input the velocity used to follow the facing, which can be well off the way the character was
        // going in the air.
        [Test]
        public void Slows_down_the_way_it_was_going_when_the_input_is_released_with_controlled()
        {
            var harness = RunAndJump(new FakeLocomotionSettings
            {
                AirborneMovement = AirborneMovement.Controlled,
                AllowAirborneRotation = false
            });
            harness.Controller.Move(Vector3.right);
            harness.StepFor(0.3f);
            Assert.Less(Quaternion.Angle(harness.Motor.Rotation, Quaternion.identity), 0.01f,
                "Setup failed: the character turned in the air.");
            Assert.Greater(Vector3.Dot(LastHorizontalVelocity(harness).normalized, Vector3.right), 0.99f,
                "Setup failed: the character isn't moving right.");

            harness.Controller.Move(Vector3.zero);
            harness.Step();

            Assert.Greater(harness.LastRequestedSpeed, 1f, "Setup failed: the character has already stopped.");
            Assert.Greater(Vector3.Dot(LastHorizontalVelocity(harness).normalized, Vector3.right), 0.99f,
                "The character should keep going the way it was, not the way it's facing.");
        }

        [Test]
        public void Keeps_its_velocity_in_the_air_when_the_input_is_released_with_controlled_keeps_momentum()
        {
            var harness = RunAndJump(new FakeLocomotionSettings
            {
                AirborneMovement = AirborneMovement.ControlledKeepsMomentum
            });
            harness.Controller.Move(Vector3.right);
            harness.StepFor(0.1f);
            var released = LastHorizontalVelocity(harness);
            Assert.Greater(released.magnitude, 1f, "Setup failed: the character isn't moving.");

            harness.Controller.Move(Vector3.zero);
            harness.StepFor(0.3f);

            Assert.IsFalse(harness.Motor.IsGrounded, "Setup failed: the character landed too soon.");
            AssertVelocity(released, LastHorizontalVelocity(harness),
                "The character should carry on at the velocity it had when the input was released.");
        }

        [Test]
        public void Ignores_steering_in_the_air_with_momentum()
        {
            var harness = RunAndJump(new FakeLocomotionSettings { AirborneMovement = AirborneMovement.Momentum });
            var takeOff = LastHorizontalVelocity(harness);

            harness.Controller.Move(Vector3.right);
            harness.StepFor(0.3f);

            Assert.IsFalse(harness.Motor.IsGrounded, "Setup failed: the character landed too soon.");
            AssertVelocity(takeOff, LastHorizontalVelocity(harness),
                "The character should carry on at the velocity it jumped with.");
        }

        [Test]
        public void Keeps_its_velocity_in_the_air_when_the_input_is_released_with_momentum()
        {
            var harness = RunAndJump(new FakeLocomotionSettings { AirborneMovement = AirborneMovement.Momentum });
            var takeOff = LastHorizontalVelocity(harness);

            harness.Controller.Move(Vector3.zero);
            harness.StepFor(0.3f);

            Assert.IsFalse(harness.Motor.IsGrounded, "Setup failed: the character landed too soon.");
            AssertVelocity(takeOff, LastHorizontalVelocity(harness),
                "The character should carry on at the velocity it jumped with.");
        }

        [Test]
        public void Ignores_steering_while_falling_with_momentum()
        {
            var harness = new LocomotionControllerHarness(new FakeLocomotionSettings
            {
                AirborneMovement = AirborneMovement.Momentum,
                FallDistance = FallDistance
            });
            var fallBegan = false;
            harness.Controller.FallBegan += () => fallBegan = true;
            Run(harness);

            harness.Motor.GroundHeight = null;
            Assert.IsTrue(harness.StepUntil(() => fallBegan), "Setup failed: the fall didn't begin.");
            var falling = LastHorizontalVelocity(harness);

            harness.Controller.Move(Vector3.left);
            harness.StepFor(0.3f);

            AssertVelocity(falling, LastHorizontalVelocity(harness),
                "The character should carry on at the velocity it was falling with.");
        }

        // A drop shorter than FallDistance isn't airborne, so there's control as on the ground.
        [Test]
        public void Steers_stepping_down_a_drop_shorter_than_fall_distance_with_momentum()
        {
            var harness = new LocomotionControllerHarness(new FakeLocomotionSettings
            {
                AirborneMovement = AirborneMovement.Momentum,
                FallDistance = FallDistance
            });
            Run(harness);

            harness.Motor.GroundHeight = -0.5f;
            harness.Controller.Move(Vector3.right);
            harness.Step(6);

            Assert.IsFalse(harness.Motor.IsGrounded, "Setup failed: the character has already reached the lower ground.");
            Assert.Greater(Vector3.Dot(LastHorizontalVelocity(harness).normalized, Vector3.right), 0.99f,
                "The character should move the way it's steered.");
        }

        [TestCase(AirborneMovement.Momentum)]
        [TestCase(AirborneMovement.ControlledKeepsMomentum)]
        public void Momentum_is_stopped_by_a_wall(AirborneMovement movement)
        {
            var harness = RunAndJump(new FakeLocomotionSettings { AirborneMovement = movement });
            harness.Controller.Move(Vector3.zero);
            harness.Step();
            Assert.Greater(harness.LastRequestedSpeed, 1f, "Setup failed: the character isn't carrying momentum.");

            harness.Motor.BlockedSideways = true;
            harness.Motor.HorizontalFreedom = 0f;
            harness.Step(2);

            Assert.IsFalse(harness.Motor.IsGrounded, "Setup failed: the character landed too soon.");
            Assert.AreEqual(0f, harness.LastRequestedSpeed, VelocityTolerance, "A wall should stop the momentum.");
        }

        [TestCase(AirborneMovement.Momentum)]
        [TestCase(AirborneMovement.ControlledKeepsMomentum)]
        public void Has_control_again_once_it_lands(AirborneMovement movement)
        {
            var harness = RunAndJump(new FakeLocomotionSettings { AirborneMovement = movement });
            harness.Controller.Move(Vector3.zero);
            Assert.IsTrue(harness.StepUntil(() => harness.Motor.IsGrounded), "Setup failed: the character never landed.");
            Assert.Greater(harness.LastRequestedSpeed, 1f, "Setup failed: the character didn't carry its momentum.");

            harness.StepFor(0.5f);

            Assert.AreEqual(0f, harness.LastRequestedSpeed, VelocityTolerance,
                "Without input, the character should slow to a stop once it lands.");
        }

        static LocomotionControllerHarness RunAndJump(FakeLocomotionSettings settings)
        {
            var harness = new LocomotionControllerHarness(settings);
            var jumpBegan = false;
            harness.Controller.JumpBegan += () => jumpBegan = true;
            Run(harness);

            harness.Controller.Jump();
            harness.Step();

            Assert.IsTrue(jumpBegan, "Setup failed: the jump didn't begin.");
            return harness;
        }

        static void Run(LocomotionControllerHarness harness)
        {
            harness.Controller.Move(Vector3.forward);
            harness.StepFor(1f);

            Assert.AreEqual(DefaultLocomotionSettings.RunSpeed, harness.LastRequestedSpeed, VelocityTolerance,
                "Setup failed: the character isn't running.");
        }

        static Vector3 LastHorizontalVelocity(LocomotionControllerHarness harness) =>
            Vector3.ProjectOnPlane(harness.Motor.LastMove.Velocity, Vector3.up);

        static void AssertVelocity(Vector3 expected, Vector3 actual, string message) =>
            Assert.Less(Vector3.Distance(expected, actual), VelocityTolerance,
                $"{message} Expected {expected}, was {actual}.");
    }
}
