using NUnit.Framework;

namespace Strawhenge.Navigation.Unity.Tests
{
    // What the locomotion movement source passes on from its LocomotionController.
    public class LocomotionMovementSourceTests
    {
        const int JumpId = 3;

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

        static LocomotionControllerHarness WithJump() =>
            new(new FakeLocomotionSettings { Jumps = new IJumpProfile[] { new FakeJumpProfile(JumpId, Any) } });
    }
}
