using System.Collections.Generic;
using ShoelaceStudios.Utilities;
using UnityEditor;
using UnityEngine;

namespace ShoelaceStudios.GridSystem.Regions
{
	public class RegionDataSO : ScriptableObject
	{
		[Header("Region Info")]
		public string RegionName;
		public Color RegionColor;
		public SerializableGuid ID { private set; get; }

		[Header("Grid Data")]
		[SerializeField] private List<Vector2Int> containedCoords = new();
		[SerializeField] private List<GridEdge> perimeterEdges = new();

		public IReadOnlyList<Vector2Int> ContainedCoords => containedCoords;
		public IReadOnlyList<GridEdge> PerimeterEdges => perimeterEdges;

		public void Initialize(string n, Color color)
		{
			SetRegionName(n);
			RegionColor = color;
			ID = SerializableGuid.NewGuid();
		}
		
		public void SetRegionName(string newName)
		{
			RegionName = newName;

			#if UNITY_EDITOR
			// Sync the ScriptableObject asset name in Project
			if (this)
			{
				name = newName;
				EditorUtility.SetDirty(this);
				AssetDatabase.SaveAssets();
			}
			#endif
		}
		public void SetColor(Color newColor)
		{
			RegionColor = newColor;
			EditorUtility.SetDirty(this);
			AssetDatabase.SaveAssets();
		}
		
		public void SetCoords(IEnumerable<Vector2Int> contained, IEnumerable<GridEdge> edges)
		{
			containedCoords.Clear();
			containedCoords.AddRange(contained);

			perimeterEdges.Clear();
			perimeterEdges.AddRange(edges);

			#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
			#endif
		}
		
		public bool ContainsCell(Vector2Int coord) => containedCoords.Contains(coord);
	}
}