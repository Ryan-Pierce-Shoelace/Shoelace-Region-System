using System.Collections.Generic;
using System.Linq;
using ShoelaceStudios.GridSystem;
using ShoelaceStudios.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShoelaceStudios.GridSystem.Regions.Editor
{
    public class RegionConnectorWindow : EditorWindow
    {
        private SceneRegionContainerSO regionContainer;
        private ConnectorContainerSO connectorContainer;

        private float edgeHoverTolerance = 0.1f;

        [MenuItem("Tools/Region Connector Editor")]
        public static void ShowWindow()
        {
            GetWindow<RegionConnectorWindow>("Region Connector Editor");
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            
            if (regionContainer == null)
	            regionContainer = SceneAssetHelper.GetOrCreateAsset<SceneRegionContainerSO>(SceneManager.GetActiveScene().name + "_Regions");

            if (connectorContainer == null)
	            connectorContainer = SceneAssetHelper.GetOrCreateAsset<ConnectorContainerSO>(SceneManager.GetActiveScene().name + "_Connectors");
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Region Connector Editor", EditorStyles.boldLabel);
            regionContainer = (SceneRegionContainerSO)EditorGUILayout.ObjectField("Region Container", regionContainer, typeof(SceneRegionContainerSO), false);
            connectorContainer = (ConnectorContainerSO)EditorGUILayout.ObjectField("Connector Container", connectorContainer, typeof(ConnectorContainerSO), false);

            edgeHoverTolerance = EditorGUILayout.Slider("Edge Hover Tolerance", edgeHoverTolerance, 0.01f, 0.5f);

            if (GUILayout.Button("Refresh Scene"))
            {
                RebuildAllConnectors();
                SceneView.RepaintAll();
            }
            
            GUILayout.Label("Yellow connectors are doors");
            GUILayout.Label("Cyan connectors are windows");
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (connectorContainer == null || regionContainer == null) return;

            Event ev = Event.current;
            Vector3 mouseWorld = GetMouseWorldPoint(ev);

            // Allow selection but override clicks
            if (ev.type == EventType.Layout)
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            

            // Draw connectors
            foreach (RegionConnector c in connectorContainer.Connectors.ToList())
            {
                DrawConnectorGizmo(c);
            }
            
            // Draw regions (fallback if painter not open)
            foreach (RegionDataSO region in regionContainer.Regions)
            {
	            DrawRegion(region, false);
            }

            // Hover detection
            RegionDataSO hoveredRegion = null;
            GridEdge hoveredEdge = default;

            foreach (RegionDataSO region in regionContainer.Regions)
            {
                if (TryGetEdgeUnderMouse(mouseWorld, region, out GridEdge edge))
                {
                    hoveredRegion = region;
                    hoveredEdge = edge;
                    break;
                }
            }

            if (hoveredRegion != null)
            {
                // Highlight edge + owning region
                Vector3[] verts = hoveredEdge.ToWorldVerts(WorldGridManager.Instance.CellSize);
                Handles.color = Color.cyan;
                Handles.DrawLine(verts[0], verts[1], 4f);
                DrawRegion(hoveredRegion, true);
                
                //DrawRegion(GetOppositeRegionData(hoveredEdge, out GridEdge? edgeB));
                
                

                if (ev.type == EventType.MouseDown && ev.button == 0)
                {
                    if (ev.shift)
                        CreateConnector(hoveredRegion, hoveredEdge, ConnectorType.Window);
                    else
                        CycleConnectorAtEdge(hoveredRegion, hoveredEdge);
                    ev.Use();
                }
            }
        }

        #region Connector Logic

        private void CycleConnectorAtEdge(RegionDataSO region, GridEdge edge)
        {
            List<RegionConnector> connectors = GetConnectorList();
            RegionConnector existing = connectors.FirstOrDefault(c => c.RegionA == region && c.EdgeA.Equals(edge));

            if (existing != null)
            {
                if (existing.Type == ConnectorType.Door)
                {
                    existing.Type = ConnectorType.Window;
                }
                else if (existing.Type == ConnectorType.Window)
                {
                    connectors.Remove(existing);
                }
            }
            else
            {
                CreateConnector(region, edge, ConnectorType.Door);
                return;
            }

            connectorContainer.Connectors = connectors;
            EditorUtility.SetDirty(connectorContainer);
            SceneView.RepaintAll();
        }

        private void CreateConnector(RegionDataSO regionA, GridEdge edgeA, ConnectorType type = ConnectorType.Door)
        {
            List<RegionConnector> connectors = GetConnectorList();
            if (connectors.Any(c => c.RegionA == regionA && c.EdgeA.Equals(edgeA)))
                return;

            
            RegionDataSO regionB = GetOppositeRegionData(edgeA, out GridEdge? edgeB);

            connectors.Add(new RegionConnector(regionA: regionA, edgeA: edgeA, regionB: regionB, edgeB: edgeB ?? edgeA, type: type));

            connectorContainer.Connectors = connectors;
            EditorUtility.SetDirty(connectorContainer);
            SceneView.RepaintAll();
        }

        private RegionDataSO GetOppositeRegionData(GridEdge edgeA, out GridEdge? edgeB)
        {
	        Vector2Int neighborCell = GetNeighborCell(edgeA);
	        RegionDataSO regionB = GetRegionAtCell(neighborCell);
	        edgeB = null;

	        if (regionB != null)
	        {
		        GridEdge expected = new GridEdge(neighborCell, Opposite(edgeA.Edge));
		        foreach (GridEdge pe in regionB.PerimeterEdges)
		        {
			        if (pe.Cell == expected.Cell && pe.Edge == expected.Edge)
			        {
				        edgeB = pe;
				        break;
			        }
		        }
	        }

	        return regionB;
        }

        /// <summary>
        /// Update the existing connectors list: validate each existing connector,
        /// update RegionB/EdgeB if neighbor changed, and drop invalid connectors.
        /// (previous behavior created connectors for every perimeter edge — fixed)
        /// </summary>
        private void RebuildAllConnectors()
        {
            if (connectorContainer == null || regionContainer == null) return;

            List<RegionConnector> existing = GetConnectorList();
            List<RegionConnector> rebuilt = new List<RegionConnector>();

            foreach (RegionConnector c in existing)
            {
                // Must have a valid RegionA reference
                if (c.RegionA == null) continue;

                // RegionA must still contain the original edge cell and the edge must still be a perimeter edge
                bool aHasCell = c.RegionA.ContainedCoords.Contains(c.EdgeA.Cell);
                bool aHasEdge = c.RegionA.PerimeterEdges.Contains(c.EdgeA);
                if (!aHasCell || !aHasEdge)
                {
                    // connector is no longer valid: drop it
                    continue;
                }

                // Attempt to find neighbor region & matching edge on the opposite side
                Vector2Int neighborCell = GetNeighborCell(c.EdgeA);
                RegionDataSO regionB = GetRegionAtCell(neighborCell);
                GridEdge? matchingEdgeB = null;

                if (regionB != null)
                {
                    GridEdge expected = new GridEdge(neighborCell, Opposite(c.EdgeA.Edge));
                    foreach (GridEdge pe in regionB.PerimeterEdges)
                    {
                        if (pe.Cell == expected.Cell && pe.Edge == expected.Edge)
                        {
                            matchingEdgeB = pe;
                            break;
                        }
                    }
                }

                // Preserve original type, update RegionB/EdgeB (or set RegionB=null and keep a visual placeholder)
                RegionConnector updated = new RegionConnector(regionA: c.RegionA, edgeA: c.EdgeA, regionB: regionB, edgeB: matchingEdgeB ?? c.EdgeA, type: c.Type);

                rebuilt.Add(updated);
            }

            connectorContainer.Connectors = rebuilt;
            EditorUtility.SetDirty(connectorContainer);
            SceneView.RepaintAll();
        }

        private List<RegionConnector> GetConnectorList()
        {
            if (connectorContainer.Connectors == null)
                connectorContainer.Connectors = new List<RegionConnector>();
            return connectorContainer.Connectors;
        }

        #endregion

        #region Drawing

        private void DrawRegion(RegionDataSO region, bool active)
        {
            if (RegionPainterWindow.Logic == null) return;

            // Ask the painter logic to render the region with "active" flag so it uses the same visuals
            RegionPainterWindow.Logic.DrawRegion(region, active);
        }

        private void DrawConnectorGizmo(RegionConnector c)
        {
            Vector3[] aVerts = c.EdgeA.ToWorldVerts(WorldGridManager.Instance.CellSize);
            Vector3[] bVerts = c.RegionB != null ? c.EdgeB?.ToWorldVerts(WorldGridManager.Instance.CellSize) : new Vector3[] { aVerts[0], aVerts[1] };

            Handles.color = c.Type == ConnectorType.Door ? Color.yellow : Color.cyan;
            Handles.DrawLine(aVerts[0], bVerts[0], 2f);
            Handles.DrawLine(aVerts[1], bVerts[1], 2f);

            Vector3 mid = (aVerts[0] + aVerts[1]) * 0.5f;

            Handles.DrawSolidDisc(mid, Vector3.back, .5f);
        }

        #endregion

        #region Helpers

        private Vector3 GetMouseWorldPoint(Event e)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Plane plane = new Plane(Vector3.forward, Vector3.zero);
            plane.Raycast(ray, out float dist);
            return ray.GetPoint(dist);
        }

        private bool TryGetEdgeUnderMouse(Vector3 worldPoint, RegionDataSO region, out GridEdge edge)
        {
            float cellSize = WorldGridManager.Instance.CellSize;
            float scaledTolerance = edgeHoverTolerance * cellSize;

            foreach (GridEdge e in region.PerimeterEdges)
            {
                Vector3[] verts = e.ToWorldVerts(cellSize);
                Vector3 closest = ClosestPointOnLineSegment(worldPoint, verts[0], verts[1]);
                if ((worldPoint - closest).magnitude < scaledTolerance)
                {
                    edge = e;
                    return true;
                }
            }

            edge = default;
            return false;
        }

        private Vector3 ClosestPointOnLineSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float t = Vector3.Dot(point - a, ab) / ab.sqrMagnitude;
            t = Mathf.Clamp01(t);
            return a + ab * t;
        }

        private Vector2Int GetNeighborCell(GridEdge edge)
        {
            switch (edge.Edge)
            {
                case GridEdge.CellEdge.Top:    return edge.Cell + Vector2Int.up;
                case GridEdge.CellEdge.Bottom: return edge.Cell + Vector2Int.down;
                case GridEdge.CellEdge.Right:  return edge.Cell + Vector2Int.right;
                case GridEdge.CellEdge.Left:   return edge.Cell + Vector2Int.left;
                default: return edge.Cell;
            }
        }

        private GridEdge.CellEdge Opposite(GridEdge.CellEdge dir)
        {
            switch (dir)
            {
                case GridEdge.CellEdge.Top:    return GridEdge.CellEdge.Bottom;
                case GridEdge.CellEdge.Bottom: return GridEdge.CellEdge.Top;
                case GridEdge.CellEdge.Right:  return GridEdge.CellEdge.Left;
                case GridEdge.CellEdge.Left:   return GridEdge.CellEdge.Right;
                default: return dir;
            }
        }

        private RegionDataSO GetRegionAtCell(Vector2Int cell)
        {
            return regionContainer.Regions.FirstOrDefault(r => r.ContainsCell(cell));
        }

        #endregion
    }
}
