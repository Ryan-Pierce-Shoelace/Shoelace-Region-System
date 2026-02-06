using System.Collections.Generic;
using UnityEngine;

namespace ShoelaceStudios.RegionSystem
{
	public static class RegionUtility
	{
		public static HashSet<GridEdge> CalculatePerimeterEdges(HashSet<Vector2Int> cells)
		{
			HashSet<GridEdge> edges = new();

			foreach (Vector2Int cell in cells)
			{
				// Top
				if (!cells.Contains(cell + Vector2Int.up)) edges.Add(new GridEdge(cell, GridEdge.CellEdge.Top));
				// Bottom
				if (!cells.Contains(cell + Vector2Int.down)) edges.Add(new GridEdge(cell, GridEdge.CellEdge.Bottom));
				// Left
				if (!cells.Contains(cell + Vector2Int.left)) edges.Add(new GridEdge(cell, GridEdge.CellEdge.Left));
				// Right
				if (!cells.Contains(cell + Vector2Int.right)) edges.Add(new GridEdge(cell, GridEdge.CellEdge.Right));
			}

			return edges;
		}
		
		public static List<Vector2Int> CalculatePerimeter(HashSet<Vector2Int> contained, System.Func<int, int, bool> isValidCell)
		{
			List<Vector2Int> perimeter = new();
			foreach (Vector2Int coord in contained)
			{
				Vector2Int[] neighbors = {
					new(coord.x+1, coord.y),
					new(coord.x-1, coord.y),
					new(coord.x, coord.y+1),
					new(coord.x, coord.y-1)
				};

				foreach (Vector2Int n in neighbors)
				{
					if (contained.Contains(n) || !isValidCell(n.x, n.y)) continue;

					perimeter.Add(coord);
					break;
				}
			}
			return perimeter;
		}
	}
}