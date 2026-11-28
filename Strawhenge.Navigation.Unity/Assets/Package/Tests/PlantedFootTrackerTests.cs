using NUnit.Framework;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Tests
{
    // MovementAnimationSyncScript sets 'Pivot Right Foot Planted' from this, so a pivot plays the clip for the foot
    // that's planted. It used to pick the foot further forward, which is the wrong one once that foot has landed
    // and the other is swinging past it.
    public class PlantedFootTrackerTests
    {
        const float Margin = 0.1f;
        const float DeltaTime = 0.02f;

        PlantedFootTracker _tracker;
        Vector3 _leftFoot;
        Vector3 _rightFoot;

        [SetUp]
        public void SetUp()
        {
            _tracker = new PlantedFootTracker(Margin);
            _leftFoot = new Vector3(-0.1f, 0f, 0f);
            _rightFoot = new Vector3(0.1f, 0f, 0f);
            _tracker.Update(_leftFoot, _rightFoot, DeltaTime);
        }

        [Test]
        public void Right_foot_is_planted_while_the_left_swings_past_it()
        {
            Step(leftSpeed: 3f, rightSpeed: 0f);

            Assert.IsTrue(_tracker.IsRightFootPlanted);
        }

        [Test]
        public void Left_foot_is_planted_while_the_right_swings_past_it()
        {
            Step(leftSpeed: 0f, rightSpeed: 3f);
            Assume(rightFootPlanted: false);
            Step(leftSpeed: 3f, rightSpeed: 0f);
            Assume(rightFootPlanted: true);

            Step(leftSpeed: 0f, rightSpeed: 3f);

            Assert.IsFalse(_tracker.IsRightFootPlanted);
        }

        // Both feet move while running: the planted one is the slower.
        [Test]
        public void Slower_foot_is_planted_when_both_move()
        {
            Step(leftSpeed: 6f, rightSpeed: 4.5f);

            Assert.IsTrue(_tracker.IsRightFootPlanted);
        }

        [Test]
        public void Planted_foot_stays_planted_while_both_feet_are_still()
        {
            Step(leftSpeed: 3f, rightSpeed: 0f);
            Assume(rightFootPlanted: true);

            for (var i = 0; i < 10; i++)
                Step(leftSpeed: 0f, rightSpeed: 0f);

            Assert.IsTrue(_tracker.IsRightFootPlanted, "Standing still should keep the foot that last landed.");
        }

        [Test]
        public void Planted_foot_stays_planted_while_the_speeds_are_within_the_margin()
        {
            Step(leftSpeed: 3f, rightSpeed: 0f);
            Assume(rightFootPlanted: true);

            Step(leftSpeed: 1f, rightSpeed: 1f + Margin * 0.5f);

            Assert.IsTrue(_tracker.IsRightFootPlanted, "Feet moving at nearly the same speed shouldn't change it.");
        }

        // Baseline for the test above.
        [Test]
        public void Planted_foot_changes_when_the_other_is_slower_by_more_than_the_margin()
        {
            Step(leftSpeed: 3f, rightSpeed: 0f);
            Assume(rightFootPlanted: true);

            Step(leftSpeed: 1f, rightSpeed: 1f + Margin * 2f);

            Assert.IsFalse(_tracker.IsRightFootPlanted);
        }

        // Over the ground: a foot lifting straight up isn't swinging past the other.
        [Test]
        public void Vertical_foot_movement_is_ignored()
        {
            Step(leftSpeed: 3f, rightSpeed: 0f);
            Assume(rightFootPlanted: true);

            _leftFoot += Vector3.up * (3f * DeltaTime);
            _rightFoot += Vector3.forward * (0.5f * DeltaTime);
            _tracker.Update(_leftFoot, _rightFoot, DeltaTime);

            Assert.IsFalse(_tracker.IsRightFootPlanted,
                "The left foot only lifted, while the right moved over the ground, so the left should be planted.");
        }

        [Test]
        public void Update_after_reset_only_records_where_the_feet_are()
        {
            Step(leftSpeed: 0f, rightSpeed: 3f);
            Assume(rightFootPlanted: false);

            _tracker.Reset();
            _leftFoot += Vector3.forward * 5f;
            _tracker.Update(_leftFoot, _rightFoot, DeltaTime);

            Assert.IsFalse(_tracker.IsRightFootPlanted,
                "A jump in the feet's positions while nothing was tracked shouldn't count as movement.");
        }

        [Test]
        public void Update_without_time_passing_changes_nothing()
        {
            Step(leftSpeed: 0f, rightSpeed: 3f);
            Assume(rightFootPlanted: false);

            _leftFoot += Vector3.forward;
            _tracker.Update(_leftFoot, _rightFoot, 0f);

            Assert.IsFalse(_tracker.IsRightFootPlanted);
        }

        // Moves each foot forward at its speed for one update.
        void Step(float leftSpeed, float rightSpeed)
        {
            _leftFoot += Vector3.forward * (leftSpeed * DeltaTime);
            _rightFoot += Vector3.forward * (rightSpeed * DeltaTime);
            _tracker.Update(_leftFoot, _rightFoot, DeltaTime);
        }

        void Assume(bool rightFootPlanted) =>
            Assert.AreEqual(rightFootPlanted, _tracker.IsRightFootPlanted,
                $"Setup failed: the {(rightFootPlanted ? "right" : "left")} foot should be planted.");
    }
}
