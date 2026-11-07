using NUnit.Framework;
using Strawhenge.Navigation.Unity.Destination;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

            _controller.CompletePivot(Quaternion.Euler(0, 180, 0));
            Assert.Less(
                Vector3.Angle(_navMeshAgent.transform.forward, Vector3.back),
                1f,
                "The pivot's rotation wasn't applied.");

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

            _controller.CompletePivot(Quaternion.identity);
            yield return TickUntil(() => _results.Count > 0);

            AssertArrived();
            Assert.AreEqual(1, _pivotIds.Count, "The pivot was requested again after completing without turning.");
        }

        [UnityTest]
        public IEnumerator Pivot_nobody_completes_times_out_and_the_trip_carries_on()
        {
            _settings.AnimatorWaitTimeout = 0.2f;
            yield return Create();

            LogAssert.Expect(LogType.Warning, new Regex($"Pivot ID {StationaryPivotId} wasn't completed"));

            _controller.GoTo(Trip(Behind).Build());
            yield return TickUntil(() => _results.Count > 0);

            AssertArrived();
            Assert.AreEqual(1, _pivotIds.Count, "The timed-out pivot was requested again.");
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

            _controller.CompletePivot(Quaternion.Euler(0, 180, 0));

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

            _controller = new DestinationController(NullDestinationContext.Instance, agent, _settings);
            _controller.PivotRequested += _pivotIds.Add;

            yield return null;
        }

        IDestinationArgsBuilder Trip(Vector3 location) =>
            DestinationArgs.GoToApproximately(location).WithSpeed(Speed).Callback(_results.Add);

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
