using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ShoelaceStudios.GridSystem.Regions
{
	[CreateAssetMenu(menuName = "GridSystem/Regions/ConnectorContainer")]
	public class ConnectorContainerSO : ScriptableObject
	{
		public List<RegionConnector> Connectors = new List<RegionConnector>();

		public void AddConnector(RegionConnector connector)
		{
			if (!Connectors.Contains(connector))
				Connectors.Add(connector);
			EditorUtility.SetDirty(this);
		}

		public void RemoveConnector(RegionConnector connector)
		{
			Connectors.Remove(connector);
			EditorUtility.SetDirty(this);
		}

		// Automatically clean up connectors when regions change
		public void OnRegionUpdated(RegionDataSO region)
		{
			List<RegionConnector> toRemove = new List<RegionConnector>();

			foreach (RegionConnector c in Connectors)
			{
				if (c.RegionA == region && !region.ContainsCell(c.EdgeA.Cell))
					toRemove.Add(c);

				if (c.RegionB == region && c.CellB != null && !region.ContainsCell(c.CellB.Value))
					toRemove.Add(c);
			}

			foreach (RegionConnector r in toRemove)
				Connectors.Remove(r);

			if (toRemove.Count > 0)
				EditorUtility.SetDirty(this);
		}
	}
}