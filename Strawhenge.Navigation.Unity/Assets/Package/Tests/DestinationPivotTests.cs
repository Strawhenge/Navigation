using NUnit.Framework;
using Strawhenge.Navigation.Unity.Destination;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Strawhenge.Navigation.Unity.Tests
{
    // Destination trips play pivots as locomotion does: a stationary pivot before setting off towards a
    // destination behind the character, and a moving pivot on a sharp turn. The agent waits for the
    // Animator's CompletePivot (called here by hand), and never waits forever.
    public class DestinationPivotTests
    {
        const int StationaryPivotId = 1;
        const int MovingPivotId = 2;
        const float Speed = 3f;
        const float CruisingSpeed = 2f;
        const float Timeout = 6f;
        const float HoldTime = 0.3f;
        const float StillTolerance = 0.05f;

        // The character starts at the origin facing +z.
        static readonly Vector3 Ahead = new(1, 0, 10);
        static readonly Vector3 FarAhead = new(0, 0, 20);
        static readonly Vector3 Behind = new(1, 0, -10);

        readonly List<Object> _created = new();
        readonly List<int> _pivotIds = new();
        readonly List<DestinationResult> _results = new();

        NavMeshDataInstance _navMesh;
        NavMeshAgent _navMeshAgent;
        DestinationController _controller;
        FakeDestinationSettings _settings;
        FakeLogger _logger;
        int _pivotCancelledCount;

        [SetUp]
        public void SetUp()
        {
            _settings = new FakeDestinationSettings
            {
                StationaryPivots = new IPivotProfile[]
                {
                    new FakePivotProfile(
                        StationaryPivotId,
                        new ValueRange(0, 0.5f),
                        new ValueRange(135, 180),
                        new ValueRange(-180, -135))
                },
                MovingPivots = new IPivotProfile[]
                {
                    new FakePivotProfile(
                        MovingPivotId,
                        new ValueRange(1, 100),
                        new ValueRange(135, 180),
                        new ValueRange(-180, -135))
                }
            };
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
            _pivotIds.Clear();
            _results.Clear();
            _pivotCancelledCount = 0;

            // Destroy is deferred to the end of the frame; wait so the next test starts clean.
            yield return null;
        }

        // Baseline for the tests below that expect no pivot.
        [UnityTest]
        public IEnumerator Stationary_pivot_plays_before_setting_off_towards_a_destination_behind()
        {
            yield return Create();

            _controller.GoTo(Trip(Behind).Build());
            _controller.Update(Time.deltaTime);

            CollectionAssert.AreEqual(new[] { StationaryPivotId }, _pivotIds, "Expected the stationary pivot.");

            yield return Tick(HoldTime);
            var position = _navMeshAgent.transform.position;
            Assert.Less(
                new Vector2(position.x, position.z).magnitude,
                StillTolerance,
                "The agent set off before the pivot was completed.");

            CompletePivotTurnedBy(180);
            Assert.Less(
                Vector3.Angle(_navMeshAgent.transform.forward, Vector3.back),
                1f,
                "The character should face where the pivot turned it.");

            yield return TickUntil(() => _results.Count > 0);
            AssertArrived();
        }

        [UnityTest]
        public IEnumerator No_pivot_towards_a_destination_ahead()
        {
            yield return Create();

            _controller.GoTo(Trip(Ahead).Build());
            yield return TickUntil(() => _results.Count > 0);

            AssertArrived();
            CollectionAssert.IsEmpty(_pivotIds, "No pivot should play for a destination ahead.");
        }

        [UnityTest]
        public IEnumerator No_pivot_while_strafing()
        {
            yield return Create();

            _controller.GoTo(Trip(Behind).Strafe().Build());
            yield return TickUntil(() => _results.Count > 0);

            AssertArrived();
            CollectionAssert.IsEmpty(_pivotIds, "No pivot should play while strafing.");
        }

        // Without this, a pivot that doesn't turn the character would match again straight away, forever.
        [UnityTest]
        public IEnumerator Pivot_that_did_not_turn_the_character_is_not_repeated()
        {
            yield return Create();

            _controller.GoTo(Trip(Behind).Build());
            _controller.Update(Time.deltaTime);
            Assert.AreEqual(1, _pivotIds.Count, "Setup failed: expected the stationary pivot.");

            _controller.CompletePivot();
            yield return TickUntil(() => _results.Count > 0);

            AssertArrived();
            Assert.AreEqual(1, _pivotIds.Count, "The pivot was requested again after completing without turning.");
        }

        // Root motion turns the character while the pivot plays, so completing it mustn't turn it any further
        // (the Animator's last-frame delta used to be applied again on top).
        [UnityTest]
        public IEnumerator Pivot_turned_by_root_motion_is_not_turned_again_when_it_completes()
        {
            yield return Create();

            _controller.GoTo(Trip(Behind).Build());
            _controller.Update(Time.deltaTime);
            Assert.AreEqual(1, _pivotIds.Count, "Setup failed: expected the stationary pivot.");

            var turned = Quaternion.LookRotation(Vector3.back);
            _navMeshAgent.transform.rotation = turned; // Root motion played the turn.
            _controller.CompletePivot();

            Assert.Less(
                Quaternion.Angle(_navMeshAgent.transform.rotation, turned),
                1f,
                "The character should face where root motion turned it.");
        }

        // Root motion can drift a little even when the clip doesn't turn the character: that's still no turn.
        [UnityTest]
        public IEnumerator Pivot_that_barely_turned_the_character_is_not_repeated()
        {
            yield return Create();

            _controller.GoTo(Trip(Behind).Build());
            _controller.Update(Time.deltaTime);
            Assert.AreEqual(1, _pivotIds.Count, "Setup failed: expected the stationary pivot.");

            CompletePivotTurnedBy(0.5f);
            yield return TickUntil(() => _results.Count > 0);

            AssertArrived();
            Assert.AreEqual(1, _pivotIds.Count, "The pivot was requested again after completing without turning.");
        }

        [UnityTest]
        public IEnumerator Pivot_nobody_completes_times_out_and_the_trip_carries_on()
        {
            _settings.AnimatorWaitTimeout = 0.2f;
            yield return Create();

            _controller.GoTo(Trip(Behind).Build());
            yield return TickUntil(() => _results.Count > 0);

            AssertArrived();
            Assert.AreEqual(1, _pivotIds.Count, "The timed-out pivot was requested again.");
            Assert.IsTrue(
                _logger.Warnings.Any(w => w.Contains($"Pivot ID {StationaryPivotId} wasn't completed")),
                "The timed-out pivot wasn't warned about.");
        }

        // Baseline for the test below.
        [UnityTest]
        public IEnumerator Completed_pivot_is_not_cancelled()
        {
            yield return Create();

            _controller.GoTo(Trip(Behind).Build());
            _controller.Update(Time.deltaTime);
            Assert.AreEqual(1, _pivotIds.Count, "Setup failed: expected the stationary pivot.");

            CompletePivotTurnedBy(180);
            yield return TickUntil(() => _results.Count > 0);

            AssertArrived();
            Assert.AreEqual(0, _pivotCancelledCount, "A completed pivot shouldn't be cancelled.");
        }

        // Its animation may still be playing, or yet to start: whatever plays it should drop it.
        [UnityTest]
        public IEnumerator Pivot_that_times_out_is_cancelled()
        {
            _settings.AnimatorWaitTimeout = 0.2f;
            yield return Create();

            _controller.GoTo(Trip(Behind).Build());
            yield return TickUntil(() => _results.Count > 0);

            AssertArrived();
            Assert.AreEqual(1, _pivotCancelledCount, "The timed-out pivot should be cancelled once.");
        }

        [UnityTest]
        public IEnumerator Cancel_mid_pivot_calls_back_straight_away_and_the_pivot_still_finishes()
        {
            yield return Create();

            _controller.GoTo(Trip(Behind).Build());
            _controller.Update(Time.deltaTime);
            Assert.AreEqual(1, _pivotIds.Count, "Setup failed: expected the stationary pivot.");

            _controller.Cancel();

            CollectionAssert.AreEqual(new[] { DestinationResult.Cancelled }, _results, "Expected one Cancelled callback.");
            Assert.IsTrue(_controller.IsActive, "The controller should stay active until the pivot finishes.");

            CompletePivotTurnedBy(180);

            Assert.IsFalse(_controller.IsActive, "The controller should stop once the pivot finishes.");
            Assert.AreEqual(1, _results.Count, "The cancelled request was called back again.");
        }

        [UnityTest]
        public IEnumerator Moving_pivot_plays_when_redirected_behind_while_running()
        {
            yield return Create();

            _controller.GoTo(Trip(FarAhead).Build());
            yield return TickUntil(() => _controller.CurrentVelocity.magnitude >= CruisingSpeed);
            Assume(_controller.CurrentVelocity.magnitude >= CruisingSpeed, "the agent never got up to speed");
            Assume(_pivotIds.Count == 0, "a pivot played on the way out");

            _controller.GoTo(Trip(Behind).Build());
            yield return TickUntil(() => _pivotIds.Count > 0);

            CollectionAssert.AreEqual(new[] { MovingPivotId }, _pivotIds, "Expected the moving pivot.");
        }

        IEnumerator Create()
        {
            _navMesh = TestNavMesh.Build(_created, TestNavMesh.Box(Vector3.zero, new Vector2(60, 60)));

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

            _logger = new FakeLogger();
            _controller = new DestinationController(NullDestinationContext.Instance, agent, _logger, _settings);
            _controller.PivotRequested += _pivotIds.Add;
            _controller.PivotCancelled += () => _pivotCancelledCount++;

            yield return null;
        }

        IDestinationArgsBuilder Trip(Vector3 location) =>
            DestinationArgs.GoToApproximately(location).WithSpeed(Speed).Callback(_results.Add);

        // Plays the Animator's part: root motion turns the character, then the pivot completes.
        void CompletePivotTurnedBy(float degrees)
        {
            var transform = _navMeshAgent.transform;
            transform.rotation = Quaternion.Euler(0, degrees, 0) * transform.rotation;
            _controller.CompletePivot();
        }

        void AssertArrived() =>
            CollectionAssert.AreEqual(new[] { DestinationResult.Arrived }, _results, "Expected the trip to arrive.");

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
