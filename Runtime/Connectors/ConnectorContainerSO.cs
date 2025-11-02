using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ShoelaceStudios.RegionSystem
{
	[CreateAssetMenu(menuName = "GridSystem/Regions/ConnectorContainer")]
	public class ConnectorContainerSO : ScriptableObject
	{
		public List<RegionConnector> Connectors = new();

		public void AddConnector(RegionConnector connector)
		{
			if (connector == null || Connectors.Contains(connector))
				return;

			Connectors.Add(connector);

			#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
			#endif
		}

		public void RemoveConnector(RegionConnector connector)
		{
			if (Connectors.Remove(connector))
			{
				#if UNITY_EDITOR
				EditorUtility.SetDirty(this);
				#endif
			}
		}

		public void OnRegionUpdated(RegionDataSO region)
		{
			if (region == null)
				return;

			int removedCount = Connectors.RemoveAll(connector => ShouldRemoveConnector(connector, region));

			if (removedCount > 0)
			{
				#if UNITY_EDITOR
				EditorUtility.SetDirty(this);
				#endif
			}
		}

		private bool ShouldRemoveConnector(RegionConnector connector, RegionDataSO updatedRegion)
		{
			if (connector.RegionA == updatedRegion)
			{
				if (!updatedRegion.ContainsCell(connector.EdgeA.Cell))
					return true;
			}

			if (connector.RegionB == updatedRegion && connector.EdgeB.HasValue)
			{
				if (!updatedRegion.ContainsCell(connector.EdgeB.Value.Cell))
					return true;
			}

			return false;
		}
	}
}