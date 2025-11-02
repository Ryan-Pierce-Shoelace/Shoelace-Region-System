using UnityEngine;

namespace ShoelaceStudios.RegionSystem
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

		public Vector2Int Cell; // The "owner" cell
		public CellEdge Edge; // Which side of the cell

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
			Vector3 center = new Vector3(
				(Cell.x + 0.5f) * cellSize,
				(Cell.y + 0.5f) * cellSize,
				0
			);

			float half = cellSize * 0.5f;

			return Edge switch
			{
				CellEdge.Top => center + new Vector3(0, half, 0),
				CellEdge.Bottom => center + new Vector3(0, -half, 0),
				CellEdge.Left => center + new Vector3(-half, 0, 0),
				CellEdge.Right => center + new Vector3(half, 0, 0),
				_ => center
			};
		}

		// Optional: compute world-space endpoints for visualization
		public void GetWorldVerts(float cellSize, Vector3[] output)
		{
			if (output == null || output.Length < 2)
			{
				Debug.LogError("Output array must have length >= 2");
				return;
			}

			Vector3 bl = new(Cell.x * cellSize, Cell.y * cellSize, 0);
			Vector3 br = bl + new Vector3(cellSize, 0, 0);
			Vector3 tl = bl + new Vector3(0, cellSize, 0);
			Vector3 tr = bl + new Vector3(cellSize, cellSize, 0);

			switch (Edge)
			{
				case CellEdge.Top:
					output[0] = tl;
					output[1] = tr;
					break;
				case CellEdge.Bottom:
					output[0] = bl;
					output[1] = br;
					break;
				case CellEdge.Left:
					output[0] = bl;
					output[1] = tl;
					break;
				case CellEdge.Right:
					output[0] = br;
					output[1] = tr;
					break;
				default:
					output[0] = bl;
					output[1] = br;
					break;
			}
		}

		public Vector3[] ToWorldVerts(float cellSize)
		{
			Vector3[] vert = new Vector3[2];
			GetWorldVerts(cellSize, vert);
			return vert;
		}
	}
}