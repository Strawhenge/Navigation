using NUnit.Framework;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Tests
{
    // Speeds, turning and walls, on a FakeMotor.
    public class LocomotionControllerMovementTests
    {
        const float SpeedTolerance = 0.01f;
        const float AngleTolerance = 1f;

        [Test]
        public void Moves_in_the_input_direction()
        {
            var harness = new LocomotionControllerHarness();

            harness.Controller.Move(Vector3.forward);
            harness.StepFor(1f);

            Assert.Greater(harness.Motor.Position.z, 1f, "The character should have moved forward.");
            Assert.AreEqual(0f, harness.Motor.Position.x, 0.001f, "The character shouldn't have moved sideways.");
        }

        [Test]
        public void Accelerates_to_run_speed()
        {
            var harness = new LocomotionControllerHarness();

            harness.Controller.Move(Vector3.forward);
            harness.StepFor(1f);

            Assert.AreEqual(DefaultLocomotionSettings.RunSpeed, harness.LastRequestedSpeed, SpeedTolerance);
        }

        [Test]
        public void Walks_at_walk_speed()
        {
            var harness = new LocomotionControllerHarness();

            harness.Controller.Walk = true;
            harness.Controller.Move(Vector3.forward);
            harness.StepFor(1f);

            Assert.AreEqual(DefaultLocomotionSettings.WalkSpeed, harness.LastRequestedSpeed, SpeedTolerance);
        }

        [Test]
        public void Sprints_at_sprint_speed()
        {
            var harness = new LocomotionControllerHarness();

            harness.Controller.Sprint = true;
            harness.Controller.Move(Vector3.forward);
            harness.StepFor(1f);

            Assert.AreEqual(DefaultLocomotionSettings.SprintSpeed, harness.LastRequestedSpeed, SpeedTolerance);
        }

        [Test]
        public void Sprinting_while_strafing_runs_at_run_speed()
        {
            var harness = new LocomotionControllerHarness();

            harness.Controller.Sprint = true;
            harness.Controller.Strafe = true;
            harness.Controller.Move(Vector3.forward);
            harness.StepFor(1f);

            Assert.AreEqual(DefaultLocomotionSettings.RunSpeed, harness.LastRequestedSpeed, SpeedTolerance);
        }

        [Test]
        public void Decelerates_to_a_stop_when_the_input_is_released()
        {
            var harness = new LocomotionControllerHarness();

            harness.Controller.Move(Vector3.forward);
            harness.StepFor(1f);
            Assert.Greater(harness.LastRequestedSpeed, 1f, "Setup failed: the character never got moving.");

            harness.Controller.Move(Vector3.zero);
            harness.StepFor(0.5f);

            Assert.AreEqual(0f, harness.LastRequestedSpeed, SpeedTolerance);
        }

        [Test]
        public void Turns_to_face_the_input_direction()
        {
            var harness = new LocomotionControllerHarness();

            harness.Controller.Move(Vector3.right);
            harness.StepFor(0.5f); // 90 degrees at the default 360 degrees per second takes 0.25 s.

            Assert.Less(Quaternion.Angle(harness.Motor.Rotation, Quaternion.LookRotation(Vector3.right)), AngleTolerance);
        }

        [Test]
        public void Keeps_its_facing_while_strafing()
        {
            var harness = new LocomotionControllerHarness();

            harness.Controller.Strafe = true;
            harness.Controller.Move(Vector3.right);
            harness.StepFor(0.5f);

            Assert.Less(Quaternion.Angle(harness.Motor.Rotation, Quaternion.identity), AngleTolerance,
                "A strafing character shouldn't turn.");
            Assert.Greater(harness.Motor.Position.x, 0.5f, "The strafing character should have moved right.");
        }

        // Baseline for the test below: a move that doesn't happen isn't enough to slow down. It must be reported
        // as blocked sideways.
        [Test]
        public void Keeps_its_speed_when_held_back_without_being_blocked_sideways()
        {
            var harness = RunIntoWall(blockedSideways: false);

            Assert.AreEqual(DefaultLocomotionSettings.RunSpeed, harness.LastRequestedSpeed, SpeedTolerance);
        }

        [Test]
        public void Slows_to_its_actual_speed_when_blocked_sideways()
        {
            var harness = RunIntoWall(blockedSideways: true);

            Assert.Less(harness.LastRequestedSpeed, 1f,
                "Blocked by a wall, the speed should drop to what the character actually managed (none).");
        }

        static LocomotionControllerHarness RunIntoWall(bool blockedSideways)
        {
            var harness = new LocomotionControllerHarness();

            harness.Controller.Move(Vector3.forward);
            harness.StepFor(1f);
            Assert.AreEqual(DefaultLocomotionSettings.RunSpeed, harness.LastRequestedSpeed, SpeedTolerance,
                "Setup failed: the character never reached run speed.");

            harness.Motor.HorizontalFreedom = 0f;
            harness.Motor.BlockedSideways = blockedSideways;
            harness.Step(2); // One step hits the wall; the next shows what the controller made of it.

            return harness;
        }
    }
}
