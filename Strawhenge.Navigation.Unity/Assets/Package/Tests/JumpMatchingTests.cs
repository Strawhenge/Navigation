using NUnit.Framework;
using UnityEngine;

namespace Strawhenge.Navigation.Unity.Tests
{
    // Picking the jump when a jump starts, shared by locomotion and destination. Destination jumps follow
    // baked off-mesh links, which tests can't build, so this is where its matching is covered.
    public class JumpMatchingTests
    {
        const int JumpId = 1;
        const int OtherJumpId = 2;
        const float Tolerance = 0.01f;

        static readonly ValueRange AnySpeed = new(0f, 10f);
        static readonly ValueRange Standing = new(0f, 0.5f);
        static readonly ValueRange AnyDirection = new(-180f, 180f);
        static readonly ValueRange Right = new(45f, 135f);

        static readonly IJumpProfile[] NoJumps = new IJumpProfile[0];
        static readonly IStrafeJumpProfile[] NoStrafeJumps = new IStrafeJumpProfile[0];

        [Test]
        public void Not_strafing_matches_jumps()
        {
            var jumps = new IJumpProfile[] { new FakeJumpProfile(JumpId, AnySpeed) };

            var matched = JumpMatching.TryMatch(jumps, NoStrafeJumps, false, speed: 3f, direction: 0f, out var jumpId);

            Assert.IsTrue(matched);
            Assert.AreEqual(JumpId, jumpId);
        }

        [Test]
        public void Strafing_matches_strafe_jumps_only()
        {
            var jumps = new IJumpProfile[] { new FakeJumpProfile(OtherJumpId, AnySpeed) };
            var strafeJumps = new IStrafeJumpProfile[] { new FakeStrafeJumpProfile(JumpId, AnySpeed, Right) };

            var matched = JumpMatching.TryMatch(jumps, strafeJumps, true, speed: 3f, direction: 90f, out var jumpId);

            Assert.IsTrue(matched);
            Assert.AreEqual(JumpId, jumpId);
        }

        [Test]
        public void Strafing_with_no_matching_strafe_jump_matches_nothing()
        {
            var jumps = new IJumpProfile[] { new FakeJumpProfile(OtherJumpId, AnySpeed) };
            var strafeJumps = new IStrafeJumpProfile[] { new FakeStrafeJumpProfile(JumpId, AnySpeed, Right) };

            Assert.IsFalse(JumpMatching.TryMatch(jumps, strafeJumps, true, speed: 3f, direction: -90f, out _));
        }

        [Test]
        public void No_profiles_match_nothing()
        {
            Assert.IsFalse(JumpMatching.TryMatch(NoJumps, NoStrafeJumps, false, speed: 0f, direction: 0f, out _));
        }

        [Test]
        public void First_match_wins()
        {
            var jumps = new IJumpProfile[]
            {
                new FakeJumpProfile(JumpId, AnySpeed),
                new FakeJumpProfile(OtherJumpId, AnySpeed)
            };

            JumpMatching.TryMatch(jumps, NoStrafeJumps, false, speed: 3f, direction: 0f, out var jumpId);

            Assert.AreEqual(JumpId, jumpId);
        }

        [Test]
        public void Profile_without_an_id_is_skipped()
        {
            var jumps = new IJumpProfile[]
            {
                new FakeJumpProfile(null, AnySpeed),
                new FakeJumpProfile(JumpId, AnySpeed)
            };

            JumpMatching.TryMatch(jumps, NoStrafeJumps, false, speed: 3f, direction: 0f, out var jumpId);

            Assert.AreEqual(JumpId, jumpId);
        }

        [TestCase(0f, 1f, 0f)]
        [TestCase(1f, 0f, 90f)]
        [TestCase(-1f, 0f, -90f)]
        public void Direction_is_relative_to_the_facing(float x, float z, float expected)
        {
            var direction = JumpMatching.GetDirection(Quaternion.identity, new Vector3(x, 0f, z) * 3f);

            Assert.AreEqual(expected, direction, Tolerance);
        }

        [Test]
        public void Moving_back_is_180_degrees_either_way()
        {
            var direction = JumpMatching.GetDirection(Quaternion.identity, Vector3.back * 3f);

            Assert.AreEqual(180f, Mathf.Abs(direction), Tolerance);
        }

        [Test]
        public void Direction_follows_a_turned_facing()
        {
            var facingRight = Quaternion.LookRotation(Vector3.right);

            Assert.AreEqual(-90f, JumpMatching.GetDirection(facingRight, Vector3.forward * 3f), Tolerance);
        }

        [Test]
        public void Direction_ignores_vertical_movement()
        {
            Assert.AreEqual(90f, JumpMatching.GetDirection(Quaternion.identity, new Vector3(3f, 5f, 0f)), Tolerance);
        }

        [Test]
        public void Too_little_movement_counts_as_forward()
        {
            Assert.AreEqual(0f, JumpMatching.GetDirection(Quaternion.identity, Vector3.left * 0.05f), Tolerance);
        }

        [Test]
        public void Standing_strafe_jump_with_a_full_direction_range_matches()
        {
            var strafeJumps = new IStrafeJumpProfile[] { new FakeStrafeJumpProfile(JumpId, Standing, AnyDirection) };
            var direction = JumpMatching.GetDirection(Quaternion.identity, Vector3.zero);

            Assert.IsTrue(JumpMatching.TryMatch(NoJumps, strafeJumps, true, speed: 0f, direction, out _));
        }
    }
}
