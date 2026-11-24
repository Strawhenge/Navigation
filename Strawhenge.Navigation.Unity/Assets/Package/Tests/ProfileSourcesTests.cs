using NUnit.Framework;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

namespace Strawhenge.Navigation.Unity.Tests
{
    // Which resolved profiles settings keep, and the warnings for those they ignore.
    public class ProfileSourcesTests
    {
        static readonly ValueRange AnySpeed = new(0f, 10f);
        static readonly ValueRange AnyDirection = new(-180f, 180f);

        [Test]
        public void Usable_profiles_are_kept_in_order()
        {
            var first = new FakeJumpProfile(1, AnySpeed);
            var second = new FakeJumpProfile(2, AnySpeed);

            var profiles = ProfileSources.Filter(
                new IJumpProfile[] { first, second }, JumpProfileExtensions.FindProblem, "_jumps", null);

            CollectionAssert.AreEqual(new IJumpProfile[] { first, second }, profiles);
        }

        [Test]
        public void Unset_profiles_are_skipped()
        {
            var jump = new FakeJumpProfile(1, AnySpeed);

            var profiles = ProfileSources.Filter(
                new IJumpProfile[] { null, jump, null }, JumpProfileExtensions.FindProblem, "_jumps", null);

            CollectionAssert.AreEqual(new IJumpProfile[] { jump }, profiles);
        }

        [Test]
        public void Jump_without_a_jump_asset_is_skipped_with_a_warning()
        {
            var jump = new FakeJumpProfile(1, AnySpeed);
            LogAssert.Expect(LogType.Warning, new Regex(@"_jumps\[0\] has no jump asset assigned"));

            var profiles = ProfileSources.Filter(
                new IJumpProfile[] { new FakeJumpProfile(null, AnySpeed), jump },
                JumpProfileExtensions.FindProblem,
                "_jumps",
                null);

            CollectionAssert.AreEqual(new IJumpProfile[] { jump }, profiles);
        }

        [Test]
        public void Strafe_jump_with_no_direction_ranges_is_skipped_with_a_warning()
        {
            var jump = new FakeStrafeJumpProfile(1, AnySpeed, AnyDirection);
            LogAssert.Expect(LogType.Warning, new Regex(@"_strafeJumps\[1\] has no direction ranges"));

            var profiles = ProfileSources.Filter(
                new IStrafeJumpProfile[] { jump, new FakeStrafeJumpProfile(2, AnySpeed) },
                StrafeJumpProfileExtensions.FindProblem,
                "_strafeJumps",
                null);

            CollectionAssert.AreEqual(new IStrafeJumpProfile[] { jump }, profiles);
        }

        [Test]
        public void Pivot_without_a_pivot_asset_is_skipped_with_a_warning()
        {
            LogAssert.Expect(LogType.Warning, new Regex(@"_movingPivots\[0\] has no pivot asset assigned"));

            var profiles = ProfileSources.Filter(
                new IPivotProfile[] { new FakePivotProfile(null, AnySpeed) },
                PivotProfileExtensions.FindProblem,
                "_movingPivots",
                null);

            CollectionAssert.IsEmpty(profiles);
        }
    }
}
