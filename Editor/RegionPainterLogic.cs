using System.Collections.Generic;
using ShoelaceStudios.GridSystem;
using UnityEditor;
using UnityEngine;

namespace ShoelaceStudios.RegionSystem.Editor
{
    public class RegionPainterLogic
    {
        private RegionPainterWindow editorWindow;

        public RegionPainterLogic(RegionPainterWindow regionPainterWindow)
        {
            editorWindow = regionPainterWindow;
        }

        public void DrawRegion(RegionDataSO region, bool isActive)
        {
            DrawRegionCells(region, isActive);
            DrawIslandWarnings(region);
        }

        private void DrawRegionCells(RegionDataSO region, bool isActive)
        {
            WorldGridManager grid = WorldGridManager.Instance;
            if (grid == null) return;

            Color drawFill = new Color(
                region.RegionColor.r, 
                region.RegionColor.g, 
                region.RegionColor.b, 
                isActive ? 0.5f : 0.15f);
            Color drawOutline = new Color(
                region.RegionColor.r, 
                region.RegionColor.g, 
                region.RegionColor.b, 
                0.7f);

            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            foreach (Vector2Int cell in region.ContainedCoords)
            {
                Vector3 cellWorld = grid.CellToWorldSpace(cell);
                float size = grid.CellSize;

                Handles.DrawSolidRectangleWithOutline(
                    new[]
                    {
                        cellWorld + new Vector3(-0.5f, -0.5f, 0) * size,
                        cellWorld + new Vector3(0.5f, -0.5f, 0) * size,
                        cellWorld + new Vector3(0.5f, 0.5f, 0) * size,
                        cellWorld + new Vector3(-0.5f, 0.5f, 0) * size
                    },
                    drawFill,
                    drawOutline
                );
            }

            DrawRegionEdges(region, isActive);
        }

        private void DrawRegionEdges(RegionDataSO region, bool isActive)
        {
            WorldGridManager grid = WorldGridManager.Instance;
            if (grid == null) return;

            Color edgeColor = isActive ? Color.yellow : region.RegionColor;
            edgeColor.a = 1f;

            Handles.color = edgeColor;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

            float thickness = grid.CellSize * (isActive ? 0.5f : 0.2f);

            foreach (GridEdge edge in region.PerimeterEdges)
            {
                Vector3[] verts = edge.ToWorldVerts(grid.CellSize);
                Handles.DrawLine(verts[0], verts[1], thickness);
            }
        }

        public void DrawIslandWarnings(RegionDataSO region)
        {
            WorldGridManager grid = WorldGridManager.Instance;
            if (grid == null) return;

            List<HashSet<Vector2Int>> islands = GetIslands(region.ContainedCoords);
            if (islands.Count <= 1) return;

            foreach (HashSet<Vector2Int> island in islands)
            {
                Vector2 center = Vector2.zero;
                foreach (Vector2Int c in island) center += (Vector2)c;
                center /= island.Count;

                Vector3 worldCenter = grid.CellToWorldSpace(Vector2Int.RoundToInt(center));
                float size = grid.CellSize * 0.5f;

                Handles.color = Color.yellow;
                Handles.DrawWireDisc(worldCenter, Vector3.forward, size);
                Handles.Label(worldCenter + Vector3.up * size, "⚠ ", new GUIStyle { fontSize = 16 });
            }
        }

        public void DrawHoverHighlight(Vector2Int coord, bool addMode)
        {
            WorldGridManager grid = WorldGridManager.Instance;
            if (grid == null) return;

            Vector3 worldCenter = grid.CellToWorldSpace(coord);
            float size = grid.CellSize;

            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            Handles.color = addMode ? Color.green : Color.red;
            Handles.DrawWireCube(worldCenter, Vector3.one * size * 0.95f);
        }

        public void HandlePenMode(Event e, int controlID, Vector2Int coord, RegionDataSO region, bool addMode, bool overwrite)
        {
            EventType eventType = e.GetTypeForControl(controlID);

            if (eventType == EventType.MouseDown || eventType == EventType.MouseDrag)
            {
                if (e.button == 0 && !e.alt)
                {
                    if (eventType == EventType.MouseDown)
                        GUIUtility.hotControl = controlID;

                    ApplyToRegion(region, new List<Vector2Int> { coord }, addMode, overwrite, editorWindow.Container);
                    e.Use();
                }
            }
            else if (eventType == EventType.MouseUp && GUIUtility.hotControl == controlID)
            {
                GUIUtility.hotControl = 0;
                e.Use();
            }
        }

