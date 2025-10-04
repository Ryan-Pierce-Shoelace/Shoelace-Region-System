using System.Collections.Generic;
using UnityEngine;

namespace ShoelaceStudios.GridSystem.Regions
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
		
		public static List<Vector2Int> CalculatePerimeter(
			HashSet<Vector2Int> contained, 
			System.Func<int, int, bool> isValidCell)
		{
			List<Vector2Int> perimeter = new();
			foreach (Vector2Int coord in contained)
			{
				Vector2Int[] neighbors = new Vector2Int[]
				{
					new Vector2Int(coord.x+1, coord.y),
					new Vector2Int(coord.x-1, coord.y),
					new Vector2Int(coord.x, coord.y+1),
					new Vector2Int(coord.x, coord.y-1)
				};

				foreach (Vector2Int n in neighbors)
				{
					if (!contained.Contains(n) && isValidCell(n.x, n.y))
					{
						perimeter.Add(coord);
						break;
					}
				}
			}
			return perimeter;
		}
	}
}