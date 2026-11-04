using NUnit.Framework;
using Strawhenge.Navigation.Unity.Destination;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Strawhenge.Navigation.Unity.Tests
{
    // IsLocationAccessible is a question: it must not move the character, change the agent, or log
    // anything, and it answers yes only for a complete path. It used to enable the NavMeshAgent to
    // ask, which snapped the character onto the NavMesh, and logged a warning when it was too far off.
    public class LocationAccessibleTests
    {
        const float PositionTolerance = 0.0001f;

        static readonly Vector3 ReachableLocation = new(5, 0, 0);
        static readonly Vector3 IslandLocation = new(100, 0, 0);

        readonly List<Object> _created = new();
        readonly List<NavMeshDataInstance> _navMeshes = new();

        NavMeshAgent _navMeshAgent;
        DestinationController _controller;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var navMesh in _navMeshes.Where(navMesh => navMesh.valid))
                navMesh.Remove();

            foreach (var obj in _created)
            {
                if (obj != null)
                    Object.Destroy(obj);
            }

            _navMeshes.Clear();
            _created.Clear();

            // Destroy is deferred to the end of the frame; wait so the next test starts clean.
            yield return null;
        }

        [UnityTest]
        public IEnumerator Does_not_move_character_slightly_above_the_NavMesh()
        {
            BuildGroundAndIsland();
            CreateController(position: new Vector3(0, 0.5f, 0));
            yield return null;

            var start = _navMeshAgent.transform.position;

            var accessible = _controller.IsLocationAccessible(ReachableLocation);

            Assert.IsTrue(accessible, "A location on the same NavMesh should be accessible.");
            Assert.Less(
                Vector3.Distance(start, _navMeshAgent.transform.position),
                PositionTolerance,
                $"Checking accessibility moved the character from {start} to {_navMeshAgent.transform.position}.");
        }

        [UnityTest]
        public IEnumerator Leaves_the_agent_disabled()
        {
            BuildGroundAndIsland();
            CreateController(position: Vector3.zero);
            yield return null;

            _controller.IsLocationAccessible(ReachableLocation);

            Assert.IsFalse(_navMeshAgent.enabled, "Checking accessibility left the NavMeshAgent enabled.");
        }

        [UnityTest]
        public IEnumerator Returns_false_for_a_location_with_no_path()
        {
            BuildGroundAndIsland();
            CreateController(position: Vector3.zero);
            yield return null;

            var accessible = _controller.IsLocationAccessible(IslandLocation);

            Assert.IsFalse(accessible, "A location on a disconnected NavMesh should not be accessible.");
        }

        [UnityTest]
        public IEnumerator Returns_false_without_logging_when_far_above_the_NavMesh()
        {
            BuildGroundAndIsland();
            CreateController(position: new Vector3(0, 10, 0));
            yield return null;

            var accessible = _controller.IsLocationAccessible(ReachableLocation);

            Assert.IsFalse(accessible, "A character far from any NavMesh has no path anywhere.");
            LogAssert.NoUnexpectedReceived();
        }

        // Ground: 50 m x 50 m around the origin. Island: 10 m x 10 m at x = 100, not connected.
        void BuildGroundAndIsland() =>
            BuildNavMesh(
                Box(center: new Vector3(0, 0, 0), size: new Vector2(50, 50)),
                Box(center: IslandLocation, size: new Vector2(10, 10)));

        void BuildNavMesh(params NavMeshBuildSource[] sources)
        {
            var bounds = new Bounds(Vector3.zero, new Vector3(300, 20, 300));

            var data = NavMeshBuilder.BuildNavMeshData(
                NavMesh.GetSettingsByID(0),
                sources.ToList(),
                bounds,
                Vector3.zero,
                Quaternion.identity);
            _created.Add(data);

            _navMeshes.Add(NavMesh.AddNavMeshData(data));
        }

        // A thin box whose top surface is walkable, at center.y.
        static NavMeshBuildSource Box(Vector3 center, Vector2 size, int area = 0)
        {
            const float thickness = 0.1f;

            return new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                size = new Vector3(size.x, thickness, size.y),
                transform = Matrix4x4.Translate(center - new Vector3(0, thickness / 2, 0)),
                area = area
            };
        }

        void CreateController(Vector3 position)
        {
            var character = new GameObject("Agent");
            character.SetActive(false);
            character.transform.position = position;
            _created.Add(character);

            // Starts disabled, as Idle keeps it. Zero offset so the transform sits on the NavMesh
            // surface when the agent is placed there, which makes any snap visible.
            _navMeshAgent = character.AddComponent<NavMeshAgent>();
            _navMeshAgent.baseOffset = 0;
            _navMeshAgent.enabled = false;
            character.SetActive(true);

            var agent = new Agent(_navMeshAgent, NullNavMeshAgentModes.Instance, new AgentPriorityManager());
            _controller = new DestinationController(NullDestinationContext.Instance, agent);
        }
    }
}
