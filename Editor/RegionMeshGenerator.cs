using System.Collections.Generic;
using UnityEngine;

namespace ShoelaceStudios.GridSystem.Regions.Editor
{
    public static class RegionMeshGenerator
    {
        /// <summary>
        /// Generates a flat 2D mesh for the given region using its contained grid cells.
        /// </summary>
        public static Mesh GenerateRegionMesh(RegionDataSO region, WorldGridManager grid, bool useWorldUVs)
        {
            HashSet<Vector2Int> cells = new(region.ContainedCoords);
            float cellSize = grid.CellSize;

            // Collect vertices and triangles
            List<Vector3> vertices = new();
            List<int> triangles = new();
            List<Vector2> uvs = new();

            int vertOffset = 0;

            foreach (Vector2Int cell in cells)
            {
                Vector3 basePos = new(cell.x * cellSize, cell.y * cellSize, 0);

                // 4 corners of the cell quad
                Vector3 bl = basePos;
                Vector3 br = basePos + new Vector3(cellSize, 0, 0);
                Vector3 tr = basePos + new Vector3(cellSize, cellSize, 0);
                Vector3 tl = basePos + new Vector3(0, cellSize, 0);

                vertices.AddRange(new[] { bl, br, tr, tl });

                triangles.AddRange(new[]
                {
                    vertOffset, vertOffset + 1, vertOffset + 2,
                    vertOffset, vertOffset + 2, vertOffset + 3
                });

                if (useWorldUVs)
                {
                    // UVs mapped directly from world position
                    uvs.Add(new Vector2(bl.x, bl.y));
                    uvs.Add(new Vector2(br.x, br.y));
                    uvs.Add(new Vector2(tr.x, tr.y));
                    uvs.Add(new Vector2(tl.x, tl.y));
                }
                else
                {
                    // Local UVs per-cell (0–1 range)
                    uvs.Add(new Vector2(0, 0));
                    uvs.Add(new Vector2(1, 0));
                    uvs.Add(new Vector2(1, 1));
                    uvs.Add(new Vector2(0, 1));
                }

                vertOffset += 4;
            }

            Mesh mesh = new Mesh
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