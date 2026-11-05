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
    // A character deactivated mid-trip (pooling, hiding NPCs) must give up its avoidance priority slot,
    // and carry on with its trip, holding a slot again, when reactivated. It used to keep the slot for
    // as long as it was inactive, because the state machine stops when DestinationScript stops updating.
    // These use DestinationScript and the shared SingleAgentPriorityManager pool, like a real scene.
    public class DestinationDeactivationTests
    {
        const float Speed = 3f;
        const float MovingSpeed = 0.5f;
        const float Timeout = 5f;

        // The shared pool: default reserves from the most important end, leisurely from the least.
        const int MostImportant = 0;
        const int NextMostImportant = 1;
        const int LeastImportant = 99;

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
        public IEnumerator Deactivating_mid_trip_frees_the_slot()
        {
            BuildNavMesh();
            var first = DestinationTestCharacter.Create(new Vector3(0, 0, -5), _created);
            var second = DestinationTestCharacter.Create(new Vector3(0, 0, 5), _created);
            yield return null;

            yield return StartTrip(first, new Vector3(20, 0, -5));
            Assert.AreEqual(MostImportant, Agent(first).avoidancePriority, "Setup failed: the shared pool wasn't empty.");

            first.gameObject.SetActive(false);

            yield return StartTrip(second, new Vector3(20, 0, 5));

            Assert.AreEqual(
                MostImportant,
                Agent(second).avoidancePriority,
                "The deactivated character should have given up its slot.");
        }

        // Guards that a reactivated character carries on, without ending up with two slots.
        [UnityTest]
        public IEnumerator Reactivating_mid_trip_resumes_the_trip_with_one_slot()
        {
            BuildNavMesh();
            var first = DestinationTestCharacter.Create(new Vector3(0, 0, -5), _created);
            var second = DestinationTestCharacter.Create(new Vector3(0, 0, 5), _created);
            var third = DestinationTestCharacter.Create(new Vector3(0, 0, 15), _created);
            yield return null;

            var firstResults = new List<DestinationResult>();
            yield return StartTrip(first, new Vector3(6, 0, -5), firstResults.Add);

            first.gameObject.SetActive(false);
            yield return null;
            first.gameObject.SetActive(true);

            // If the reactivated character held two slots, these wouldn't be the next free ones.
            yield return StartTrip(second, new Vector3(20, 0, 5));
            yield return StartTrip(third, new Vector3(20, 0, 15));

            CollectionAssert.AreEquivalent(
                new[] { MostImportant, NextMostImportant, NextMostImportant + 1 },
                new[] { Agent(first).avoidancePriority, Agent(second).avoidancePriority, Agent(third).avoidancePriority },
                "Three moving characters should hold three different slots, one each.");

            yield return WaitUntilOrTimeout(() => firstResults.Count > 0);

            Assert.AreEqual(1, firstResults.Count, "The reactivated character's trip never finished.");
            Assert.IsTrue(firstResults[0].IsAtDestination, "The reactivated character should carry on and arrive.");
        }

        // Guards that the slot taken back on reactivation is for the same mode.
        [UnityTest]
        public IEnumerator Reactivating_a_leisurely_trip_keeps_the_leisurely_priority()
        {
            BuildNavMesh();
            var character = DestinationTestCharacter.Create(Vector3.zero, _created);
            yield return null;

            yield return StartTrip(character, new Vector3(20, 0, 0), leisurely: true);
            Assert.AreEqual(LeastImportant, Agent(character).avoidancePriority, "Setup failed: wrong leisurely priority.");

            character.gameObject.SetActive(false);
            yield return null;
            character.gameObject.SetActive(true);
            yield return null;

            Assert.AreEqual(
                LeastImportant,
                Agent(character).avoidancePriority,
                "A reactivated leisurely trip should take a slot from the least important end again.");
        }

        IEnumerator StartTrip(
            DestinationScript character,
            Vector3 location,
            Action<DestinationResult> callback = null,
            bool leisurely = false)
        {
            var trip = DestinationArgs
                .GoToApproximately(location)
                .WithSpeed(Speed)
                .Callback(callback ?? (_ => { }));

            if (leisurely)
                trip.Leisurely();

            character.DestinationController.GoTo(trip.Build());
            yield return WaitUntilOrTimeout(() => character.DestinationController.CurrentVelocity.magnitude > MovingSpeed);

            Assert.Greater(
                character.DestinationController.CurrentVelocity.magnitude,
                MovingSpeed,
                $"Setup failed: '{character.name}' never started moving.");
        }

        void BuildNavMesh() =>
            _navMesh = TestNavMesh.Build(_created, TestNavMesh.Box(Vector3.zero, new Vector2(60, 60)));

        static NavMeshAgent Agent(DestinationScript character) => character.GetComponent<NavMeshAgent>();

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
