using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Strawhenge.Navigation.Unity.Tests
{
    // A state that goes back to Locomotion, such as a moving pivot, says where Locomotion's walk and run cycle picks
    // up through LocomotionCycleOffsetStateMachineBehaviour, so the legs carry on from where its clip leaves them.
    // The Animator plays TestAnimatorController's one state, and the tests call the behaviour themselves.
    public class LocomotionCycleOffsetTests
    {
        readonly List<Object> _created = new();

        Animator _animator;
        LocomotionCycleOffsetStateMachineBehaviour _behaviour;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var character = new GameObject("Character");
            _created.Add(character);

            _animator = character.AddComponent<Animator>();
            _animator.runtimeAnimatorController = TestAnimatorController.Create();
            _created.Add(_animator.runtimeAnimatorController);

            _behaviour = ScriptableObject.CreateInstance<LocomotionCycleOffsetStateMachineBehaviour>();
            _created.Add(_behaviour);

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

        [Test]
        public void Cycle_offset_is_set_while_the_state_plays()
        {
            Assert.AreEqual(0f, GetCycleOffset(), "Setup failed: the cycle offset should start at 0.");
            _behaviour.SetCycleOffset(0.4f);

            _behaviour.OnStateUpdate(_animator, default, 0);

            Assert.AreEqual(0.4f, GetCycleOffset(), 0.0001f);
        }

        [Test]
        public void Cycle_offset_follows_the_state_that_last_played()
        {
            _behaviour.SetCycleOffset(0.4f);
            _behaviour.OnStateUpdate(_animator, default, 0);
            Assert.AreEqual(0.4f, GetCycleOffset(), 0.0001f, "Setup failed: the first offset should be set.");

            var mirrored = ScriptableObject.CreateInstance<LocomotionCycleOffsetStateMachineBehaviour>();
            _created.Add(mirrored);
            mirrored.SetCycleOffset(0.9f);
            mirrored.OnStateUpdate(_animator, default, 0);

            Assert.AreEqual(0.9f, GetCycleOffset(), 0.0001f);
        }

        float GetCycleOffset() => _animator.GetFloat(AnimatorParameters.LocomotionCycleOffset.Id);
    }
}
