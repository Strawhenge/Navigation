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
    // Every destination request must get exactly one callback, even when a callback starts a new
    // request. A "patrol" whose callback re-issues GoTo whenever it is replaced reproduces the bug:
    // redirecting it ran the old request's callback again and again (capped here by MaxChain,
    // otherwise a stack overflow), and the newest request was lost.
    public class DestinationCallbackTests
    {
        const int MaxChain = 10;
        const float Speed = 3f;
        const float Timeout = 3f;
        const float DestinationTolerance = 0.5f;

        static readonly Vector3 PointA = new(10, 0, 0);
        static readonly Vector3 PointB = new(-10, 0, 0);

        readonly List<Object> _created = new();
        readonly List<string> _requests = new();
        readonly Dictionary<string, List<string>> _callbacks = new();

        NavMeshDataInstance _navMesh;
        NavMeshAgent _navMeshAgent;
        DestinationController _controller;
        int _chainCount;

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
            _requests.Clear();
            _callbacks.Clear();
            _chainCount = 0;

            // Destroy is deferred to the end of the frame; wait so the next test starts clean.
            yield return null;
        }

        // PrepareGoing.GoTo: redirected in the same frame, before the agent starts moving.
        // Needs no NavMesh: nothing here reaches the agent.
        [UnityTest]
        public IEnumerator Redirect_before_moving_calls_back_each_request_once()
        {
            CreateController();
            yield return null;

            _controller.GoTo(Patrol(PointA));
            _controller.GoTo(Request("redirect", PointB));

            _controller.Cancel();

            AssertEachRequestCalledBackOnce();
            AssertNewestRequestWasActive();
        }

        // Going.GoTo: redirected while walking along a path.
        [UnityTest]
        public IEnumerator Redirect_while_moving_calls_back_each_request_once()
        {
            BuildNavMesh();
            CreateController();

            _controller.GoTo(Patrol(PointA));
            yield return TickUntil(() => _controller.CurrentVelocity.sqrMagnitude > 0.01f);

            Assert.Greater(
                _controller.CurrentVelocity.sqrMagnitude,
                0.01f,
                "Setup failed: the agent never started moving.");

            _controller.GoTo(Request("redirect", PointB));
            var destination = _navMeshAgent.destination;

            _controller.Cancel();

            AssertEachRequestCalledBackOnce();
            AssertNewestRequestWasActive();

            Assert.Less(
                HorizontalDistance(destination, PointA),
                DestinationTolerance,
                $"The agent should head for the newest request (the patrol's, {PointA}), but was heading for {destination}.");
        }

        void BuildNavMesh()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(5, 1, 5); // 50 m x 50 m
            _created.Add(ground);

            var bounds = new Bounds(Vector3.zero, new Vector3(60, 10, 60));
            var sources = new List<NavMeshBuildSource>();

            NavMeshBuilder.CollectSources(
                bounds,
                ~0,
                NavMeshCollectGeometry.RenderMeshes,
                0, // default area: Walkable
                new List<NavMeshBuildMarkup>(),
                sources);

            var data = NavMeshBuilder.BuildNavMeshData(
                NavMesh.GetSettingsByID(0),
                sources,
                bounds,
                Vector3.zero,
                Quaternion.identity);
            _created.Add(data);

            _navMesh = NavMesh.AddNavMeshData(data);
        }

        void CreateController()
        {
            var character = new GameObject("Agent");
            character.SetActive(false);
            _created.Add(character);

            // Starts disabled, as Idle keeps it; PrepareGoing enables it.
            _navMeshAgent = character.AddComponent<NavMeshAgent>();
            _navMeshAgent.enabled = false;
            character.SetActive(true);

            var agent = new Agent(_navMeshAgent, new FixedNavMeshAgentModes(NavMesh.AllAreas), new AgentPriorityManager());
            _controller = new DestinationController(NullDestinationContext.Instance, agent, new FakeLogger());
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

        // Like `.Callback(GoToNextWaypoint)`: when replaced by another request, starts a new one.
        DestinationArgs Patrol(Vector3 location)
        {
            var name = _chainCount == 0 ? "patrol" : $"patrol (chained {_chainCount})";

            return Request(name, location, result =>
            {
                if (!result.HasNewDestination || _chainCount >= MaxChain)
                    return;

                _chainCount++;
                _controller.GoTo(Patrol(location));
            });
        }

        DestinationArgs Request(string name, Vector3 location, Action<DestinationResult> onResult = null)
        {
            _requests.Add(name);
            _callbacks[name] = new List<string>();

            return DestinationArgs
                .GoToApproximately(location)
                .WithSpeed(Speed)
                .Callback(result =>
                {
                    _callbacks[name].Add(Describe(result));
                    onResult?.Invoke(result);
                })
                .Build();
        }

        void AssertEachRequestCalledBackOnce()
        {
            var wrong = _requests
                .Where(name => _callbacks[name].Count != 1)
                .Select(name =>
                    $"'{name}': {_callbacks[name].Count} callback(s) [{string.Join(", ", _callbacks[name])}]")
                .ToArray();

            Assert.IsEmpty(
                wrong,
                "Every request should get exactly one callback. These didn't:\n" + string.Join("\n", wrong));
        }

        void AssertNewestRequestWasActive()
        {
            var newest = _requests.Last();

            CollectionAssert.AreEqual(
                new[] { "Cancelled" },
                _callbacks[newest],
                $"The newest request ('{newest}') should have been the active one when Cancel() was called.");
        }

        static float HorizontalDistance(Vector3 a, Vector3 b) =>
            Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        static string Describe(DestinationResult result)
        {
            if (result.IsAtDestination) return "Arrived";
            if (result.IsDestinationInaccessible) return "Inaccessible";
            if (result.HasNewDestination) return "CancelledByNewDestination";
            return "Cancelled";
        }
    }
}
