using System.Collections.Generic;
using ShoelaceStudios.GridSystem;
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
			Color fillColor = new(
				region.RegionColor.r,
				region.RegionColor.g,
				region.RegionColor.b,
				isActive ? 0.5f : 0.15f
			);

			Color outlineColor = new(
				region.RegionColor.r,
				region.RegionColor.g,
				region.RegionColor.b,
				0.7f
			);

			Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

			foreach (Vector2Int cell in region.ContainedCoords)
			{
				Vector3 cellWorld = WorldGridManager.Instance.CellToWorldSpace(cell);
				float size = WorldGridManager.Instance.CellSize;

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
			Color edgeColor = isActive ? Color.yellow : region.RegionColor;
			edgeColor.a = 1f;

			Handles.color = edgeColor;
			Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

			float thickness = WorldGridManager.Instance.CellSize * (isActive ? 0.5f : 0.2f);

			foreach (GridEdge edge in region.PerimeterEdges)
			{
				Vector3[] verts = edge.ToWorldVerts(WorldGridManager.Instance.CellSize);
				Handles.DrawLine(verts[0], verts[1], thickness);
			}
		}

		private static void DrawIslandWarnings(RegionDataSO region)
		{
			List<HashSet<Vector2Int>> islands = GetIslands(region.ContainedCoords);
			if (islands.Count <= 1)
				return;

			foreach (HashSet<Vector2Int> island in islands)
			{
				Vector2 center = Vector2.zero;
				foreach (Vector2Int c in island)
					center += (Vector2)c;
				center /= island.Count;

				Vector3 worldCenter = WorldGridManager.Instance.CellToWorldSpace(Vector2Int.RoundToInt(center));
				float size = WorldGridManager.Instance.CellSize * 0.5f;

				Handles.color = Color.yellow;
				Handles.DrawWireDisc(worldCenter, Vector3.forward, size);
				Handles.Label(worldCenter + Vector3.up * size, "⚠", new GUIStyle { fontSize = 16 });
			}
		}

		private static List<HashSet<Vector2Int>> GetIslands(IEnumerable<Vector2Int> coords)
		{
			List<HashSet<Vector2Int>> islands = new();
			HashSet<Vector2Int> remaining = new(coords);

			while (remaining.Count > 0)
			{
				HashSet<Vector2Int> island = new();
				Queue<Vector2Int> frontier = new();

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

					Vector2Int[] neighbors =
					{
						new(current.x + 1, current.y),
						new(current.x - 1, current.y),
						new(current.x, current.y + 1),
						new(current.x, current.y - 1)
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