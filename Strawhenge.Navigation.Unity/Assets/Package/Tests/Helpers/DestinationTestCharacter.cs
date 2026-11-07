using Strawhenge.Navigation.Unity.Destination;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace Strawhenge.Navigation.Unity.Tests
{
    static class DestinationTestCharacter
    {
        // Builds a character with a NavMeshAgent and DestinationScript. It is built inactive and set up in
        // full before Awake runs, so Awake doesn't log the "not set" warnings meant for scene setup. Every
        // area is walkable in both modes, and the destination settings are the defaults. Adds it to
        // created for tear-down.
        public static DestinationScript Create(Vector3 position, ICollection<Object> created)
        {
            var character = new GameObject($"Character {created.Count}");
            character.SetActive(false);
            character.transform.position = position;
            created.Add(character);

            // Starts disabled, as Idle keeps it; PrepareGoing enables it.
            var navMeshAgent = character.AddComponent<NavMeshAgent>();
            navMeshAgent.baseOffset = 0;
            navMeshAgent.enabled = false;

            var destination = character.AddComponent<DestinationScript>();
            destination.SetNavMeshAgent(navMeshAgent);
            destination.SetModes(new FakeNavMeshAgentModes(NavMesh.AllAreas, NavMesh.AllAreas));
            destination.SetSettings(DefaultDestinationSettings.Instance);
            destination.SetLogger(new FakeLogger());

            character.SetActive(true);
            return destination;
        }
    }
}
