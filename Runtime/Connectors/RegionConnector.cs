using System;
using UnityEngine;

namespace ShoelaceStudios.GridSystem.Regions
{
	[Serializable]
	public class RegionConnector
	{
		public RegionDataSO RegionA; // First region
		public GridEdge EdgeA; // Edge on RegionA's perimeter

		public RegionDataSO RegionB; // Second region (null or "Outside")
		public Vector2Int? CellB; // The cell in RegionB or "outside"
		public GridEdge? EdgeB; // Edge on RegionB's perimeter

		public ConnectorType Type;
		public RegionConnector(RegionDataSO regionA, GridEdge edgeA, RegionDataSO regionB, GridEdge? edgeB, ConnectorType type)
		{
			RegionA = regionA;
			EdgeA = edgeA;
			RegionB = regionB;
			EdgeB = edgeB;
			Type = type;
		}
		
		public override bool Equals(object obj)
		{
			if (obj is not RegionConnector other) return false;
			return RegionA == other.RegionA && EdgeA.Equals(other.EdgeA);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 23 + (RegionA != null ? RegionA.GetHashCode() : 0);
				hash = hash * 23 + EdgeA.GetHashCode();
				return hash;
			}
		}
	}
}