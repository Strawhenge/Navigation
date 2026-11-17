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
    // When the character can't navigate (e.g. it's ragdolled), a phase that can't otherwise be interrupted is
    // abandoned straight away rather than finished: the Animator won't play a pivot or landing for a ragdoll,
    // and a jump or drop would keep moving the character by hand. These use a pivot, the one such phase a
    // NavMesh built in code can reach; jumps and drops go through the same code in DestinationController.
    // Pivots used to keep the agent enabled until AnimatorWaitTimeout, then log a false timeout warning.
    public class DestinationContextPivotTests
    {
        const int StationaryPivotId = 1;
        const float Speed = 3f;
        const float Timeout = 6f;
        const float AnimatorWaitTimeout = 0.2f;
        const float DestinationTolerance = 0.5f;

        // The character starts at the origin facing +z, so a trip behind it starts with a stationary pivot.
        static readonly Vector3 Behind = new(1, 0, -8);

        readonly List<Object> _created = new();
        readonly List<int> _pivotIds = new();
        readonly List<DestinationResult> _results = new();

        NavMeshDataInstance _navMesh;
        NavMeshAgent _navMeshAgent;
        FakeDestinationContext _context;
        DestinationController _controller;
        FakeLogger _logger;
        int _deactivatedCount;

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
            _pivotIds.Clear();
            _results.Clear();
            _deactivatedCount = 0;

            // Destroy is deferred to the end of the frame; wait so the next test starts clean.
            yield return null;
        }

        [UnityTest]
        public IEnumerator Pivot_is_abandoned_when_the_character_cannot_navigate()
        {
            yield return StartPivot();

            _context.CanNavigate = false;
            _controller.Update(Time.deltaTime);

            Assert.IsFalse(_navMeshAgent.enabled, "The agent should be disabled as soon as the pause starts.");
            Assert.IsTrue(_controller.IsActive, "The trip should still be active while paused.");
            Assert.IsEmpty(_results, "A pause shouldn't end the trip.");
        }

        // The Animator could still finish the abandoned pivot, e.g. as the ragdoll ends.
        [UnityTest]
        public IEnumerator Abandoned_pivot_completing_late_is_ignored()
        {
            yield return StartPivot();

            _context.CanNavigate = false;
            _controller.Update(Time.deltaTime);

            var forward = _navMeshAgent.transform.forward;
            _controller.CompletePivot(Quaternion.Euler(0, 180, 0));

            Assert.Less(
                Vector3.Angle(forward, _navMeshAgent.transform.forward),
                1f,
                "The abandoned pivot's rotation shouldn't be applied.");
        }

        // DestinationPivotTests shows this warning is logged for a pivot nobody completes.
        [UnityTest]
        public IEnumerator No_pivot_timeout_warning_while_the_character_cannot_navigate()
        {
            yield return StartPivot();

            _context.CanNavigate = false;
            yield return Tick(AnimatorWaitTimeout * 3);

            Assert.IsEmpty(_logger.Warnings, "Nothing should time out while the character can't navigate.");
        }

        [UnityTest]
        public IEnumerator Trip_restarts_with_its_pivot_and_arrives_once_the_character_can_navigate()
        {
            yield return StartPivot();

            _context.CanNavigate = false;
            _controller.Update(Time.deltaTime);

            _context.CanNavigate = true;
            yield return TickUntil(() => _pivotIds.Count > 1);

            CollectionAssert.AreEqual(
                new[] { StationaryPivotId, StationaryPivotId },
                _pivotIds,
                "The restarted trip should play its stationary pivot again.");

            _controller.CompletePivot(Quaternion.Euler(0, 180, 0));
            yield return TickUntil(() => _results.Count > 0);

            CollectionAssert.AreEqual(new[] { DestinationResult.Arrived }, _results, "Expected one Arrived callback.");
            Assert.Less(
                Vector3.Distance(Behind, _navMeshAgent.transform.position),
                DestinationTolerance,
                $"The character should end at {Behind}, but it's at {_navMeshAgent.transform.position}.");
        }

        // Its request has already been called back, so there's nothing to carry on with afterwards.
        [UnityTest]
        public IEnumerator Cancelled_pivot_stops_the_controller_when_the_character_cannot_navigate()
        {
            yield return StartPivot();

            _controller.Cancel();
            Assume(_controller.IsActive, "the controller should stay active while a cancelled pivot plays");

            _context.CanNavigate = false;
            _controller.Update(Time.deltaTime);

            Assert.IsFalse(_controller.IsActive, "The controller should stop.");
            Assert.AreEqual(1, _deactivatedCount, "Deactivated should be raised once.");
            Assert.IsFalse(_navMeshAgent.enabled, "The agent should be disabled.");
            CollectionAssert.AreEqual(
                new[] { DestinationResult.Cancelled },
                _results,
                "The cancelled request shouldn't be called back again.");

            _context.CanNavigate = true;
            yield return Tick(AnimatorWaitTimeout * 3);

            Assert.IsFalse(_controller.IsActive, "Nothing should restart once the character can navigate.");
        }

        IEnumerator StartPivot()
        {
            _navMesh = TestNavMesh.Build(_created, TestNavMesh.Box(Vector3.zero, new Vector2(40, 40)));
            CreateController();
            yield return null;

            _controller.GoTo(DestinationArgs.GoToApproximately(Behind).WithSpeed(Speed).Callback(_results.Add).Build());
            _controller.Update(Time.deltaTime);

            CollectionAssert.AreEqual(
                new[] { StationaryPivotId },
                _pivotIds,
                "Setup failed: the trip should start with the stationary pivot.");
            Assume(_navMeshAgent.enabled, "the agent should be enabled while the pivot plays");
        }

        void CreateController()
        {
            var character = new GameObject("Agent");
            character.SetActive(false);
            _created.Add(character);

            // Starts disabled, as it is while idle; Preparing enables it.
            _navMeshAgent = character.AddComponent<NavMeshAgent>();
            _navMeshAgent.baseOffset = 0;
            _navMeshAgent.enabled = false;
            character.SetActive(true);

            var agent = new Agent(
                _navMeshAgent,
                new FakeNavMeshAgentModes(NavMesh.AllAreas, NavMesh.AllAreas),
                new AgentPriorityManager());

            var settings = new FakeDestinationSettings
            {
                AnimatorWaitTimeout = AnimatorWaitTimeout,
                StationaryPivots = new IPivotProfile[]
                {
                    new FakePivotProfile(
                        StationaryPivotId,
                        new ValueRange(0, 0.5f),
                        new ValueRange(135, 180),
                        new ValueRange(-180, -135))
                }
            };

            _context = new FakeDestinationContext();
            _logger = new FakeLogger();
            _controller = new DestinationController(_context, agent, _logger, settings);
            _controller.PivotRequested += _pivotIds.Add;
            _controller.Deactivated += () => _deactivatedCount++;
        }

        static void Assume(bool condition, string failure) =>
            Assert.IsTrue(condition, $"Setup failed: {failure}.");

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
