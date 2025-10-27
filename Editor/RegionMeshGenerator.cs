using System.Collections.Generic;
using System.Linq;
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

            Vector2Int min = region.ContainedCoords.Min();
            Vector2Int max = region.ContainedCoords.Max();
            
            int width = max.x - min.x;
            int height = max.y - min.y;
            

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
                    vertOffset, vertOffset + 2, vertOffset + 1,
                    vertOffset, vertOffset + 3, vertOffset + 2
                });

                if (useWorldUVs)
                {
                    // UVs mapped directly from world position
                    uvs.Add(new Vector2(bl.x, bl.y)/ grid.TotalWorldSize);
                    uvs.Add(new Vector2(br.x, br.y)/ grid.TotalWorldSize);
                    uvs.Add(new Vector2(tr.x, tr.y)/ grid.TotalWorldSize);
                    uvs.Add(new Vector2(tl.x, tl.y)/ grid.TotalWorldSize);
                }
                else
                {
                    // Local UVs per-cell (0–1 range)
                    uvs.Add(new Vector2((bl.x - min.x) / width, (bl.y - min.y) / height));
                    uvs.Add(new Vector2((br.x - min.x) / width, (br.y - min.y) / height));
                    uvs.Add(new Vector2((tr.x - min.x) / width, (tr.y - min.y) / height));
                    uvs.Add(new Vector2((tl.x - min.x) / width, (tl.y - min.y) / height));
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