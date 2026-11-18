using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Strawhenge.Navigation.Unity.Tests
{
    // When another movement source takes over, everything MovementAnimationSyncScript set on the Animator for
    // the old one must be dropped: the old source has stopped waiting for it. A pivot or landing that had
    // already started used to keep going: a pivot left root motion on, and when either ended it completed
    // the new source's pivot or landing instead.
    // The Animator plays TestAnimatorController, which has the parameters but no pivot or landing states, so
    // the tests play its part by calling the pivot and landing behaviours themselves.
    public class MovementAnimationSyncTests
    {
        const int PivotId = 1;
        const int LandingId = 2;

        readonly List<Object> _created = new();

        Animator _animator;
        FakeMovementSource _first;
        FakeMovementSource _second;
        PivotStateMachineBehaviour _pivot;
        LandingStateMachineBehaviour _landing;
        MovementSourceManagerScript _manager;
        int _activeSourceChangedCount;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var character = new GameObject("Character");
            character.SetActive(false);
            _created.Add(character);

            _animator = character.AddComponent<Animator>();
            _animator.runtimeAnimatorController = TestAnimatorController.Create();
            _created.Add(_animator.runtimeAnimatorController);

            var first = character.AddComponent<FakeMovementSourceScript>();
            var second = character.AddComponent<FakeMovementSourceScript>();
            _first = first.Source;
            _second = second.Source;
            _first.IsActive = true;

            _manager = character.AddComponent<MovementSourceManagerScript>();
            _manager.SetMovementSources(first, second);

            _pivot = ScriptableObject.CreateInstance<PivotStateMachineBehaviour>();
            _landing = ScriptableObject.CreateInstance<LandingStateMachineBehaviour>();
            _created.Add(_pivot);
            _created.Add(_landing);

            var sync = character.AddComponent<MovementAnimationSyncScript>();
            sync.SetAnimator(_animator);
            sync.SetMovementSourceManager(_manager);
            sync.SetBehaviours(_pivot, _landing);

            character.SetActive(true);
            _manager.ActiveSourceChanged += () => _activeSourceChangedCount++;

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
            _activeSourceChangedCount = 0;

            // Destroy is deferred to the end of the frame; wait so the next test starts clean.
            yield return null;
        }

        // Baseline for the tests below.
        [Test]
        public void Pivot_completes_on_the_source_that_requested_it()
        {
            _first.RequestPivot(PivotId);
            Assert.IsTrue(_animator.applyRootMotion, "Setup failed: root motion should be on for the pivot.");

            StartPivot();
            EndPivot();

            Assert.AreEqual(1, _first.CompletedPivots.Count, "The pivot should complete on the source that requested it.");
            Assert.IsFalse(_animator.applyRootMotion, "Root motion should be off once the pivot ends.");
        }

        // Baseline for the tests below.
        [Test]
        public void Landing_completes_on_the_source_that_requested_it()
        {
            _first.RequestLanding(LandingId);
            StartLanding();
            EndLanding();

            Assert.AreEqual(1, _first.CompletedLandings, "The landing should complete on the source that requested it.");
        }

        [UnityTest]
        public IEnumerator Pivot_not_started_is_dropped_when_the_source_changes()
        {
            _first.RequestPivot(PivotId);
            SwitchToSecondSource();

            Assert.IsFalse(_animator.applyRootMotion, "Root motion should be off once the source changes.");

            // Past MovementAnimationSyncScript's start timeout, which would otherwise complete it.
            yield return new WaitForSeconds(0.5f);

            Assert.IsEmpty(_second.CompletedPivots, "The old source's pivot shouldn't complete on the new source.");
        }

        [Test]
        public void Started_pivot_is_dropped_when_the_source_changes()
        {
            _first.RequestPivot(PivotId);
            StartPivot();
            SwitchToSecondSource();

            Assert.IsFalse(_animator.applyRootMotion, "Root motion should be off once the source changes.");
        }

        [Test]
        public void Started_pivot_ending_after_the_source_changed_completes_nothing()
        {
            _first.RequestPivot(PivotId);
            StartPivot();
            SwitchToSecondSource();
            EndPivot();

            Assert.IsEmpty(_second.CompletedPivots, "The old source's pivot shouldn't complete on the new source.");
            Assert.IsEmpty(_first.CompletedPivots, "The old source has stopped waiting for its pivot.");
        }

        [Test]
        public void Started_landing_ending_after_the_source_changed_completes_nothing()
        {
            _first.RequestLanding(LandingId);
            StartLanding();
            SwitchToSecondSource();
            EndLanding();

            Assert.AreEqual(0, _second.CompletedLandings, "The old source's landing shouldn't complete on the new source.");
            Assert.AreEqual(0, _first.CompletedLandings, "The old source has stopped waiting for its landing.");
        }

        // As when the destination controller abandons a pivot because the character can't navigate.
        [Test]
        public void Started_pivot_cancelled_by_its_source_is_dropped()
        {
            _first.RequestPivot(PivotId);
            StartPivot();
            _first.CancelPivot();

            Assert.IsFalse(_animator.applyRootMotion, "Root motion should be off once the pivot is cancelled.");

            EndPivot();

            Assert.IsEmpty(_first.CompletedPivots, "A cancelled pivot shouldn't complete.");
        }

        void SwitchToSecondSource()
        {
            var changesBefore = _activeSourceChangedCount;

            _second.IsActive = true;
            _first.IsActive = false;

            Assert.AreEqual(
                changesBefore + 1,
                _activeSourceChangedCount,
                "Setup failed: the second source should have taken over.");
        }

        void StartPivot() => _pivot.OnStateEnter(_animator, default, 0);

        void EndPivot() => _pivot.OnStateMachineExit(_animator, 0);

        void StartLanding() => _landing.OnStateEnter(_animator, default, 0);

        void EndLanding() => _landing.OnStateMachineExit(_animator, 0);
    }
}
