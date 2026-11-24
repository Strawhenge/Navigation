using NUnit.Framework;
using Strawhenge.Navigation.Unity.Destination;

namespace Strawhenge.Navigation.Unity.Tests
{
    // The settings caches pass the jump lists through, and rebuild them when invalidated.
    public class JumpSettingsCacheTests
    {
        static readonly ValueRange AnySpeed = new(0f, 10f);
        static readonly ValueRange AnyDirection = new(-180f, 180f);

        [Test]
        public void Locomotion_cache_keeps_jump_lists_until_invalidated()
        {
            var jump = new FakeJumpProfile(1, AnySpeed);
            var strafeJump = new FakeStrafeJumpProfile(2, AnySpeed, AnyDirection);
            var settings = new FakeLocomotionSettings
            {
                Jumps = new IJumpProfile[] { jump },
                StrafeJumps = new IStrafeJumpProfile[] { strafeJump }
            };
            var cached = new CachedLocomotionSettings(settings);
            CollectionAssert.AreEqual(new[] { jump }, cached.Jumps, "Setup failed: jumps weren't passed through.");
            CollectionAssert.AreEqual(new[] { strafeJump }, cached.StrafeJumps,
                "Setup failed: strafe jumps weren't passed through.");

            var newJump = new FakeJumpProfile(3, AnySpeed);
            var newStrafeJump = new FakeStrafeJumpProfile(4, AnySpeed, AnyDirection);
            settings.Jumps = new IJumpProfile[] { newJump };
            settings.StrafeJumps = new IStrafeJumpProfile[] { newStrafeJump };

            CollectionAssert.AreEqual(new[] { jump }, cached.Jumps, "Jumps should stay cached until invalidated.");
            CollectionAssert.AreEqual(new[] { strafeJump }, cached.StrafeJumps,
                "Strafe jumps should stay cached until invalidated.");

            CachedLocomotionSettings.Invalidate();

            CollectionAssert.AreEqual(new[] { newJump }, cached.Jumps);
            CollectionAssert.AreEqual(new[] { newStrafeJump }, cached.StrafeJumps);
        }

        [Test]
        public void Destination_cache_keeps_jump_lists_until_invalidated()
        {
            var jump = new FakeJumpProfile(1, AnySpeed);
            var strafeJump = new FakeStrafeJumpProfile(2, AnySpeed, AnyDirection);
            var settings = new FakeDestinationSettings
            {
                Jumps = new IJumpProfile[] { jump },
                StrafeJumps = new IStrafeJumpProfile[] { strafeJump }
            };
            var cached = new CachedDestinationSettings(settings);
            CollectionAssert.AreEqual(new[] { jump }, cached.Jumps, "Setup failed: jumps weren't passed through.");
            CollectionAssert.AreEqual(new[] { strafeJump }, cached.StrafeJumps,
                "Setup failed: strafe jumps weren't passed through.");

            var newJump = new FakeJumpProfile(3, AnySpeed);
            var newStrafeJump = new FakeStrafeJumpProfile(4, AnySpeed, AnyDirection);
            settings.Jumps = new IJumpProfile[] { newJump };
            settings.StrafeJumps = new IStrafeJumpProfile[] { newStrafeJump };

            CollectionAssert.AreEqual(new[] { jump }, cached.Jumps, "Jumps should stay cached until invalidated.");
            CollectionAssert.AreEqual(new[] { strafeJump }, cached.StrafeJumps,
                "Strafe jumps should stay cached until invalidated.");

            CachedLocomotionSettings.Invalidate();

            CollectionAssert.AreEqual(new[] { newJump }, cached.Jumps);
            CollectionAssert.AreEqual(new[] { newStrafeJump }, cached.StrafeJumps);
        }
    }
}
