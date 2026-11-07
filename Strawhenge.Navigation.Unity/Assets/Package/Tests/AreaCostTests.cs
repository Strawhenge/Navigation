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
    // A mode's area costs decide which way the agent goes. Start and goal are joined by a short cut (a
    // corridor in its own area, or an off-mesh link in the Jump area) and by a long walkable detour. At
    // project costs the short cut is cheaper; leisurely mode makes it costly enough that the detour wins.
    public class AreaCostTests
    {
        const float Speed = 3f;
        const float Timeout = 3f;
        const float MovingSpeed = 0.5f;
        const float ShortcutCost = 10f;
        const int CorridorArea = 3; // First user-defined NavMesh area.

        // Any path corner this far along z is on the detour; the short cut stays at z = 0.
        const float DetourZ = 10f;

        static readonly Vector3 Start = new(0, 0, 0);
        static readonly Vector3 Goal = new(20, 0, 0);
        static readonly Vector3 LinkStart = new(1, 0, 0);
        static readonly Vector3 LinkEnd = new(19, 0, 0);

        readonly List<Object> _created = new();
        NavMeshDataInstance _navMesh;
        NavMeshAgent _navMeshAgent;
        DestinationController _controller;

        static int JumpArea => NavMesh.GetAreaFromName("Jump");

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
        public IEnumerator Default_mode_takes_the_corridor_at_project_costs()
        {
            yield return SetUpCorridor();

            yield return StartTrip(Trip());

            Assert.IsFalse(IsOnDetour(), "With no cost overrides, the corridor should be the cheaper way.");
        }

        [UnityTest]
        public IEnumerator Leisurely_mode_detours_around_a_costly_area()
        {
            yield return SetUpCorridor();

            yield return StartTrip(Trip().Leisurely());

            Assert.IsTrue(IsOnDetour(), "Leisurely mode makes the corridor costly, so the detour should be cheaper.");
        }

        [UnityTest]
        public IEnumerator Default_mode_restores_project_costs_after_a_leisurely_trip()
        {
            yield return SetUpCorridor();
            yield return StartTrip(Trip().Leisurely());
            Assert.IsTrue(IsOnDetour(), "Setup failed: the leisurely trip should take the detour.");

            _controller.Cancel();
            yield return StartTrip(Trip());

            Assert.IsFalse(
                IsOnDetour(),
                "The default mode doesn't override the corridor's area, so it should be back at its project cost.");
        }

        [UnityTest]
        public IEnumerator Leisurely_turned_on_mid_trip_applies_its_costs()
        {
            yield return SetUpCorridor();
            yield return StartTrip(Trip());
            Assert.IsFalse(IsOnDetour(), "Setup failed: the default trip should take the corridor.");

            yield return TickUntil(() => _controller.CurrentVelocity.magnitude >= MovingSpeed);
            Assert.GreaterOrEqual(
                _controller.CurrentVelocity.magnitude,
                MovingSpeed,
                "Setup failed: the agent never started moving.");

            _controller.GoTo(Trip().Leisurely().Build());
            yield return TickUntil(HasPath);

            Assert.IsTrue(IsOnDetour(), "The redirect asked for leisurely mode, so the path should take the detour.");
        }

        [UnityTest]
        public IEnumerator Default_mode_takes_the_off_mesh_link_at_project_costs()
        {
            yield return SetUpLink();

            yield return StartTrip(Trip());

            Assert.IsFalse(
                IsOnDetour(),
                "Setup failed or link not connected: with no cost overrides, the link should be the cheaper way.");
        }

        [UnityTest]
        public IEnumerator Leisurely_mode_detours_around_a_costly_off_mesh_link()
        {
            yield return SetUpLink();

            yield return StartTrip(Trip().Leisurely());

            Assert.IsTrue(IsOnDetour(), "Leisurely mode makes the Jump area costly, so the detour should be cheaper.");
        }

        IEnumerator SetUpCorridor()
        {
            BuildNavMesh(withCorridor: true);
            CreateController(CorridorArea);
            yield return null;
        }

        IEnumerator SetUpLink()
        {
            BuildNavMesh(withCorridor: false);
            CreateLink();
            CreateController(JumpArea);

            // Give the link a frame to connect to the NavMesh.
            yield return null;
        }

        IEnumerator StartTrip(IDestinationArgsBuilder trip)
        {
            _controller.GoTo(trip.Build());

            // Update at least once, so the path is this trip's: a cancelled trip's path stays on the agent
            // until the controller's next update disables it.
            yield return null;
            _controller.Update(Time.deltaTime);
            yield return TickUntil(HasPath);

            Assert.IsTrue(HasPath(), "Setup failed: the agent never got a path.");
        }

        static IDestinationArgsBuilder Trip() =>
            DestinationArgs.GoToApproximately(Goal).WithSpeed(Speed);

        bool HasPath() =>
            _navMeshAgent.enabled && _navMeshAgent.hasPath && !_navMeshAgent.pathPending;

        bool IsOnDetour()
        {
            foreach (var corner in _navMeshAgent.path.corners)
            {
                if (corner.z > DetourZ)
                    return true;
            }

            return false;
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

        // Start and goal pads 20 m apart, joined by a detour about 60 m long: up 20 m from the start,
        // across, and back down to the goal. The corridor between the pads is in CorridorArea.
        void BuildNavMesh(bool withCorridor)
        {
            var boxes = new List<NavMeshBuildSource>
            {
                TestNavMesh.Box(center: Start, size: new Vector2(4, 4)),
                TestNavMesh.Box(center: Goal, size: new Vector2(4, 4)),
                TestNavMesh.Box(center: new Vector3(0, 0, 11), size: new Vector2(4, 18)),
                TestNavMesh.Box(center: new Vector3(10, 0, 22), size: new Vector2(24, 4)),
                TestNavMesh.Box(center: new Vector3(20, 0, 11), size: new Vector2(4, 18))
            };

            if (withCorridor)
                boxes.Add(TestNavMesh.Box(center: new Vector3(10, 0, 0), size: new Vector2(16, 2), area: CorridorArea));

            _navMesh = TestNavMesh.Build(_created, boxes.ToArray());
        }

        // An 18 m off-mesh link in the Jump area, straight across the gap between the pads.
        void CreateLink()
        {
            var link = new GameObject("Link");
            link.SetActive(false);
            _created.Add(link);

            var start = new GameObject("Link Start").transform;
            start.SetParent(link.transform);
            start.position = LinkStart;

            var end = new GameObject("Link End").transform;
            end.SetParent(link.transform);
            end.position = LinkEnd;

            var offMeshLink = link.AddComponent<OffMeshLink>();
            offMeshLink.startTransform = start;
            offMeshLink.endTransform = end;
            offMeshLink.area = JumpArea;
            offMeshLink.biDirectional = true;

            link.SetActive(true);
            offMeshLink.UpdatePositions();
        }

        // Default mode leaves every area at its project cost; leisurely makes the short cut's area costly.
        void CreateController(int shortcutArea)
        {
            var character = new GameObject("Agent");
            character.SetActive(false);
            character.transform.position = Start;
            _created.Add(character);

            // Starts disabled, as Idle keeps it; Preparing enables it.
            _navMeshAgent = character.AddComponent<NavMeshAgent>();
            _navMeshAgent.baseOffset = 0;
            _navMeshAgent.enabled = false;
            character.SetActive(true);

            var modes = new FakeNavMeshAgentModes(
                new FakeNavMeshAgentMode(NavMesh.AllAreas),
                new FakeNavMeshAgentMode(NavMesh.AllAreas).WithAreaCost(shortcutArea, ShortcutCost));

            var agent = new Agent(_navMeshAgent, modes, new AgentPriorityManager());

            _controller = new DestinationController(NullDestinationContext.Instance, agent, new FakeLogger());
        }
    }
}
