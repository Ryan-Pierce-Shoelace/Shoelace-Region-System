using System.Collections.Generic;
using System.Linq;
using ShoelaceStudios.GridSystem;
using ShoelaceStudios.Utilities.Helpers;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShoelaceStudios.RegionSystem.Editor
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

			WorldGridManager grid = FindObjectOfType<WorldGridManager>();
			if (grid == null) return;

			Event ev = Event.current;
			Vector3 mouseWorld = GetMouseWorldPoint(ev);

			if (ev.type == EventType.Layout)
				HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

			foreach (RegionConnector c in connectorContainer.Connectors)
			{
				DrawConnectorGizmo(c);
			}

			foreach (RegionDataSO region in regionContainer.Regions)
			{
				DrawRegion(region, false);
			}

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
				Vector3[] verts = hoveredEdge.ToWorldVerts(grid.CellSize);
				Handles.color = Color.cyan;
				Handles.DrawLine(verts[0], verts[1], 4f);
				DrawRegion(hoveredRegion, true);

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
				switch (existing.Type)
				{
					case ConnectorType.Door:
						existing.Type = ConnectorType.Window;
						break;
					case ConnectorType.Window:
						connectors.Remove(existing);
						break;
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

			if (regionB == null)
				return regionB;

			GridEdge expected = new(neighborCell, Opposite(edgeA.Edge));

			if (regionB.PerimeterEdges.Contains(expected))
			{
				edgeB = expected;
			}

			return regionB;
		}

		private void RebuildAllConnectors()
		{
			if (connectorContainer == null || regionContainer == null)
				return;

			List<RegionConnector> existing = GetConnectorList();
			List<RegionConnector> rebuilt = new();

			foreach (RegionConnector c in existing)
			{
				if (c.RegionA == null)
					continue;

				if (!c.RegionA.ContainsCell(c.EdgeA.Cell))
					continue;

				if (!c.RegionA.PerimeterEdges.Contains(c.EdgeA))
					continue;

				Vector2Int neighborCell = GetNeighborCell(c.EdgeA);
				RegionDataSO regionB = GetRegionAtCell(neighborCell);
				GridEdge? matchingEdgeB = null;

				if (regionB != null)
				{
					GridEdge expected = new(neighborCell, Opposite(c.EdgeA.Edge));

					if (regionB.PerimeterEdges.Contains(expected))
					{
						matchingEdgeB = expected;
					}
				}

				RegionConnector updated = new(
					regionA: c.RegionA,
					edgeA: c.EdgeA,
					regionB: regionB,
					edgeB: matchingEdgeB ?? c.EdgeA,
					type: c.Type
				);

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
			RegionPainterWindow.Logic.DrawRegion(region, active);
		}

		private void DrawConnectorGizmo(RegionConnector c)
		{
			WorldGridManager grid = FindObjectOfType<WorldGridManager>();
			if (grid == null) return;

			Vector3[] aVerts = c.EdgeA.ToWorldVerts(grid.CellSize);
			Vector3[] bVerts = c.RegionB != null && c.EdgeB.HasValue
				? c.EdgeB.Value.ToWorldVerts(grid.CellSize)
				: new Vector3[] { aVerts[0], aVerts[1] };

			Handles.color = c.Type == ConnectorType.Door ? Color.yellow : Color.cyan;
			Handles.DrawLine(aVerts[0], bVerts[0], 2f);
			Handles.DrawLine(aVerts[1], bVerts[1], 2f);

			Vector3 mid = (aVerts[0] + aVerts[1]) * 0.5f;
			Handles.DrawSolidDisc(mid, Vector3.back, 0.5f);
		}


		#endregion

		#region Helpers

		private Vector3 GetMouseWorldPoint(Event e)
		{
			Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
			Plane plane = new(Vector3.forward, Vector3.zero);
			plane.Raycast(ray, out float dist);
			return ray.GetPoint(dist);
		}

		private bool TryGetEdgeUnderMouse(Vector3 worldPoint, RegionDataSO region, out GridEdge edge)
		{
			WorldGridManager grid = FindObjectOfType<WorldGridManager>();
			if (grid == null)
			{
				edge = default;
				return false;
			}

			float cellSize = grid.CellSize;
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
				case GridEdge.CellEdge.Top: return edge.Cell + Vector2Int.up;
				case GridEdge.CellEdge.Bottom: return edge.Cell + Vector2Int.down;
				case GridEdge.CellEdge.Right: return edge.Cell + Vector2Int.right;
				case GridEdge.CellEdge.Left: return edge.Cell + Vector2Int.left;
				default: return edge.Cell;
			}
		}

		private GridEdge.CellEdge Opposite(GridEdge.CellEdge dir)
		{
			switch (dir)
			{
				case GridEdge.CellEdge.Top: return GridEdge.CellEdge.Bottom;
				case GridEdge.CellEdge.Bottom: return GridEdge.CellEdge.Top;
				case GridEdge.CellEdge.Right: return GridEdge.CellEdge.Left;
				case GridEdge.CellEdge.Left: return GridEdge.CellEdge.Right;
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