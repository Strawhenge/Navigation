using NUnit.Framework;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Tests
{
    // Activating and deactivating the controller (what LocomotionScript does when it's enabled and disabled),
    // on a FakeMotor.
    public class LocomotionControllerActivationTests
    {
        static readonly ValueRange Any = new(float.NegativeInfinity, float.PositiveInfinity);

        [Test]
        public void Deactivating_turns_the_motor_off_and_reports_it()
        {
            var harness = new LocomotionControllerHarness();
            bool? isActiveChanged = null;
            harness.Controller.IsActiveChanged += isActive => isActiveChanged = isActive;
            Assert.IsTrue(harness.Motor.IsActive, "Setup failed: the motor isn't on.");

            harness.Controller.Deactivate();

            Assert.IsFalse(harness.Controller.IsActive);
            Assert.IsFalse(harness.Motor.IsActive, "The motor should be off.");
            Assert.AreEqual(false, isActiveChanged, "IsActiveChanged(false) should have been raised.");
        }

        [Test]
        public void Activating_turns_the_motor_on_and_reports_it()
        {
            var harness = new LocomotionControllerHarness();
            harness.Controller.Deactivate();
            bool? isActiveChanged = null;
            harness.Controller.IsActiveChanged += isActive => isActiveChanged = isActive;

            harness.Controller.Activate();

            Assert.IsTrue(harness.Controller.IsActive);
            Assert.IsTrue(harness.Motor.IsActive, "The motor should be on.");
            Assert.AreEqual(true, isActiveChanged, "IsActiveChanged(true) should have been raised.");
        }

        // Nothing to turn to before the first input: the character keeps facing the way it did when activated.
        [Test]
        public void Keeps_its_facing_without_input()
        {
            var harness = new LocomotionControllerHarness();

            harness.StepFor(0.5f);

            Assert.Less(Quaternion.Angle(harness.Motor.Rotation, Quaternion.identity), 0.01f,
                "The character shouldn't turn without input.");
        }

        // While inactive something else (Destination) can turn the character; activating again mustn't turn it
        // back to the input it had before.
        [Test]
        public void Keeps_the_facing_it_was_given_while_inactive()
        {
            var harness = new LocomotionControllerHarness();
            harness.Controller.Move(Vector3.forward);
            harness.Step();
            harness.Controller.Move(Vector3.zero);
            harness.Controller.Deactivate();
            harness.Motor.Rotation = Quaternion.LookRotation(Vector3.right);

            harness.Controller.Activate();
            harness.StepFor(0.5f);

            Assert.Less(Quaternion.Angle(harness.Motor.Rotation, Quaternion.LookRotation(Vector3.right)), 0.01f,
                "The character should keep the facing it was given while inactive.");
        }

        [Test]
        public void Does_not_move_the_character_while_inactive()
        {
            var harness = new LocomotionControllerHarness();
            harness.Controller.Deactivate();

            harness.Controller.Move(Vector3.forward);
            harness.StepFor(0.5f);

            Assert.AreEqual(0, harness.Motor.Moves.Count, "An inactive controller shouldn't move the motor.");
        }

        // While inactive, the Animator's completion calls go to another movement source, so a pivot waiting for
        // one would block the character forever.
        [Test]
        public void Deactivating_gives_up_a_pivot_waiting_for_the_animator()
        {
            var harness = new LocomotionControllerHarness(new FakeLocomotionSettings
            {
                MovingPivots = new IPivotProfile[]
                {
                    new FakePivotProfile(3, Any, new ValueRange(150f, 180f), new ValueRange(-180f, -150f))
                }
            });
            var pivotRequested = false;
            harness.Controller.PivotRequested += _ => pivotRequested = true;
            harness.Controller.Move(Vector3.forward);
            harness.StepFor(1f);
            harness.Controller.Move(Vector3.back);
            harness.Step();
            Assert.IsTrue(pivotRequested, "Setup failed: no pivot was requested.");

            harness.Controller.Deactivate();
            harness.Controller.Activate();
            var moves = harness.Motor.Moves.Count;
            harness.Controller.Move(Vector3.forward); // Ahead, so the pivot doesn't match again.
            harness.Step();

            Assert.Greater(harness.Motor.Moves.Count, moves, "The character should move again without the pivot.");
        }

        [Test]
        public void Deactivating_cancels_a_deferred_jump_waiting_for_its_trigger()
        {
            var harness = new LocomotionControllerHarness(new FakeLocomotionSettings { DeferJumpTrigger = true });
            var jumpBegan = false;
            harness.Controller.JumpBegan += () => jumpBegan = true;
            harness.Controller.Jump();

            harness.Controller.Deactivate();
            harness.Controller.Activate();
            harness.Controller.TriggerJump();
            harness.Step();

            Assert.IsFalse(jumpBegan, "A trigger after reactivating shouldn't launch the cancelled jump.");
        }
    }
}
