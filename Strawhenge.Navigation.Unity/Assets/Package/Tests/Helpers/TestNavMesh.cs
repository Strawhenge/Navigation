using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace Strawhenge.Navigation.Unity.Tests
{
    // Builds a NavMesh in code from flat walkable boxes, so tests need no scene geometry.
    static class TestNavMesh
    {
        const float Thickness = 0.1f;

        // Builds a NavMesh from the given boxes and adds it. Adds the NavMesh data to created for
        // tear-down; remove the returned instance in tear-down too.
        public static NavMeshDataInstance Build(ICollection<Object> created, params NavMeshBuildSource[] boxes)
        {
            var data = NavMeshBuilder.BuildNavMeshData(
                NavMesh.GetSettingsByID(0),
                new List<NavMeshBuildSource>(boxes),
                new Bounds(Vector3.zero, new Vector3(300, 20, 300)),
                Vector3.zero,
                Quaternion.identity);
            created.Add(data);

            return NavMesh.AddNavMeshData(data);
        }

        // A thin box whose top surface is walkable, at center.y.
        public static NavMeshBuildSource Box(Vector3 center, Vector2 size, int area = 0) =>
            new()
            {
                shape = NavMeshBuildSourceShape.Box,
                size = new Vector3(size.x, Thickness, size.y),
                transform = Matrix4x4.Translate(center - new Vector3(0, Thickness / 2, 0)),
                area = area
            };
    }
}
