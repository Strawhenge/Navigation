using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Strawhenge.Navigation.Unity.Tests
{
    // Turning the Animator off and on (e.g. a ragdoll), or deactivating its GameObject (e.g. pooling), makes Unity
    // reset its parameters and state and create new instances of its StateMachineBehaviours. Pivots and landings
    // must keep playing afterwards, and nothing in progress may be left waiting for an animation that will never
    // finish. Unlike MovementAnimationSyncTests, the Animator plays the pivot and landing itself here, through
    // TestAnimatorController.CreateWithPivotAndLandingStates, so Unity's own instances are used.
    public class MovementAnimationSyncAnimatorResetTests
    {
        // Longer than the pivot or landing takes to play, and than MovementAnimationSyncScript's start timeout.
        const float WaitTimeout = 1f;

        readonly List<Object> _created = new();
        readonly List<string> _warnings = new();

        GameObject _character;
        Animator _animator;
        FakeMovementSource _source;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Application.logMessageReceived += OnLogMessageReceived;

            _character = new GameObject("Character");
            _character.SetActive(false);
            _created.Add(_character);

            _animator = _character.AddComponent<Animator>();
            _animator.runtimeAnimatorController = TestAnimatorController.CreateWithPivotAndLandingStates();
            _created.Add(_animator.runtimeAnimatorController);

            var sourceScript = _character.AddComponent<FakeMovementSourceScript>();
            _source = sourceScript.Source;
            _source.IsActive = true;

            var manager = _character.AddComponent<MovementSourceManagerScript>();
            manager.SetMovementSources(sourceScript);

            var sync = _character.AddComponent<MovementAnimationSyncScript>();
            sync.SetMovementSourceManager(manager);

            _character.SetActive(true);

            Assume(
                _animator.GetBehaviour<PivotStateMachineBehaviour>() != null &&
                _animator.GetBehaviour<LandingStateMachineBehaviour>() != null,
                "the controller should have the pivot and landing behaviours");

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Application.logMessageReceived -= OnLogMessageReceived;

            foreach (var obj in _created)
            {
                if (obj != null)
                    Object.Destroy(obj);
            }

            _created.Clear();
            _warnings.Clear();

            // Destroy is deferred to the end of the frame; wait so the next test starts clean.
            yield return null;
        }

        // Baseline for the tests below.
        [UnityTest]
        public IEnumerator Pivot_plays_and_completes_through_the_Animator()
        {
            yield return PlayPivot();
        }

        // Baseline for the tests below.
        [UnityTest]
        public IEnumerator Landing_plays_and_completes_through_the_Animator()
        {
            yield return PlayLanding();
        }

        [UnityTest]
        public IEnumerator Pivot_plays_after_the_Animator_is_turned_off_and_on()
        {
            yield return TurnAnimatorOffAndOn();
            yield return PlayPivot();
        }

        [UnityTest]
        public IEnumerator Landing_plays_after_the_Animator_is_turned_off_and_on()
        {
            yield return TurnAnimatorOffAndOn();
            yield return PlayLanding();
        }

        [UnityTest]
        public IEnumerator Pivot_plays_after_the_GameObject_is_deactivated_and_activated()
        {
            _character.SetActive(false);
            yield return null;
            _character.SetActive(true);
            yield return null;

            yield return PlayPivot();
        }

        [UnityTest]
        public IEnumerator Playing_pivot_completes_when_the_Animator_is_turned_off()
        {
            _source.RequestPivot(TestAnimatorController.PivotId);
            yield return WaitUntilPlaying(TestAnimatorController.PivotTag);
            Assume(_source.CompletedPivots == 0, "the pivot shouldn't have completed yet");

            _animator.enabled = false;
            yield return null;

            Assert.AreEqual(1, _source.CompletedPivots, "The pivot should complete once the Animator is off.");
            Assert.IsFalse(_animator.applyRootMotion, "Root motion should be off once the pivot is dropped.");
        }

        [UnityTest]
        public IEnumerator Playing_landing_completes_when_the_Animator_is_turned_off()
        {
            _source.RequestLanding(TestAnimatorController.LandingId);
            yield return WaitUntilPlaying(TestAnimatorController.LandingTag);
            Assume(_source.CompletedLandings == 0, "the landing shouldn't have completed yet");

            _animator.enabled = false;
            yield return null;

            Assert.AreEqual(1, _source.CompletedLandings, "The landing should complete once the Animator is off.");
        }

        [UnityTest]
        public IEnumerator Playing_pivot_completes_when_the_GameObject_is_deactivated()
        {
            _source.RequestPivot(TestAnimatorController.PivotId);
            yield return WaitUntilPlaying(TestAnimatorController.PivotTag);
            Assume(_source.CompletedPivots == 0, "the pivot shouldn't have completed yet");

            _character.SetActive(false);

            Assert.AreEqual(1, _source.CompletedPivots, "The pivot should complete once the GameObject is inactive.");
            Assert.IsFalse(_animator.applyRootMotion, "Root motion should be off once the pivot is dropped.");
        }

        [Test]
        public void Pivot_requested_while_the_Animator_is_off_completes_straight_away()
        {
            _animator.enabled = false;

            _source.RequestPivot(TestAnimatorController.PivotId);

            Assert.AreEqual(1, _source.CompletedPivots, "Nothing can play the pivot, so it should complete straight away.");
            Assert.IsFalse(_animator.applyRootMotion, "Root motion shouldn't be turned on for a pivot that can't play.");
        }

        [Test]
        public void Landing_requested_while_the_Animator_is_off_completes_straight_away()
        {
            _animator.enabled = false;

            _source.RequestLanding(TestAnimatorController.LandingId);

            Assert.AreEqual(1, _source.CompletedLandings, "Nothing can play the landing, so it should complete straight away.");
        }

        [Test]
        public void Deferred_jump_requested_while_the_Animator_is_off_triggers_straight_away()
        {
            _animator.enabled = false;

            _source.RequestJumpTrigger(1);

            Assert.AreEqual(1, _source.TriggeredJumps, "Nothing can launch the jump, so it should trigger straight away.");
        }

        [UnityTest]
        public IEnumerator Falling_is_set_again_after_the_Animator_is_turned_off_and_on()
        {
            _source.BeginFall();
            Assume(_animator.GetBool(AnimatorParameters.Falling.Id), "Falling should be set while falling");

            yield return TurnAnimatorOffAndOn();

            Assert.IsTrue(_animator.GetBool(AnimatorParameters.Falling.Id), "Falling should be set again.");
        }

        [UnityTest]
        public IEnumerator Jumping_is_set_again_after_the_Animator_is_turned_off_and_on()
        {
            _source.BeginJump();
            Assume(_animator.GetBool(AnimatorParameters.Jumping.Id), "Jumping should be set while jumping");

            yield return TurnAnimatorOffAndOn();

            Assert.IsTrue(_animator.GetBool(AnimatorParameters.Jumping.Id), "Jumping should be set again.");
        }

        static void Assume(bool condition, string failure) =>
            Assert.IsTrue(condition, $"Setup failed: {failure}.");

        // Requests the pivot and checks the Animator played it and that its end completed it. A pivot that
        // doesn't start completes too, after MovementAnimationSyncScript's start timeout, but with a warning.
        IEnumerator PlayPivot()
        {
            _source.RequestPivot(TestAnimatorController.PivotId);
            yield return WaitUntilPlaying(TestAnimatorController.PivotTag);
            yield return WaitUntil(() => _source.CompletedPivots > 0);

            Assert.AreEqual(1, _source.CompletedPivots, "The pivot should complete once.");
            Assert.IsFalse(_animator.applyRootMotion, "Root motion should be off once the pivot ends.");
            AssertNoWarnings();
        }

        IEnumerator PlayLanding()
        {
            _source.RequestLanding(TestAnimatorController.LandingId);
            yield return WaitUntilPlaying(TestAnimatorController.LandingTag);
            yield return WaitUntil(() => _source.CompletedLandings > 0);

            Assert.AreEqual(1, _source.CompletedLandings, "The landing should complete once.");
            AssertNoWarnings();
        }

        IEnumerator TurnAnimatorOffAndOn()
        {
            _animator.enabled = false;
            yield return null;
            _animator.enabled = true;
            yield return null;
        }

        IEnumerator WaitUntilPlaying(string tag)
        {
            yield return WaitUntil(() => _animator.GetCurrentAnimatorStateInfo(0).IsTag(tag));

            Assume(_animator.GetCurrentAnimatorStateInfo(0).IsTag(tag), $"the Animator should be playing the {tag} state");
        }

        static IEnumerator WaitUntil(Func<bool> condition)
        {
            var time = 0f;

            while (!condition() && time < WaitTimeout)
            {
                time += Time.deltaTime;
                yield return null;
            }
        }

        void AssertNoWarnings() =>
            Assert.IsEmpty(_warnings, "Nothing should have been logged: " + string.Join(" | ", _warnings));

        void OnLogMessageReceived(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Warning)
                _warnings.Add(message);
        }
    }
}
