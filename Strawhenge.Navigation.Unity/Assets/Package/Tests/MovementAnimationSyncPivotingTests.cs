using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Strawhenge.Navigation.Unity.Tests
{
    // The Animator plays a pivot while Pivoting is set, through TestAnimatorController.CreateWithPivotAndLandingStates:
    // its pivot state is entered while Pivoting is set and exits as soon as it's cleared, as a consumer's should.
    // A pivot that's no longer wanted used to play its clip to the end, and a Pivot trigger that wasn't reset could
    // start one late.
    public class MovementAnimationSyncPivotingTests
    {
        // Longer than the pivot takes to play, and than MovementAnimationSyncScript's start timeout.
        const float WaitTimeout = 1f;

        readonly List<Object> _created = new();

        Animator _animator;
        FakeMovementSource _source;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var character = new GameObject("Character");
            character.SetActive(false);
            _created.Add(character);

            _animator = character.AddComponent<Animator>();
            _animator.runtimeAnimatorController = TestAnimatorController.CreateWithPivotAndLandingStates();
            _created.Add(_animator.runtimeAnimatorController);

            var sourceScript = character.AddComponent<FakeMovementSourceScript>();
            _source = sourceScript.Source;
            _source.IsActive = true;

            var manager = character.AddComponent<MovementSourceManagerScript>();
            manager.SetMovementSources(sourceScript);

            var sync = character.AddComponent<MovementAnimationSyncScript>();
            sync.SetMovementSourceManager(manager);

            character.SetActive(true);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var obj in _created)
            {
                if (obj != null)
                    Object.Destroy(obj);
            }

            _created.Clear();

            // Destroy is deferred to the end of the frame; wait so the next test starts clean.
            yield return null;
        }

        // Baseline for the tests below.
        [UnityTest]
        public IEnumerator Requested_pivot_plays_until_it_ends()
        {
            _source.RequestPivot(TestAnimatorController.PivotId);
            yield return WaitUntil(IsPlayingPivot);
            Assert.IsTrue(IsPlayingPivot(), "The pivot should play once requested.");

            yield return WaitUntil(() => _source.CompletedPivots > 0);

            Assert.AreEqual(1, _source.CompletedPivots, "The pivot should complete once it ends.");
        }

        [UnityTest]
        public IEnumerator Cancelled_pivot_stops_playing_straight_away()
        {
            _source.RequestPivot(TestAnimatorController.PivotId);
            yield return WaitUntil(IsPlayingPivot);
            Assume(IsPlayingPivot(), "the pivot should be playing");

            _source.CancelPivot();
            yield return null;
            yield return null;

            Assert.IsFalse(IsPlayingPivot(), "A cancelled pivot should stop rather than play its clip to the end.");
            Assert.AreEqual(0, _source.CompletedPivots, "A cancelled pivot shouldn't complete.");
        }

        [UnityTest]
        public IEnumerator Pivot_cancelled_before_it_starts_never_plays()
        {
            _source.RequestPivot(TestAnimatorController.PivotId);
            _source.CancelPivot();

            var played = false;

            for (var time = 0f; time < WaitTimeout; time += Time.deltaTime)
            {
                played |= IsPlayingPivot();
                yield return null;
            }

            Assert.IsFalse(played, "A pivot cancelled before it started shouldn't play.");
            Assert.AreEqual(0, _source.CompletedPivots, "A cancelled pivot shouldn't complete.");
        }

        static void Assume(bool condition, string failure) =>
            Assert.IsTrue(condition, $"Setup failed: {failure}.");

        bool IsPlayingPivot() => _animator.GetCurrentAnimatorStateInfo(0).IsTag(TestAnimatorController.PivotTag);

        static IEnumerator WaitUntil(Func<bool> condition)
        {
            for (var time = 0f; time < WaitTimeout && !condition(); time += Time.deltaTime)
                yield return null;
        }
    }
}
