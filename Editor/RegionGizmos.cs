#if UNITY_EDITOR
using System.Collections.Generic;
using ShoelaceStudios.GridSystem;
using ShoelaceStudios.GridSystem.Core;
using UnityEditor;
using UnityEngine;

namespace ShoelaceStudios.RegionSystem.Editor
{
	public static class RegionGizmos
	{
		public static void DrawRegion(RegionDataSO region, bool isActive)
		{
			DrawRegionCells(region, isActive);
			DrawRegionEdges(region, isActive);
			DrawIslandWarnings(region);
		}

		private static void DrawRegionCells(RegionDataSO region, bool isActive)
		{
			WorldGridManager gridManager = Object.FindObjectOfType<WorldGridManager>();
			if (gridManager == null) return;

			Color fillColor = new Color(
				region.RegionColor.r,
				region.RegionColor.g,
				region.RegionColor.b,
				isActive ? 0.5f : 0.15f
			);

			Color outlineColor = new Color(
				region.RegionColor.r,
				region.RegionColor.g,
				region.RegionColor.b,
				0.7f
			);

			Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

			foreach (Vector2Int cell in region.ContainedCoords)
			{
				Vector3 cellWorld = gridManager.GetWorldFromCell(cell);
				float size = gridManager.CellSize;

				Vector3[] verts = new Vector3[]
				{
					cellWorld + new Vector3(-0.5f, -0.5f, 0) * size,
					cellWorld + new Vector3(0.5f, -0.5f, 0) * size,
					cellWorld + new Vector3(0.5f, 0.5f, 0) * size,
					cellWorld + new Vector3(-0.5f, 0.5f, 0) * size
				};

				Handles.DrawSolidRectangleWithOutline(verts, fillColor, outlineColor);
			}
		}

		private static void DrawRegionEdges(RegionDataSO region, bool isActive)
		{
			WorldGridManager gridManager = Object.FindObjectOfType<WorldGridManager>();
			if (gridManager == null) return;

			Color edgeColor = isActive ? Color.yellow : region.RegionColor;
			edgeColor.a = 1f;

			Handles.color = edgeColor;
			Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

			float thickness = gridManager.CellSize * (isActive ? 0.5f : 0.2f);

			foreach (GridEdge edge in region.PerimeterEdges)
			{
				Vector3[] verts = edge.ToWorldVerts(gridManager.CellSize);
				Handles.DrawLine(verts[0], verts[1], thickness);
			}
		}

		private static void DrawIslandWarnings(RegionDataSO region)
		{
			WorldGridManager gridManager = Object.FindObjectOfType<WorldGridManager>();
			if (gridManager == null) return;

			List<HashSet<Vector2Int>> islands = GetIslands(region.ContainedCoords);
			if (islands.Count <= 1)
				return;

			foreach (HashSet<Vector2Int> island in islands)
			{
				Vector2 center = Vector2.zero;
				foreach (Vector2Int c in island)
					center += (Vector2)c;
				center /= island.Count;

				Vector3 worldCenter = gridManager.GetWorldFromCell(Vector2Int.RoundToInt(center));
				float size = gridManager.CellSize * 0.5f;

				Handles.color = Color.yellow;
				Handles.DrawWireDisc(worldCenter, Vector3.forward, size);
				Handles.Label(worldCenter + Vector3.up * size, "⚠", new GUIStyle { fontSize = 16 });
			}
		}

		private static List<HashSet<Vector2Int>> GetIslands(IEnumerable<Vector2Int> coords)
		{
			List<HashSet<Vector2Int>> islands = new List<HashSet<Vector2Int>>();
			HashSet<Vector2Int> remaining = new HashSet<Vector2Int>(coords);

			while (remaining.Count > 0)
			{
				HashSet<Vector2Int> island = new HashSet<Vector2Int>();
				Queue<Vector2Int> frontier = new Queue<Vector2Int>();

				Vector2Int start = default;
				foreach (Vector2Int c in remaining)
				{
					start = c;
					break;
				}

				frontier.Enqueue(start);
				island.Add(start);
				remaining.Remove(start);

				while (frontier.Count > 0)
				{
					Vector2Int current = frontier.Dequeue();

					Vector2Int[] neighbors = new Vector2Int[]
					{
						new Vector2Int(current.x + 1, current.y),
						new Vector2Int(current.x - 1, current.y),
						new Vector2Int(current.x, current.y + 1),
						new Vector2Int(current.x, current.y - 1)
					};

					foreach (Vector2Int neighbor in neighbors)
					{
						if (!remaining.Contains(neighbor)) continue;

						frontier.Enqueue(neighbor);
						island.Add(neighbor);
						remaining.Remove(neighbor);
					}
				}

				islands.Add(island);
			}

			return islands;
		}
	}
}
#endif