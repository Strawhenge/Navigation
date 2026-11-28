using NUnit.Framework;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Tests
{
    // What the locomotion movement source passes on from its LocomotionController.
    public class LocomotionMovementSourceTests
    {
        const int JumpId = 3;
        const int PivotId = 4;

        static readonly ValueRange Any = new(float.NegativeInfinity, float.PositiveInfinity);

        [Test]
        public void Passes_on_the_jump_id_of_a_wind_up()
        {
            var harness = WithJump();
            var source = new LocomotionMovementSource(harness.Controller);
            int? requestedId = null;
            source.JumpTriggerRequested += id => requestedId = id;

            harness.Controller.Jump();

            Assert.AreEqual(JumpId, requestedId);
        }

        // So the animation sync resets the Jump trigger, rather than it firing on the next return to locomotion.
        [Test]
        public void Passes_on_a_wind_up_that_timed_out()
        {
            var harness = WithJump();
            var source = new LocomotionMovementSource(harness.Controller);
            var cancelled = false;
            source.JumpTriggerCancelled += () => cancelled = true;

            harness.Controller.Jump();
            harness.StepFor(LocomotionControllerHarness.AnimatorWaitTimeout + 0.1f);

            Assert.IsTrue(cancelled);
        }

        // So the animation sync resets the Pivot trigger and root motion, and ignores the pivot's end.
        [Test]
        public void Passes_on_a_pivot_that_carried_the_character_off_the_ground()
        {
            var harness = new LocomotionControllerHarness(new FakeLocomotionSettings
            {
                MovingPivots = new IPivotProfile[] { new FakePivotProfile(PivotId, Any, new ValueRange(150f, 180f), new ValueRange(-180f, -150f)) }
            });

            var source = new LocomotionMovementSource(harness.Controller);
            var pivotRequested = false;
            var cancelled = false;
            source.PivotRequested += _ => pivotRequested = true;
            source.PivotCancelled += () => cancelled = true;

            harness.Controller.Move(Vector3.forward);
            harness.StepFor(1f);
            harness.Controller.Move(Vector3.back);
            harness.Step();
            Assert.IsTrue(pivotRequested, "Setup failed: no pivot was requested.");

            harness.Motor.GroundHeight = null;
            harness.Step();

            Assert.IsTrue(cancelled);
        }

        static LocomotionControllerHarness WithJump() =>
            new(new FakeLocomotionSettings { Jumps = new IJumpProfile[] { new FakeJumpProfile(JumpId, Any) } });
    }
}
