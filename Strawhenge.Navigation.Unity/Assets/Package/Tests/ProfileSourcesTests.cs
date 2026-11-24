using NUnit.Framework;

namespace Strawhenge.Navigation.Unity.Tests
{
    // Which profiles the settings caches keep, and the warnings for those they ignore.
    public class ProfileSourcesTests
    {
        static readonly ValueRange AnySpeed = new(0f, 10f);
        static readonly ValueRange AnyDirection = new(-180f, 180f);

        [Test]
        public void Usable_profiles_are_kept_in_order()
        {
            var logger = new FakeLogger();
            var first = new FakeJumpProfile(1, AnySpeed);
            var second = new FakeJumpProfile(2, AnySpeed);

            var profiles = ProfileSources.Filter(
                new IJumpProfile[] { first, second }, JumpProfileExtensions.FindProblem, "Jumps", logger);

            CollectionAssert.AreEqual(new IJumpProfile[] { first, second }, profiles);
            CollectionAssert.IsEmpty(logger.Warnings);
        }

        [Test]
        public void Unset_profiles_are_skipped_without_a_warning()
        {
            var logger = new FakeLogger();
            var jump = new FakeJumpProfile(1, AnySpeed);

            var profiles = ProfileSources.Filter(
                new IJumpProfile[] { null, jump, null }, JumpProfileExtensions.FindProblem, "Jumps", logger);

            CollectionAssert.AreEqual(new IJumpProfile[] { jump }, profiles);
            CollectionAssert.IsEmpty(logger.Warnings);
        }

        [Test]
        public void Jump_without_a_jump_asset_is_skipped_with_a_warning()
        {
            var logger = new FakeLogger();
            var jump = new FakeJumpProfile(1, AnySpeed);

            var profiles = ProfileSources.Filter(
                new IJumpProfile[] { new FakeJumpProfile(null, AnySpeed), jump },
                JumpProfileExtensions.FindProblem,
                "Jumps",
                logger);

            CollectionAssert.AreEqual(new IJumpProfile[] { jump }, profiles);
            AssertSingleWarning(logger, "Jumps[0] has no jump asset assigned");
        }

        [Test]
        public void Strafe_jump_with_no_direction_ranges_is_skipped_with_a_warning()
        {
            var logger = new FakeLogger();
            var jump = new FakeStrafeJumpProfile(1, AnySpeed, AnyDirection);

            var profiles = ProfileSources.Filter(
                new IStrafeJumpProfile[] { jump, new FakeStrafeJumpProfile(2, AnySpeed) },
                StrafeJumpProfileExtensions.FindProblem,
                "StrafeJumps",
                logger);

            CollectionAssert.AreEqual(new IStrafeJumpProfile[] { jump }, profiles);
            AssertSingleWarning(logger, "StrafeJumps[1] has no direction ranges");
        }

        [Test]
        public void Pivot_without_a_pivot_asset_is_skipped_with_a_warning()
        {
            var logger = new FakeLogger();

            var profiles = ProfileSources.Filter(
                new IPivotProfile[] { new FakePivotProfile(null, AnySpeed) },
                PivotProfileExtensions.FindProblem,
                "MovingPivots",
                logger);

            CollectionAssert.IsEmpty(profiles);
            AssertSingleWarning(logger, "MovingPivots[0] has no pivot asset assigned");
        }

        static void AssertSingleWarning(FakeLogger logger, string expected)
        {
            Assert.AreEqual(1, logger.Warnings.Count, "Expected exactly one warning.");
            StringAssert.Contains(expected, logger.Warnings[0]);
        }
    }
}
