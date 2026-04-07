using System.Collections.Generic;
using ShoelaceStudios.GridSystem;
using ShoelaceStudios.GridSystem.Core;
using UnityEngine;

namespace ShoelaceStudios.RegionSystem.Editor
{
    public static class RegionMeshGenerator
    {
        /// <summary>
        /// Generates a flat 2D mesh for the given region using its contained grid cells.
        /// </summary>
        public static Mesh GenerateRegionMesh(RegionDataSO region, WorldGridManager grid, bool useWorldUVs)
        {
            if (region.ContainedCoords.Count == 0)
            {
                Debug.LogWarning($"Region '{region.RegionName}' has no cells. Cannot generate mesh.");
                return null;
            }

            HashSet<Vector2Int> cells = new(region.ContainedCoords);
            float cellSize = grid.CellSize;

            List<Vector3> vertices = new();
            List<int> triangles = new();
            List<Vector2> uvs = new();

            int vertOffset = 0;

            Vector2Int min = region.BoundsMin;
            Vector2Int max = region.BoundsMax;

            int width = max.x - min.x;
            int height = max.y - min.y;

            if (width == 0) width = 1;
            if (height == 0) height = 1;

            foreach (Vector2Int cell in cells)
            {
                Vector3 basePos = new(cell.x * cellSize, cell.y * cellSize, 0);

                Vector3 bl = basePos;
                Vector3 br = basePos + new Vector3(cellSize, 0, 0);
                Vector3 tr = basePos + new Vector3(cellSize, cellSize, 0);
                Vector3 tl = basePos + new Vector3(0, cellSize, 0);

                vertices.AddRange(new[] { bl, br, tr, tl });

                triangles.AddRange(new[]
                {
                    vertOffset,  vertOffset + 3, vertOffset + 1,
                    vertOffset + 3,  vertOffset + 2, vertOffset + 1
                });

                if (useWorldUVs)
                {
                    Vector2 worldSize = grid.GetGridWorldSize();
                    uvs.Add(new Vector2(bl.x / worldSize.x, bl.y / worldSize.y));
                    uvs.Add(new Vector2(br.x / worldSize.x, br.y / worldSize.y));
                    uvs.Add(new Vector2(tr.x / worldSize.x, tr.y / worldSize.y));
                    uvs.Add(new Vector2(tl.x / worldSize.x, tl.y / worldSize.y));
                }
                else
                {
                    float minX = min.x * cellSize;
                    float minY = min.y * cellSize;
                    float widthWorld = width * cellSize;
                    float heightWorld = height * cellSize;

                    uvs.Add(new Vector2((bl.x - minX) / widthWorld, (bl.y - minY) / heightWorld));
                    uvs.Add(new Vector2((br.x - minX) / widthWorld, (br.y - minY) / heightWorld));
                    uvs.Add(new Vector2((tr.x - minX) / widthWorld, (tr.y - minY) / heightWorld));
                    uvs.Add(new Vector2((tl.x - minX) / widthWorld, (tl.y - minY) / heightWorld));
                }

                vertOffset += 4;
            }

            Mesh mesh = new()
            {
                name = region.RegionName + "_Mesh",
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray(),
                uv = uvs.ToArray()
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            return mesh;
        }
    }
}