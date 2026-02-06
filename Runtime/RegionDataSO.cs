using System.Collections.Generic;
using ShoelaceStudios.Utilities;
using UnityEditor;
using UnityEngine;

namespace ShoelaceStudios.RegionSystem
{
    public class RegionDataSO : ScriptableObject
    {
        [Header("Region Info")]
        public string RegionName;
        public Color RegionColor;
        public SerializableGuid ID { get; private set; }

        [Header("Grid Data")]
        [SerializeField] private List<Vector2Int> containedCoords = new();
        [SerializeField] private List<GridEdge> perimeterEdges = new();

        [SerializeField] private Vector2Int boundsMin;
        [SerializeField] private Vector2Int boundsMax;

        private HashSet<Vector2Int> coordsLookup;

        public IReadOnlyList<Vector2Int> ContainedCoords => containedCoords;
        public IReadOnlyList<GridEdge> PerimeterEdges => perimeterEdges;
        public Vector2Int BoundsMin => boundsMin;
        public Vector2Int BoundsMax => boundsMax;

        public void Initialize(string n, Color color)
        {
            SetRegionName(n);
            RegionColor = color;
            ID = SerializableGuid.NewGuid();
            coordsLookup = new HashSet<Vector2Int>();
        }

        private void OnEnable()
        {
            coordsLookup = new HashSet<Vector2Int>(containedCoords);
        }

        public void SetRegionName(string newName)
        {
            RegionName = newName;

            #if UNITY_EDITOR
            if (!this) return;

            name = newName;
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            #endif
        }

        public void SetColor(Color newColor)
        {
            RegionColor = newColor;

            #if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            #endif
        }

        public void SetCoords(IEnumerable<Vector2Int> contained, IEnumerable<GridEdge> edges)
        {
            containedCoords.Clear();
            containedCoords.AddRange(contained);

            coordsLookup = new HashSet<Vector2Int>(containedCoords);
            CalculateBounds();

            perimeterEdges.Clear();
            perimeterEdges.AddRange(edges);

            #if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            #endif
        }

        private void CalculateBounds()
        {
            if (containedCoords.Count == 0)
            {
                boundsMin = Vector2Int.zero;
                boundsMax = Vector2Int.zero;
                return;
            }

            boundsMin = containedCoords[0];
            boundsMax = containedCoords[0];

            foreach (Vector2Int coord in containedCoords)
            {
                if (coord.x < boundsMin.x) boundsMin.x = coord.x;
                if (coord.y < boundsMin.y) boundsMin.y = coord.y;
                if (coord.x > boundsMax.x) boundsMax.x = coord.x;
                if (coord.y > boundsMax.y) boundsMax.y = coord.y;
            }
        }

        public bool ContainsCell(Vector2Int coord)
        {
            if (coordsLookup == null)
            {
                coordsLookup = new HashSet<Vector2Int>(containedCoords);
            }

            return coordsLookup.Contains(coord);
        }
    }
}