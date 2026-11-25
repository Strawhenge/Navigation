using NUnit.Framework;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Tests
{
    // A stick pushed part of the way: the character moves at that fraction of the selected speed, and pivots and
    // jumps are matched on the speed it's really moving at.
    public class LocomotionControllerAnalogTests
    {
        const float HalfStick = 0.5f;
        const float HalfRunSpeed = DefaultLocomotionSettings.RunSpeed * HalfStick;
        const float SpeedTolerance = 0.01f;

        const int JoggingPivotId = 1;
        const int RunningPivotId = 2;
        const int StandingJumpId = 3;
        const int RunningJumpId = 4;

        static readonly IPivotProfile JoggingPivot = new FakePivotProfile(
            JoggingPivotId,
            speedRange: new ValueRange(1.5f, 3f),
            new ValueRange(150f, 180f),
            new ValueRange(-180f, -150f));

        static readonly IPivotProfile RunningPivot = new FakePivotProfile(
            RunningPivotId,
            speedRange: new ValueRange(3f, float.PositiveInfinity),
            new ValueRange(150f, 180f),
            new ValueRange(-180f, -150f));

        static readonly IJumpProfile RunningJump = new FakeJumpProfile(RunningJumpId, new ValueRange(3f, 10f));

        static readonly IJumpProfile StandingJump = new FakeJumpProfile(StandingJumpId, new ValueRange(0f, 3f));

        [Test]
        public void Half_a_stick_moves_at_half_the_speed()
        {
            var harness = RunForward(HalfStick);

            Assert.AreEqual(HalfRunSpeed, harness.LastRequestedSpeed, SpeedTolerance);
        }

        // Baseline for the half-stick reversal.
        [Test]
        public void Running_pivot_is_requested_when_reversing_with_the_stick_all_the_way()
        {
            var harness = RunForward(1f);
            int? pivotId = null;
            harness.Controller.PivotRequested += id => pivotId = id;

            harness.Controller.Move(Vector3.back);
            harness.Step();

            Assert.AreEqual(RunningPivotId, pivotId);
        }

        [Test]
        public void Pivot_for_the_speed_moved_at_is_requested_when_reversing_with_half_a_stick()
        {
            var harness = RunForward(HalfStick);
            int? pivotId = null;
            harness.Controller.PivotRequested += id => pivotId = id;

            harness.Controller.Move(Vector3.back * HalfStick);
            harness.Step();

            Assert.AreEqual(JoggingPivotId, pivotId);
        }

        // Baseline for the half-stick jump.
        [Test]
        public void Running_jump_is_requested_with_the_stick_all_the_way()
        {
            var harness = RunForward(1f);
            int? jumpId = null;
            harness.Controller.JumpTriggerRequested += id => jumpId = id;

            harness.Controller.Jump();

            Assert.AreEqual(RunningJumpId, jumpId);
        }

        [Test]
        public void Jump_for_the_speed_moved_at_is_requested_with_half_a_stick()
        {
            var harness = RunForward(HalfStick);
            int? jumpId = null;
            harness.Controller.JumpTriggerRequested += id => jumpId = id;

            harness.Controller.Jump();

            Assert.AreEqual(StandingJumpId, jumpId);
        }

        // Steered in the air towards the same half speed, not half of it again.
        [Test]
        public void Keeps_half_the_speed_in_the_air_with_half_a_stick()
        {
            var harness = RunForward(HalfStick, withJumps: false);
            var jumpBegan = false;
            harness.Controller.JumpBegan += () => jumpBegan = true;

            harness.Controller.Jump();
            harness.StepFor(0.2f);
            Assert.IsTrue(jumpBegan, "Setup failed: the character didn't jump.");
            Assert.IsFalse(harness.Motor.IsGrounded, "Setup failed: the character has already landed.");

            Assert.AreEqual(HalfRunSpeed, harness.LastRequestedSpeed, SpeedTolerance);
        }

        static LocomotionControllerHarness RunForward(float stick, bool withJumps = true)
        {
            var settings = new FakeLocomotionSettings
            {
                MovingPivots = new[] { RunningPivot, JoggingPivot }
            };

            if (withJumps)
                settings.Jumps = new[] { RunningJump, StandingJump };

            var harness = new LocomotionControllerHarness(settings);

            harness.Controller.Move(Vector3.forward * stick);
            harness.StepFor(1f);
            Assert.AreEqual(DefaultLocomotionSettings.RunSpeed * stick, harness.LastRequestedSpeed, SpeedTolerance,
                "Setup failed: the character never got up to speed.");

            return harness;
        }
    }
}