        public void HandleRectMode(Event e, int controlID, Vector2Int coord, RegionDataSO region, bool addMode, bool overwrite, ref Vector2Int? rectStart)
        {
            EventType eventType = e.GetTypeForControl(controlID);

            if (eventType == EventType.MouseDown && e.button == 0 && !e.alt && !rectStart.HasValue)
            {
                GUIUtility.hotControl = controlID;
                rectStart = coord;
                e.Use();
                return;
            }

            if (rectStart.HasValue)
            {
                Vector2Int start = rectStart.Value;
                RectInt rect = MakeRect(start, coord);

                Handles.color = new Color(0, 1, 0, 0.2f);
                Handles.DrawSolidRectangleWithOutline(RectToWorldVerts(rect), new Color(0, 1, 0, 0.1f), Color.green);

                if (eventType == EventType.MouseUp && e.button == 0)
                {
                    List<Vector2Int> coords = new List<Vector2Int>();
                    for (int x = rect.xMin; x < rect.xMax; x++)
                    {
                        for (int y = rect.yMin; y < rect.yMax; y++)
                        {
                            coords.Add(new Vector2Int(x, y));
                        }
                    }

                    ApplyToRegion(region, coords, addMode, overwrite, editorWindow.Container);
                    
                    GUIUtility.hotControl = 0;
                    rectStart = null;
                    e.Use();
                }
            }
        }

        private void ApplyToRegion(RegionDataSO region, List<Vector2Int> coords, bool addMode, bool overwrite, SceneRegionContainerSO container)
        {
            WorldGridManager grid = WorldGridManager.Instance;
            if (grid == null) return;

            HashSet<Vector2Int> contained = new HashSet<Vector2Int>(region.ContainedCoords);

            foreach (Vector2Int coord in coords)
            {
                if (!grid.IsValidCell(coord)) continue;

                if (addMode)
                {
                    RegionDataSO existing = null;

                    foreach (RegionDataSO r in container.Regions)
                    {
                        if (r.ContainsCell(coord))
                        {
                            existing = r;
                            break;
                        }
                    }

                    if (!overwrite)
                    {
                        if (existing != null && existing != region) continue;
                    }
                    else
                    {
                        if (existing != null && existing != region)
                        {
                            HashSet<Vector2Int> otherCoords = new HashSet<Vector2Int>(existing.ContainedCoords);
                            otherCoords.Remove(coord);
                            HashSet<GridEdge> otherPerimeter = RegionUtility.CalculatePerimeterEdges(otherCoords);
                            existing.SetCoords(otherCoords, otherPerimeter);
                            EditorUtility.SetDirty(existing);
                        }
                    }

                    contained.Add(coord);
                }
                else
                {
                    contained.Remove(coord);
                }
            }

            HashSet<GridEdge> perimeter = RegionUtility.CalculatePerimeterEdges(contained);
            region.SetCoords(contained, perimeter);

            EditorUtility.SetDirty(region);
            if (container != null) EditorUtility.SetDirty(container);
        }

        public Vector3 GetMouseWorldPoint(Event e)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Plane plane = new Plane(Vector3.forward, Vector3.zero);
            if (!plane.Raycast(ray, out float dist)) return Vector3.zero;
            return ray.GetPoint(dist);
        }

        public bool HasMultipleIslands(IEnumerable<Vector2Int> coords)
        {
            return GetIslands(coords).Count > 1;
        }

        public List<HashSet<Vector2Int>> GetIslands(IEnumerable<Vector2Int> coords)
        {
            List<HashSet<Vector2Int>> islands = new List<HashSet<Vector2Int>>();
            HashSet<Vector2Int> remaining = new HashSet<Vector2Int>(coords);

            while (remaining.Count > 0)
            {
                Queue<Vector2Int> queue = new Queue<Vector2Int>();
                HashSet<Vector2Int> island = new HashSet<Vector2Int>();
                Vector2Int start = default;
                foreach (Vector2Int c in remaining)
                {
                    start = c;
                    break;
                }

                queue.Enqueue(start);
                island.Add(start);
                remaining.Remove(start);

                while (queue.Count > 0)
                {
                    Vector2Int current = queue.Dequeue();
                    foreach (Vector2Int n in new Vector2Int[]
                    {
                        new Vector2Int(current.x + 1, current.y),
                        new Vector2Int(current.x - 1, current.y),
                        new Vector2Int(current.x, current.y + 1),
                        new Vector2Int(current.x, current.y - 1)
                    })
                    {
                        if (remaining.Contains(n))
                        {
                            queue.Enqueue(n);
                            island.Add(n);
                            remaining.Remove(n);
                        }
                    }
                }

                islands.Add(island);
            }

            return islands;
        }

        private RectInt MakeRect(Vector2Int a, Vector2Int b)
        {
            int xMin = Mathf.Min(a.x, b.x);
            int yMin = Mathf.Min(a.y, b.y);
            int xMax = Mathf.Max(a.x, b.x);
            int yMax = Mathf.Max(a.y, b.y);
            return new RectInt(xMin, yMin, xMax - xMin + 1, yMax - yMin + 1);
        }

        private Vector3[] RectToWorldVerts(RectInt rect)
        {
            WorldGridManager grid = WorldGridManager.Instance;
            if (grid == null) return new Vector3[4];

            float size = grid.CellSize;
            return new Vector3[]
            {
                new Vector3(rect.xMin, rect.yMin, 0) * size,
                new Vector3(rect.xMax, rect.yMin, 0) * size,
                new Vector3(rect.xMax, rect.yMax, 0) * size,
                new Vector3(rect.xMin, rect.yMax, 0) * size
            };
        }
    }
}