using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShoelaceStudios.GridSystem.Regions.Editor
{
    public static class RegionMeshGenerator
    {
        /// <summary>
        /// Generates a single polygon mesh representing the region's island shape.
        /// </summary>
        public static Mesh GenerateRegionMesh(RegionDataSO region, WorldGridManager grid, bool useWorldUVs)
        {
            float cellSize = grid.CellSize;

            // 1️⃣ Build perimeter edges from the region’s cells
            HashSet<Vector2Int> cellSet = new(region.ContainedCoords);
            HashSet<GridEdge> edges = RegionUtility.CalculatePerimeterEdges(cellSet);

            // 2️⃣ Convert edges to polygon vertices (ordered)
            List<Vector2> polygon = BuildOrderedPolygon(edges, cellSize);
            if (polygon.Count < 3)
            {
                Debug.LogWarning($"Region {region.RegionName} has too few points for a polygon mesh.");
                return new Mesh();
            }

            // 3️⃣ Triangulate polygon
            int[] triangles = Triangulator.Triangulate(polygon);

            // 4️⃣ Build mesh data
            List<Vector3> vertices = polygon.Select(v => new Vector3(v.x, v.y, 0)).ToList();
            List<Vector2> uvs = new();

            if (useWorldUVs)
            {
                uvs.AddRange(vertices.Select(v => new Vector2(v.x, v.y)));
            }
            else
            {
                // Normalize local-space UVs around polygon bounds
                var bounds = new Bounds(vertices[0], Vector3.zero);
                foreach (var v in vertices) bounds.Encapsulate(v);
                foreach (var v in vertices)
                {
                    Vector3 local = v - bounds.min;
                    uvs.Add(new Vector2(local.x / bounds.size.x, local.y / bounds.size.y));
                }
            }

            // 5️⃣ Assemble mesh
            Mesh mesh = new Mesh
            {
                name = region.RegionName + "_Mesh",
                vertices = vertices.ToArray(),
                triangles = triangles,
                uv = uvs.ToArray()
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            return mesh;
        }

        /// <summary>
        /// Orders perimeter edges into a clockwise polygon vertex loop.
        /// </summary>
        private static List<Vector2> BuildOrderedPolygon(HashSet<GridEdge> edges, float cellSize)
        {
            if (edges.Count == 0) return new List<Vector2>();

            // Build a map of connected points
            Dictionary<Vector2, List<Vector2>> adjacency = new();
            foreach (var edge in edges)
            {
                Vector3[] worldVerts = edge.ToWorldVerts(cellSize);
                Vector2 a = worldVerts[0];
                Vector2 b = worldVerts[1];

                if (!adjacency.ContainsKey(a)) adjacency[a] = new List<Vector2>();
                if (!adjacency.ContainsKey(b)) adjacency[b] = new List<Vector2>();

                adjacency[a].Add(b);
                adjacency[b].Add(a);
            }

            // Find a starting point (bottom-leftmost)
            Vector2 start = adjacency.Keys.OrderBy(p => p.x + p.y * 10000).First();

            List<Vector2> ordered = new();
            ordered.Add(start);

            Vector2 current = start;
            Vector2? previous = null;

            while (true)
            {
                List<Vector2> neighbors = adjacency[current];
                Vector2 next = neighbors.FirstOrDefault(n => n != previous);

                if (next == Vector2.zero) break;
                ordered.Add(next);

                previous = current;
                current = next;

                if (next == start) break; // closed loop
            }

            return ordered;
        }
    }

    /// <summary>
    /// Simple 2D polygon triangulator (ear clipping)
    /// </summary>
    internal static class Triangulator
    {
        public static int[] Triangulate(List<Vector2> points)
        {
            if (points.Count < 3) return new int[0];

            List<int> indices = new List<int>();
            List<int> verts = Enumerable.Range(0, points.Count).ToList();

            int counter = 0;
            while (verts.Count > 2 && counter < 10000)
            {
                counter++;
                bool earFound = false;

                for (int i = 0; i < verts.Count; i++)
                {
                    int prev = verts[(i - 1 + verts.Count) % verts.Count];
                    int curr = verts[i];
                    int next = verts[(i + 1) % verts.Count];

                    Vector2 a = points[prev];
                    Vector2 b = points[curr];
                    Vector2 c = points[next];

                    if (!IsConvex(a, b, c))
                        continue;

                    bool hasPointInside = false;
                    for (int j = 0; j < verts.Count; j++)
                    {
                        if (j == prev || j == curr || j == next) continue;
                        if (PointInTriangle(points[verts[j]], a, b, c))
                        {
                            hasPointInside = true;
                            break;
                        }
                    }

                    if (hasPointInside) continue;

                    indices.Add(prev);
                    indices.Add(curr);
                    indices.Add(next);

                    verts.RemoveAt(i);
                    earFound = true;
                    break;
                }

                if (!earFound) break;
            }

            return indices.ToArray();
        }

        private static bool IsConvex(Vector2 a, Vector2 b, Vector2 c)
        {
            return ((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) < 0f; // Clockwise
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float area = 0.5f * (-b.y * c.x + a.y * (-b.x + c.x) + a.x * (b.y - c.y) + b.x * c.y);
            float s = 1f / (2f * area) * (a.y * c.x - a.x * c.y + (c.y - a.y) * p.x + (a.x - c.x) * p.y);
            float t = 1f / (2f * area) * (a.x * b.y - a.y * b.x + (a.y - b.y) * p.x + (b.x - a.x) * p.y);
            return s >= 0 && t >= 0 && (s + t) <= 1;
        }
    }
}
