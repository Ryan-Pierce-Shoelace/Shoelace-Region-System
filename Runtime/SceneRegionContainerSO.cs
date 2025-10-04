using System.Collections.Generic;
using UnityEngine;

namespace ShoelaceStudios.GridSystem.Regions
{
	[CreateAssetMenu(fileName = "SceneRegionContainer", menuName = "GridSystem/Regions/Scene Region Container")]
	public class SceneRegionContainerSO : ScriptableObject
	{
		[SerializeField] private List<RegionDataSO> regions = new();
		public IReadOnlyList<RegionDataSO> Regions => regions;

		/// <summary>
		/// Creates and adds a new RegionDataSO as a nested sub-asset.
		/// </summary>
		public RegionDataSO CreateRegion(string name = "New Region")
		{
			RegionDataSO newRegion = CreateInstance<RegionDataSO>();
			newRegion.hideFlags = HideFlags.None;
			newRegion.Initialize(name, Random.ColorHSV(.7f, 1f, .7f, 1f));

			regions.Add(newRegion);

			#if UNITY_EDITOR
			UnityEditor.AssetDatabase.AddObjectToAsset(newRegion, this);
			UnityEditor.AssetDatabase.SaveAssets();
			#endif

			return newRegion;
		}

		public void RemoveRegion(RegionDataSO region)
		{
			if (regions.Remove(region))
			{
				#if UNITY_EDITOR
				DestroyImmediate(region, true);
				UnityEditor.AssetDatabase.SaveAssets();
				#endif
			}
		}
	}
}