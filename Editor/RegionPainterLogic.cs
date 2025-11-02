using System.Collections.Generic;
using ShoelaceStudios.GridSystem;
using UnityEditor;
using UnityEngine;

namespace ShoelaceStudios.RegionSystem.Editor
{
	/// <summary>
	/// Encapsulates all painting logic for the RegionPainterWindow.
	/// Handles pen & rect drawing, islands detection, and region updates.
	/// </summary>
	public class RegionPainterLogic
	{
		private Dictionary<Vector2Int, RegionDataSO> cellToRegionCache;
		private bool cacheNeedsRebuild = true;

		private readonly RegionPainterWindow editorWindow;


		public RegionPainterLogic(RegionPainterWindow regionPainterWindow)
		{
			editorWindow = regionPainterWindow;
		}


		#region Drawing/ painting

		public void DrawRegion(RegionDataSO region, bool isActive)
		{
			RegionGizmos.DrawRegion(region, isActive);
		}

		public void DrawHoverHighlight(Vector2Int coord, bool addMode)
		{
			Vector3 worldCenter = WorldGridManager.Instance.CellToWorldSpace(coord);
			float size = WorldGridManager.Instance.CellSize;

			Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
			Handles.color = addMode ? Color.green : Color.red;
			Handles.DrawWireCube(worldCenter, Vector3.one * size * 0.95f);
		}

		#endregion

		#region Painting Modes

		public void HandlePenMode(Event e, Vector2Int coord, RegionDataSO region, bool addMode, bool overwrite)
		{
			if ((e.type != EventType.MouseDown && e.type != EventType.MouseDrag) || e.button != 0 || e.alt)
				return;

			ApplyToRegion(region, new List<Vector2Int> { coord }, addMode, overwrite, editorWindow.Container);
			e.Use();
			HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
		}

		public void HandleRectMode(Event e, Vector2Int coord, RegionDataSO region, bool addMode, bool overwrite, ref Vector2Int? rectStart)
		{
			if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && rectStart == null)
			{
				rectStart = coord;
				e.Use();
				if (e.type == EventType.Layout)
					HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
			}

			if (!rectStart.HasValue) return;

			Vector2Int start = rectStart.Value;
			RectInt rect = MakeRect(start, coord);

			Handles.color = new Color(0, 1, 0, 0.2f);
			Handles.DrawSolidRectangleWithOutline(RectToWorldVerts(rect), new Color(0, 1, 0, 0.1f), Color.green);

			if (e.type != EventType.MouseUp || e.button != 0) return;

			List<Vector2Int> coords = new();
			for (int x = rect.xMin; x < rect.xMax; x++)
			for (int y = rect.yMin; y < rect.yMax; y++)
				coords.Add(new Vector2Int(x, y));

			ApplyToRegion(region, coords, addMode, overwrite, editorWindow.Container);
			rectStart = null;
			e.Use();
		}

		#endregion


		// -----------------------------
		// Region Updates
		// -----------------------------
		private void ApplyToRegion(RegionDataSO region, List<Vector2Int> coords, bool addMode, bool overwrite, SceneRegionContainerSO container)
		{
			HashSet<Vector2Int> contained = new(region.ContainedCoords);

			foreach (Vector2Int coord in coords)
			{
				if (!WorldGridManager.Instance.IsValidCell(coord))
					continue;

				if (addMode)
				{
					RegionDataSO existing = GetRegionAtCell(coord);

					if (!overwrite)
					{
						if (existing != null && existing != region)
							continue;
					}
					else
					{
						if (existing != null && existing != region)
						{
							HashSet<Vector2Int> otherCoords = new(existing.ContainedCoords);
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

			cacheNeedsRebuild = true;

			EditorUtility.SetDirty(region);
			if (container != null)
			{
				EditorUtility.SetDirty(container);
			}
		}


		// -----------------------------
		// Helpers
		// -----------------------------
		public Vector3 GetMouseWorldPoint(Event e)
		{
			Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
			Plane plane = new(Vector3.forward, Vector3.zero);
			if (!plane.Raycast(ray, out float dist)) return Vector3.zero;

			return ray.GetPoint(dist);
		}

		public bool HasMultipleIslands(IEnumerable<Vector2Int> coords)
		{
			return GetIslands(coords).Count > 1;
		}

		public List<HashSet<Vector2Int>> GetIslands(IEnumerable<Vector2Int> coords)
		{
			List<HashSet<Vector2Int>> islands = new();
			HashSet<Vector2Int> remaining = new(coords);

			while (remaining.Count > 0)
			{
				Queue<Vector2Int> queue = new();
				HashSet<Vector2Int> island = new();
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
						         new(current.x + 1, current.y),
						         new(current.x - 1, current.y),
						         new(current.x, current.y + 1),
						         new(current.x, current.y - 1)
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

			// width and height are inclusive
			return new RectInt(xMin, yMin, xMax - xMin + 1, yMax - yMin + 1);
		}


		private Vector3[] RectToWorldVerts(RectInt rect)
		{
			float size = WorldGridManager.Instance.CellSize;
			return new Vector3[]
			{
				new Vector3(rect.xMin, rect.yMin, 0) * size,
				new Vector3(rect.xMax, rect.yMin, 0) * size,
				new Vector3(rect.xMax, rect.yMax, 0) * size,
				new Vector3(rect.xMin, rect.yMax, 0) * size
			};
		}

		#region Private - Cache Management

		private void RebuildCellCache()
		{
			if (editorWindow.Container == null)
			{
				cellToRegionCache = new Dictionary<Vector2Int, RegionDataSO>();
				return;
			}

			cellToRegionCache = new Dictionary<Vector2Int, RegionDataSO>();

			foreach (RegionDataSO region in editorWindow.Container.Regions)
			{
				foreach (Vector2Int cell in region.ContainedCoords)
				{
					cellToRegionCache[cell] = region;
				}
			}

			cacheNeedsRebuild = false;
		}

		private RegionDataSO GetRegionAtCell(Vector2Int cell)
		{
			if (cacheNeedsRebuild || cellToRegionCache == null)
				RebuildCellCache();

			return cellToRegionCache.TryGetValue(cell, out RegionDataSO region) ? region : null;
		}

		#endregion
	}
}