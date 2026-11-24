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
    // the tests play its part by calling the pivot and landing behaviours themselves, with the Animator: they find
    // the MovementAnimationSyncScript beside it, as in a scene. It has no transitions either, so nothing consumes
    // a trigger: one stays set until MovementAnimationSyncScript resets it.
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
            sync.SetMovementSourceManager(_manager);

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

            Assert.AreEqual(1, _first.CompletedPivots, "The pivot should complete on the source that requested it.");
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

            Assert.AreEqual(0, _second.CompletedPivots, "The old source's pivot shouldn't complete on the new source.");
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

            Assert.AreEqual(0, _second.CompletedPivots, "The old source's pivot shouldn't complete on the new source.");
            Assert.AreEqual(0, _first.CompletedPivots, "The old source has stopped waiting for its pivot.");
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

            Assert.AreEqual(0, _first.CompletedPivots, "A cancelled pivot shouldn't complete.");
        }

        [Test]
        public void Jumping_is_cleared_when_the_source_changes()
        {
            _first.BeginJump();
            Assume(_animator.GetBool(AnimatorParameters.Jumping.Id), "Jumping should be set while jumping");

            SwitchToSecondSource();

            Assert.IsFalse(_animator.GetBool(AnimatorParameters.Jumping.Id), "The old source's jump shouldn't carry over.");
        }

        [Test]
        public void Falling_is_cleared_when_the_source_changes()
        {
            _first.BeginFall();
            Assume(_animator.GetBool(AnimatorParameters.Falling.Id), "Falling should be set while falling");

            SwitchToSecondSource();

            Assert.IsFalse(_animator.GetBool(AnimatorParameters.Falling.Id), "The old source's fall shouldn't carry over.");
        }

        [Test]
        public void Jump_trigger_is_reset_when_the_source_changes()
        {
            _first.RequestJumpTrigger(1);
            Assume(_animator.GetBool(AnimatorParameters.Jump.Id), "the Jump trigger should be set for a deferred jump");

            SwitchToSecondSource();

            Assert.IsFalse(_animator.GetBool(AnimatorParameters.Jump.Id), "The old source's jump trigger should be reset.");
        }

        [Test]
        public void Jump_id_is_set_with_the_jump_trigger()
        {
            _first.RequestJumpTrigger(5);

            Assert.AreEqual(5, _animator.GetInteger(AnimatorParameters.JumpId.Id));
            Assert.IsTrue(_animator.GetBool(AnimatorParameters.Jump.Id), "The Jump trigger should be set.");
        }

        [Test]
        public void Jump_trigger_is_reset_when_cancelled()
        {
            _first.RequestJumpTrigger(1);
            Assume(_animator.GetBool(AnimatorParameters.Jump.Id), "the Jump trigger should be set for a deferred jump");

            _first.CancelJumpTrigger();

            Assert.IsFalse(_animator.GetBool(AnimatorParameters.Jump.Id), "A cancelled jump trigger should be reset.");
        }

        [Test]
        public void Pivot_trigger_is_reset_when_the_source_changes()
        {
            _first.RequestPivot(PivotId);
            Assume(_animator.GetBool(AnimatorParameters.Pivot.Id), "the Pivot trigger should be set for the pivot");

            SwitchToSecondSource();

            Assert.IsFalse(_animator.GetBool(AnimatorParameters.Pivot.Id), "The old source's pivot trigger should be reset.");
        }

        [Test]
        public void Pivot_trigger_is_reset_when_cancelled()
        {
            _first.RequestPivot(PivotId);
            Assume(_animator.GetBool(AnimatorParameters.Pivot.Id), "the Pivot trigger should be set for the pivot");

            _first.CancelPivot();

            Assert.IsFalse(_animator.GetBool(AnimatorParameters.Pivot.Id), "A cancelled pivot's trigger should be reset.");
        }

        [Test]
        public void Land_trigger_is_reset_when_the_source_changes()
        {
            _first.RequestLanding(LandingId);
            Assume(_animator.GetBool(AnimatorParameters.Land.Id), "the Land trigger should be set for the landing");

            SwitchToSecondSource();

            Assert.IsFalse(_animator.GetBool(AnimatorParameters.Land.Id), "The old source's landing trigger should be reset.");
        }

        [Test]
        public void Land_trigger_is_reset_when_cancelled()
        {
            _first.RequestLanding(LandingId);
            Assume(_animator.GetBool(AnimatorParameters.Land.Id), "the Land trigger should be set for the landing");

            _first.CancelLanding();

            Assert.IsFalse(_animator.GetBool(AnimatorParameters.Land.Id), "A cancelled landing's trigger should be reset.");
        }

        static void Assume(bool condition, string failure) =>
            Assert.IsTrue(condition, $"Setup failed: {failure}.");

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
