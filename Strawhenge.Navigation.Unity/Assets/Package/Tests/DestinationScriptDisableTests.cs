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
    // Disabling only the DestinationScript component mid-trip must pause the trip, as deactivating the
    // GameObject does: the character stops and gives up its avoidance priority slot, and carries on when
    // the script is enabled again. It used to keep walking with nothing ticking the controller, still
    // using the priority number it had given up.
    // These use DestinationScript and the shared SingleAgentPriorityManager pool, like a real scene.
    public class DestinationScriptDisableTests
    {
        const float Speed = 3f;
        const float MovingSpeed = 0.5f;
        const float Timeout = 5f;
        const float PauseTime = 0.5f;
        const float StoppedTolerance = 0.05f;

        // The shared pool: default reserves from the most important end.
        const int MostImportant = 0;
        const int NextMostImportant = 1;

        readonly List<Object> _created = new();
        NavMeshDataInstance _navMesh;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_navMesh.valid)
                _navMesh.Remove();

            // Destroyed agents count as free slots in the shared pool, so nothing is left behind.
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
        public IEnumerator Disabling_mid_trip_stops_the_character()
        {
            BuildNavMesh();
            var character = CreateCharacter(Vector3.zero);
            yield return null;

            yield return StartTrip(character, new Vector3(20, 0, 0));

            character.enabled = false;
            yield return null;
            var position = character.transform.position;

            yield return Wait(PauseTime);

            Assert.Less(
                Vector3.Distance(position, character.transform.position),
                StoppedTolerance,
                $"The character kept moving from {position} to {character.transform.position} while its DestinationScript was disabled.");
        }

        [UnityTest]
        public IEnumerator Disabled_character_does_not_share_a_priority_with_a_moving_one()
        {
            BuildNavMesh();
            var first = CreateCharacter(new Vector3(0, 0, -5));
            var second = CreateCharacter(new Vector3(0, 0, 5));
            yield return null;

            yield return StartTrip(first, new Vector3(20, 0, -5));
            Assert.AreEqual(MostImportant, Agent(first).avoidancePriority, "Setup failed: the shared pool wasn't empty.");

            first.enabled = false;

            yield return StartTrip(second, new Vector3(20, 0, 5));

            var activePriorities = new[] { first, second }
                .Select(Agent)
                .Where(agent => agent.isActiveAndEnabled)
                .Select(agent => agent.avoidancePriority)
                .ToArray();

            CollectionAssert.AllItemsAreUnique(
                activePriorities,
                "A disabled character's agent is still active with the priority it gave up, which the moving one now holds.");
        }

        // Guards that a re-enabled character carries on with its trip and is called back once.
        [UnityTest]
        public IEnumerator Reenabling_mid_trip_resumes_the_trip()
        {
            BuildNavMesh();
            var character = CreateCharacter(Vector3.zero);
            yield return null;

            var results = new List<DestinationResult>();
            yield return StartTrip(character, new Vector3(8, 0, 0), results.Add);

            character.enabled = false;
            yield return Wait(PauseTime);
            character.enabled = true;

            yield return WaitUntilOrTimeout(() => results.Count > 0);

            Assert.AreEqual(1, results.Count, "The re-enabled character's trip never finished.");
            Assert.IsTrue(results[0].IsAtDestination, "The re-enabled character should carry on and arrive.");
        }

        // Guards that a re-enabled character takes back exactly one slot.
        [UnityTest]
        public IEnumerator Reenabling_mid_trip_takes_one_slot()
        {
            BuildNavMesh();
            var first = CreateCharacter(new Vector3(0, 0, -5));
            var second = CreateCharacter(new Vector3(0, 0, 5));
            var third = CreateCharacter(new Vector3(0, 0, 15));
            yield return null;

            yield return StartTrip(first, new Vector3(20, 0, -5));

            first.enabled = false;
            yield return null;
            first.enabled = true;

            yield return StartTrip(second, new Vector3(20, 0, 5));
            yield return StartTrip(third, new Vector3(20, 0, 15));

            CollectionAssert.AreEquivalent(
                new[] { MostImportant, NextMostImportant, NextMostImportant + 1 },
                new[] { Agent(first).avoidancePriority, Agent(second).avoidancePriority, Agent(third).avoidancePriority },
                "Three moving characters should hold three different slots, one each.");
        }

        // Guards that disabling and enabling an idle character leaves it able to navigate.
        [UnityTest]
        public IEnumerator Disabling_and_enabling_while_idle_still_navigates()
        {
            BuildNavMesh();
            var character = CreateCharacter(Vector3.zero);
            yield return null;

            character.enabled = false;
            yield return null;
            character.enabled = true;
            yield return null;

            Assert.IsFalse(Agent(character).enabled, "An idle character's NavMeshAgent should stay disabled.");

            var results = new List<DestinationResult>();
            yield return StartTrip(character, new Vector3(5, 0, 0), results.Add);
            yield return WaitUntilOrTimeout(() => results.Count > 0);

            Assert.AreEqual(1, results.Count, "The trip never finished.");
            Assert.IsTrue(results[0].IsAtDestination, "The character should arrive.");
        }

        IEnumerator StartTrip(DestinationScript character, Vector3 location, Action<DestinationResult> callback = null)
        {
            var trip = DestinationArgs
                .GoToApproximately(location)
                .WithSpeed(Speed)
                .Callback(callback ?? (_ => { }))
                .Build();

            character.DestinationController.GoTo(trip);
            yield return WaitUntilOrTimeout(() => character.DestinationController.CurrentVelocity.magnitude > MovingSpeed);

            Assert.Greater(
                character.DestinationController.CurrentVelocity.magnitude,
                MovingSpeed,
                $"Setup failed: '{character.name}' never started moving.");
        }

        void BuildNavMesh() =>
            _navMesh = TestNavMesh.Build(_created, TestNavMesh.Box(Vector3.zero, new Vector2(60, 60)));

        DestinationScript CreateCharacter(Vector3 position)
        {
            var character = new GameObject($"Character {_created.Count}");
            character.SetActive(false);
            character.transform.position = position;
            _created.Add(character);

            // Starts disabled, as Idle keeps it; PrepareGoing enables it.
            var navMeshAgent = character.AddComponent<NavMeshAgent>();
            navMeshAgent.baseOffset = 0;
            navMeshAgent.enabled = false;

            var destination = character.AddComponent<DestinationScript>();
            character.SetActive(true);

            return destination;
        }

        static NavMeshAgent Agent(DestinationScript character) => character.GetComponent<NavMeshAgent>();

        static IEnumerator Wait(float seconds)
        {
            var elapsed = 0f;

            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        static IEnumerator WaitUntilOrTimeout(Func<bool> condition)
        {
            var elapsed = 0f;

            while (!condition() && elapsed < Timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }
}
