using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ShoelaceStudios.RegionSystem
{
	[CreateAssetMenu(menuName = "GridSystem/Regions/ConnectorContainer")]
	public class ConnectorContainerSO : ScriptableObject
	{
		public List<RegionConnector> Connectors = new();


		private void OnEnable()
		{
			ValidateConnectors();
		}

		private void ValidateConnectors()
		{
			if (Connectors == null || Connectors.Count == 0)
				return;

			int removed = Connectors.RemoveAll(c => c == null || c.RegionA == null);

			if (removed > 0)
			{
				Debug.LogWarning($"[ConnectorContainerSO] Removed {removed} connectors with NULL RegionA from {name}");
				#if UNITY_EDITOR
				EditorUtility.SetDirty(this);
				#endif
			}
		}

		public void AddConnector(RegionConnector connector)
		{
			if (connector == null)
			{
				Debug.LogError("[ConnectorContainerSO] Cannot add NULL connector");
				return;
			}

			if (connector.RegionA == null)
			{
				Debug.LogError("[ConnectorContainerSO] Cannot add connector - RegionA is NULL");
				return;
			}

			if (Connectors.Contains(connector))
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
			if (connector == null || connector.RegionA == null)
				return true;

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


		[ContextMenu("Clean Invalid Connectors")]
		public void CleanInvalidConnectors()
		{
			int before = Connectors.Count;
			Connectors.RemoveAll(c => c == null || c.RegionA == null);
			int after = Connectors.Count;

			Debug.Log($"[ConnectorContainerSO] Cleaned {before - after} invalid connectors. {after} remain.");

			#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
			AssetDatabase.SaveAssets();
			#endif
		}
	}
}