using UnityEngine;

namespace ShoelaceStudios.GridSystem.Regions
{
	[System.Serializable]
	public struct GridEdge
	{
		public enum CellEdge
		{
			Top,
			Bottom,
			Left,
			Right
		}
		
		public Vector2Int Cell;   // The "owner" cell
		public CellEdge Edge;     // Which side of the cell

		public GridEdge(Vector2Int cell, CellEdge edge)
		{
			Cell = cell;
			Edge = edge;
		}

		public override bool Equals(object obj)
		{
			if (obj is not GridEdge edge) return false;

			return Cell == edge.Cell && Edge == edge.Edge;
		}

		public override int GetHashCode()
		{
			return Cell.GetHashCode() ^ Edge.GetHashCode();
		}

		public Vector3 GetEdgeMiddle(float cellSize)
		{
			// center of the cell in world space
			Vector3 center = new(
				(Cell.x + 0.5f) * cellSize,
				(Cell.y + 0.5f) * cellSize,
				0
			);

			// half offset in local space
			float half = cellSize / 2f;

			return Edge switch
			{
				CellEdge.Top    => center + new Vector3(0,  half, 0),
				CellEdge.Bottom => center + new Vector3(0, -half, 0),
				CellEdge.Left   => center + new Vector3(-half, 0, 0),
				CellEdge.Right  => center + new Vector3( half, 0, 0),
				_ => center
			};
		}

		// Optional: compute world-space endpoints for visualization
		public Vector3[] ToWorldVerts(float cellSize)
		{
			Vector3 bl = new(Cell.x * cellSize, Cell.y * cellSize, 0);
			Vector3 br = bl + new Vector3(cellSize, 0, 0);
			Vector3 tl = bl + new Vector3(0, cellSize, 0);
			Vector3 tr = bl + new Vector3(cellSize, cellSize, 0);

			return Edge switch
			{
				CellEdge.Top => new[] { tl, tr },
				CellEdge.Bottom => new[] { bl, br },
				CellEdge.Left => new[] { bl, tl },
				CellEdge.Right => new[] { br, tr },
				_ => new[] { bl, br }
			};
		}
	}
}