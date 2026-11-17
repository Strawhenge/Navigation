using NUnit.Framework;
using Strawhenge.Navigation.Unity.Destination;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Strawhenge.Navigation.Unity.Tests
{
    // While IDestinationContext.CanNavigate is false (e.g. the character is ragdolled), the trip pauses: the
    // agent is disabled and gives up its avoidance priority slot, then the trip carries on once it can
    // navigate again. The agent used to stay enabled for the update the pause started on, so it could
    // still move the character during the first ragdoll frame.
    public class DestinationContextTests
    {
        const float Speed = 3f;
        const float MovingSpeed = 0.5f;
        const float Timeout = 5f;
        const float PauseTime = 0.3f;
        const float StoppedTolerance = 0.05f;
        const float DestinationTolerance = 0.5f;

        // With a fresh AgentPriorityManager, default reserves the most important slot.
        const int MostImportant = 0;

        // Close enough to reach well within Timeout after the pause.
        static readonly Vector3 Destination = new(8, 0, 0);

        readonly List<Object> _created = new();
        readonly List<DestinationResult> _results = new();

        NavMeshDataInstance _navMesh;
        NavMeshAgent _navMeshAgent;
        FakeDestinationContext _context;
        AgentPriorityManager _priorityManager;
        DestinationController _controller;

        [SetUp]
        public void SetUp()
        {
            _navMesh = TestNavMesh.Build(_created, TestNavMesh.Box(Vector3.zero, new Vector2(60, 60)));
            CreateController();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_navMesh.valid)
                _navMesh.Remove();

            foreach (var obj in _created)
            {
                if (obj != null)
                    Object.Destroy(obj);
            }

            _created.Clear();
            _results.Clear();

            // Destroy is deferred to the end of the frame; wait so the next test starts clean.
            yield return null;
        }

        [UnityTest]
        public IEnumerator Agent_is_disabled_on_the_update_the_pause_starts()
        {
            yield return StartMoving();

            _context.CanNavigate = false;
            _controller.Update(Time.deltaTime);

            Assert.IsFalse(_navMeshAgent.enabled, "The agent should be disabled as soon as the pause starts.");
        }

        [UnityTest]
        public IEnumerator Priority_slot_is_freed_on_the_update_the_pause_starts()
        {
            yield return StartMoving();

            Assert.AreEqual(
                MostImportant,
                _navMeshAgent.avoidancePriority,
                "Setup failed: the moving agent should hold the most important slot.");

            _context.CanNavigate = false;
            _controller.Update(Time.deltaTime);

            var other = CreateNavMeshAgent("Other");
            _priorityManager.ReserveHighPriority(other);

            Assert.AreEqual(
                MostImportant,
                other.avoidancePriority,
                "The paused agent should have given up its slot as soon as the pause started.");
        }

        [UnityTest]
        public IEnumerator Character_stays_put_while_paused_then_carries_on_to_the_destination()
        {
            yield return StartMoving();

            _context.CanNavigate = false;
            _controller.Update(Time.deltaTime);

            var pausedAt = _navMeshAgent.transform.position;
            yield return Tick(PauseTime);

            Assert.Less(
                Vector3.Distance(pausedAt, _navMeshAgent.transform.position),
                StoppedTolerance,
                "The character should stay put while it can't navigate.");
            Assert.IsEmpty(_results, "A pause shouldn't end the trip.");

            _context.CanNavigate = true;
            yield return TickUntil(() => _results.Count > 0);

            Assert.AreEqual(
                1,
                _results.Count,
                $"The trip should be called back once. The character got to {_navMeshAgent.transform.position} " +
                $"(agent enabled: {_navMeshAgent.enabled}).");
            Assert.IsTrue(_results[0].IsAtDestination, "The trip should carry on to its destination.");
            Assert.Less(
                Vector3.Distance(Destination, _navMeshAgent.transform.position),
                DestinationTolerance,
                $"The character should end at {Destination}, but it's at {_navMeshAgent.transform.position}.");
        }

        [UnityTest]
        public IEnumerator IsPaused_is_true_only_while_the_character_cannot_navigate()
        {
            Assert.IsFalse(_controller.IsPaused, "An idle controller isn't paused.");

            yield return StartMoving();

            Assert.IsFalse(_controller.IsPaused, "A moving trip isn't paused.");

            _context.CanNavigate = false;
            _controller.Update(Time.deltaTime);

            Assert.IsTrue(_controller.IsPaused, "The trip should be paused while the character can't navigate.");
            Assert.IsTrue(_controller.IsActive, "A paused trip is still active.");

            yield return Tick(PauseTime);

            Assert.IsTrue(_controller.IsPaused, "The trip should stay paused while the character can't navigate.");

            _context.CanNavigate = true;
            _controller.Update(Time.deltaTime);

            Assert.IsFalse(_controller.IsPaused, "The trip should carry on once the character can navigate.");
        }

        [UnityTest]
        public IEnumerator IsPaused_is_false_after_a_cancel_while_paused()
        {
            yield return StartMoving();

            _context.CanNavigate = false;
            _controller.Update(Time.deltaTime);
            Assert.IsTrue(_controller.IsPaused, "Setup failed: the trip should be paused.");

            _controller.Cancel();

            Assert.IsFalse(_controller.IsPaused, "A cancelled trip isn't paused.");
            Assert.IsFalse(_controller.IsActive, "A cancelled trip isn't active.");
        }

        IEnumerator StartMoving()
        {
            _controller.GoTo(DestinationArgs
                .GoToExactly(Destination)
                .WithSpeed(Speed)
                .Callback(_results.Add)
                .Build());

            yield return TickUntil(() => _navMeshAgent.enabled && _navMeshAgent.velocity.magnitude > MovingSpeed);

            Assert.IsTrue(_navMeshAgent.enabled, "Setup failed: the agent should be enabled while moving.");
            Assert.Greater(
                _navMeshAgent.velocity.magnitude,
                MovingSpeed,
                "Setup failed: the agent should be moving before the pause.");
        }

        void CreateController()
        {
            _navMeshAgent = CreateNavMeshAgent("Agent");
            _context = new FakeDestinationContext();
            _priorityManager = new AgentPriorityManager();

            var agent = new Agent(_navMeshAgent, new FixedNavMeshAgentModes(NavMesh.AllAreas), _priorityManager);
            _controller = new DestinationController(_context, agent, new FakeLogger());
        }

        // Starts disabled, as it is while idle; Preparing enables it.
        NavMeshAgent CreateNavMeshAgent(string name)
        {
            var character = new GameObject(name);
            character.SetActive(false);
            _created.Add(character);

            var navMeshAgent = character.AddComponent<NavMeshAgent>();
            navMeshAgent.baseOffset = 0;
            navMeshAgent.enabled = false;
            character.SetActive(true);

            return navMeshAgent;
        }

        IEnumerator Tick(float duration)
        {
            var elapsed = 0f;

            while (elapsed < duration)
            {
                yield return null;
                elapsed += Time.deltaTime;
                _controller.Update(Time.deltaTime);
            }
        }

        IEnumerator TickUntil(Func<bool> condition)
        {
            var elapsed = 0f;

            while (!condition() && elapsed < Timeout)
            {
                yield return null;
                elapsed += Time.deltaTime;
                _controller.Update(Time.deltaTime);
            }
        }
    }
}
