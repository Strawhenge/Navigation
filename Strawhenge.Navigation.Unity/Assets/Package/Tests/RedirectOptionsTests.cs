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
    // A new request while the agent is already walking (Going.GoTo) must apply all of its options,
    // as a request from idle does in PrepareGoing: Strafe, Leisurely and FromStartingPoint used to be
    // ignored. An ordinary redirect must not stop the agent on the way.
    public class RedirectOptionsTests
    {
        const float Speed = 3f;
        const float CruisingSpeed = 2f;
        const float Timeout = 3f;
        const float RedirectObservationTime = 0.3f;
        const float MinSpeedWhileRedirecting = 0.5f;
        const float PositionTolerance = 0.5f;
        const int ExcludedArea = 3; // First user-defined NavMesh area.

        // With a fresh AgentPriorityManager (100 slots), default reserves the most important slot
        // and leisurely the least important.
        const int DefaultPriority = 0;
        const int LeisurelyPriority = 99;

        static readonly int DefaultAreaMask = NavMesh.AllAreas;
        static readonly int LeisurelyAreaMask = NavMesh.AllAreas & ~(1 << ExcludedArea);

        static readonly Vector3 FirstDestination = new(20, 0, 0);
        static readonly Vector3 RedirectDestination = new(25, 0, 3);

        readonly List<Object> _created = new();
        NavMeshDataInstance _navMesh;
        NavMeshAgent _navMeshAgent;
        DestinationController _controller;

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

            // Destroy is deferred to the end of the frame; wait so the next test starts clean.
            yield return null;
        }

        [UnityTest]
        public IEnumerator Strafe_turned_on_mid_trip_is_applied()
        {
            yield return StartMoving(Trip(FirstDestination));
            Assert.IsFalse(_controller.IsStrafing, "Setup failed: the first trip should not strafe.");

            _controller.GoTo(Trip(RedirectDestination).Strafe().Build());

            Assert.IsTrue(_controller.IsStrafing, "The redirect asked to strafe, but the agent isn't strafing.");
        }

        [UnityTest]
        public IEnumerator Strafe_turned_off_mid_trip_is_applied()
        {
            yield return StartMoving(Trip(FirstDestination).Strafe());
            Assert.IsTrue(_controller.IsStrafing, "Setup failed: the first trip should strafe.");

            _controller.GoTo(Trip(RedirectDestination).Build());

            Assert.IsFalse(_controller.IsStrafing, "The redirect didn't ask to strafe, but the agent is still strafing.");
        }

        [UnityTest]
        public IEnumerator Leisurely_turned_on_mid_trip_is_applied()
        {
            yield return StartMoving(Trip(FirstDestination));
            AssertMode(DefaultAreaMask, DefaultPriority, "Setup failed: the first trip should use the default mode.");

            _controller.GoTo(Trip(RedirectDestination).Leisurely().Build());

            AssertMode(LeisurelyAreaMask, LeisurelyPriority, "The redirect asked for leisurely mode.");
        }

        [UnityTest]
        public IEnumerator Leisurely_turned_off_mid_trip_is_applied()
        {
            yield return StartMoving(Trip(FirstDestination).Leisurely());
            AssertMode(LeisurelyAreaMask, LeisurelyPriority, "Setup failed: the first trip should be leisurely.");

            _controller.GoTo(Trip(RedirectDestination).Build());

            AssertMode(DefaultAreaMask, DefaultPriority, "The redirect asked for the default mode.");
        }

        [UnityTest]
        public IEnumerator Starting_point_given_mid_trip_is_applied()
        {
            var startingPoint = new Vector3(-10, 0, -10);

            yield return StartMoving(Trip(FirstDestination));

            _controller.GoTo(Trip(RedirectDestination).FromStartingPoint(startingPoint).Build());
            yield return TickUntil(() => DistanceTo(startingPoint) < PositionTolerance);

            Assert.Less(
                DistanceTo(startingPoint),
                PositionTolerance,
                $"The redirect gave a starting point of {startingPoint}, but the agent is at {_navMeshAgent.transform.position}.");
        }

        // Guards against redirects restarting the trip (warp + stop) when nothing needs it.
        [UnityTest]
        public IEnumerator Plain_redirect_keeps_the_agent_moving()
        {
            yield return StartMoving(Trip(FirstDestination));

            _controller.GoTo(Trip(RedirectDestination).Build());

            var minSpeed = float.MaxValue;
            var elapsed = 0f;

            while (elapsed < RedirectObservationTime)
            {
                minSpeed = Mathf.Min(minSpeed, _controller.CurrentVelocity.magnitude);

                yield return null;
                elapsed += Time.deltaTime;
                _controller.Update(Time.deltaTime);
            }

            Assert.Greater(
                minSpeed,
                MinSpeedWhileRedirecting,
                "The agent slowed down or stopped when redirected without any option changes.");
        }

        IEnumerator StartMoving(IDestinationArgsBuilder trip)
        {
            BuildNavMesh();
            CreateController();
            yield return null;

            _controller.GoTo(trip.Build());
            yield return TickUntil(() => _controller.CurrentVelocity.magnitude >= CruisingSpeed);

            Assert.GreaterOrEqual(
                _controller.CurrentVelocity.magnitude,
                CruisingSpeed,
                "Setup failed: the agent never got up to speed on its first trip.");
        }

        static IDestinationArgsBuilder Trip(Vector3 location) =>
            DestinationArgs.GoToApproximately(location).WithSpeed(Speed);

        void AssertMode(int areaMask, int avoidancePriority, string message)
        {
            Assert.AreEqual(areaMask, _navMeshAgent.areaMask, $"{message} Wrong area mask.");
            Assert.AreEqual(avoidancePriority, _navMeshAgent.avoidancePriority, $"{message} Wrong avoidance priority.");
        }

        float DistanceTo(Vector3 point)
        {
            var position = _navMeshAgent.transform.position;
            return Vector2.Distance(new Vector2(position.x, position.z), new Vector2(point.x, point.z));
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

        // One 60 m x 60 m walkable box around the origin, top surface at y = 0.
        void BuildNavMesh()
        {
            const float thickness = 0.1f;

            var source = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                size = new Vector3(60, thickness, 60),
                transform = Matrix4x4.Translate(new Vector3(0, -thickness / 2, 0)),
                area = 0
            };

            var data = NavMeshBuilder.BuildNavMeshData(
                NavMesh.GetSettingsByID(0),
                new List<NavMeshBuildSource> { source },
                new Bounds(Vector3.zero, new Vector3(80, 10, 80)),
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
            _navMeshAgent.baseOffset = 0;
            _navMeshAgent.enabled = false;
            character.SetActive(true);

            var agent = new Agent(
                _navMeshAgent,
                new FakeNavMeshAgentModes(DefaultAreaMask, LeisurelyAreaMask),
                new AgentPriorityManager());

            _controller = new DestinationController(NullDestinationContext.Instance, agent);
        }

        sealed class FakeNavMeshAgentModes : INavMeshAgentModes
        {
            public FakeNavMeshAgentModes(int defaultAreaMask, int leisurelyAreaMask)
            {
                DefaultAreaMask = defaultAreaMask;
                LeisurelyAreaMask = leisurelyAreaMask;
            }

            public int DefaultAreaMask { get; }

            public int LeisurelyAreaMask { get; }
        }
    }
}
