using NUnit.Framework;

namespace Strawhenge.Navigation.Unity.Tests
{
    // Matching jump and strafe jump profiles, and what makes them unusable.
    public class JumpProfileTests
    {
        static readonly ValueRange Running = new(3f, 6f);
        static readonly ValueRange Standing = new(0f, 0.5f);
        static readonly ValueRange Left = new(-135f, -45f);
        static readonly ValueRange Right = new(45f, 135f);
        static readonly ValueRange AnyDirection = new(-180f, 180f);

        [TestCase(3f)]
        [TestCase(4.5f)]
        [TestCase(6f)]
        public void Jump_matches_inside_its_speed_range(float speed)
        {
            var jump = new FakeJumpProfile(1, Running);

            Assert.IsTrue(jump.Matches(speed));
        }

        [TestCase(0f)]
        [TestCase(2.9f)]
        [TestCase(6.1f)]
        public void Jump_does_not_match_outside_its_speed_range(float speed)
        {
            var jump = new FakeJumpProfile(1, Running);

            Assert.IsFalse(jump.Matches(speed));
        }

        [Test]
        public void Strafe_jump_matches_inside_its_speed_and_direction_ranges()
        {
            var jump = new FakeStrafeJumpProfile(1, Running, Left);

            Assert.IsTrue(jump.Matches(speed: 4f, direction: -90f));
        }

        [Test]
        public void Strafe_jump_does_not_match_outside_its_direction_ranges()
        {
            var jump = new FakeStrafeJumpProfile(1, Running, Left);
            Assert.IsTrue(jump.Matches(speed: 4f, direction: -90f), "Setup failed: the profile doesn't match at all.");

            Assert.IsFalse(jump.Matches(speed: 4f, direction: 90f));
        }

        [Test]
        public void Strafe_jump_does_not_match_outside_its_speed_range()
        {
            var jump = new FakeStrafeJumpProfile(1, Running, Left);
            Assert.IsTrue(jump.Matches(speed: 4f, direction: -90f), "Setup failed: the profile doesn't match at all.");

            Assert.IsFalse(jump.Matches(speed: 1f, direction: -90f));
        }

        [TestCase(-90f)]
        [TestCase(90f)]
        public void Strafe_jump_matches_any_of_its_direction_ranges(float direction)
        {
            var jump = new FakeStrafeJumpProfile(1, Running, Left, Right);

            Assert.IsTrue(jump.Matches(speed: 4f, direction));
        }

        [TestCase(-180f)]
        [TestCase(0f)]
        [TestCase(180f)]
        public void Standing_strafe_jump_with_a_full_direction_range_matches_any_direction(float direction)
        {
            var jump = new FakeStrafeJumpProfile(1, Standing, AnyDirection);

            Assert.IsTrue(jump.Matches(speed: 0f, direction));
        }

        [Test]
        public void Strafe_jump_with_no_direction_ranges_never_matches()
        {
            var jump = new FakeStrafeJumpProfile(1, Standing);

            Assert.IsFalse(jump.Matches(speed: 0f, direction: 0f));
        }

        [Test]
        public void Complete_jump_has_no_problem()
        {
            Assert.IsNull(new FakeJumpProfile(1, Running).FindProblem());
        }

        [Test]
        public void Jump_without_a_jump_asset_has_a_problem()
        {
            Assert.IsNotNull(new FakeJumpProfile(null, Running).FindProblem());
        }

        [Test]
        public void Complete_strafe_jump_has_no_problem()
        {
            Assert.IsNull(new FakeStrafeJumpProfile(1, Running, Left).FindProblem());
        }

        [Test]
        public void Strafe_jump_without_a_jump_asset_has_a_problem()
        {
            Assert.IsNotNull(new FakeStrafeJumpProfile(null, Running, Left).FindProblem());
        }

        [Test]
        public void Strafe_jump_with_no_direction_ranges_has_a_problem()
        {
            Assert.IsNotNull(new FakeStrafeJumpProfile(1, Running).FindProblem());
        }
    }
}
