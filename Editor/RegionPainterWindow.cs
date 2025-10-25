using ShoelaceStudios.Utilities.Helpers;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShoelaceStudios.GridSystem.Regions.Editor
{
    /// <summary>
    /// Main editor window for painting regions onto the scene grid.
    /// Handles UI, scene rendering, and delegates painting logic.
    /// </summary>
    public class RegionPainterWindow : EditorWindow
    {
        // -----------------------------
        // Fields
        // -----------------------------
        private SceneRegionContainerSO container;
        private RegionDataSO activeRegion;

        public SceneRegionContainerSO Container => container;
        public RegionDataSO ActiveRegion => activeRegion;
        
        private bool painting = false;

        // Toggles
        private bool addMode = true;    // Add vs Subtract
        private bool rectMode = false;  // Rect vs Pen
        private bool overwrite = false; // Overwrite vs Ignore overlaps

        // Rect drawing state
        private Vector2Int? rectStart = null;

        // Encapsulated painting logic
        public static RegionPainterLogic Logic;

        // -----------------------------
        // Menu
        // -----------------------------
        [MenuItem("Tools/Region Painter")]
        public static void OpenWindow() => GetWindow<RegionPainterWindow>("Region Painter");

        // -----------------------------
        // Unity callbacks
        // -----------------------------
        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            Logic = new RegionPainterLogic(this);
            
	            if (container == null)
		            container = SceneAssetHelper.GetOrCreateAsset<SceneRegionContainerSO>(SceneManager.GetActiveScene().name + "_Regions");
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        // -----------------------------
        // GUI
        // -----------------------------
        private void OnGUI()
        {
            DrawContainerField();

            if (container == null) return;

            DrawRegionManagement();
            DrawActiveRegionControls();
        }

        private void DrawContainerField()
        {
            GUILayout.Label("Scene Region Container", EditorStyles.boldLabel);
            container = (SceneRegionContainerSO)EditorGUILayout.ObjectField("Container", container, typeof(SceneRegionContainerSO), false);
        }

        private void DrawRegionManagement()
        {
            GUILayout.Label("Regions", EditorStyles.boldLabel);

            if (GUILayout.Button("Add Region"))
                activeRegion = container.CreateRegion("Region " + container.Regions.Count);

            foreach (RegionDataSO region in container.Regions)
                DrawRegionRow(region);
        }

        private void DrawRegionRow(RegionDataSO region)
        {
            EditorGUILayout.BeginHorizontal();

            // Multiple islands warning
            bool hasIslands = Logic.HasMultipleIslands(region.ContainedCoords);
            GUILayout.Label(hasIslands ? EditorGUIUtility.IconContent("console.warnicon") : GUIContent.none, GUILayout.Width(20));

            // Name and color
            string newName = EditorGUILayout.TextField(region.RegionName);
            if (newName != region.RegionName) region.SetRegionName(newName);

            Color newColor = EditorGUILayout.ColorField(region.RegionColor, GUILayout.MaxWidth(60));
            if (newColor != region.RegionColor) region.SetColor(newColor);

            // Selection toggle
            if (GUILayout.Toggle(region == activeRegion, "Select", "Button"))
                activeRegion = region;

            // Remove button
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                container.RemoveRegion(region);
                if (activeRegion == region) activeRegion = null;
                EditorGUILayout.EndHorizontal();
                return;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawActiveRegionControls()
        {
            if (activeRegion == null) return;

            GUILayout.Label("Active Region: " + activeRegion.RegionName, EditorStyles.helpBox);
            painting = GUILayout.Toggle(painting, "Painting Mode", "Button");

            GUILayout.Space(5);

            addMode = GUILayout.Toggle(addMode, addMode ? "Add Mode" : "Subtract Mode", "Button");
            rectMode = GUILayout.Toggle(rectMode, rectMode ? "Rect Draw" : "Pen Draw", "Button");
            overwrite = GUILayout.Toggle(overwrite, overwrite ? "Overwrite On" : "Overwrite Off", "Button");
        }

        // -----------------------------
        // Scene GUI / Drawing
        // -----------------------------
        private void OnSceneGUI(SceneView sceneView)
        {
            if (container == null) return;

            Event e = Event.current;

            // Draw all regions
            DrawAllRegions();

            if (activeRegion == null || !painting) return;

            // Convert mouse to grid coordinate
            Vector3 worldPoint = Logic.GetMouseWorldPoint(e);
            Vector2Int gridCoord = WorldGridManager.Instance.GetCell(worldPoint);
            if (!WorldGridManager.Instance.IsValidCell(gridCoord)) return;

            // Draw hover preview
            Logic.DrawHoverHighlight(gridCoord, addMode);

            // Apply painting based on mode
            if (rectMode)
                Logic.HandleRectMode(e, gridCoord, activeRegion, addMode, overwrite, ref rectStart);
            else
                Logic.HandlePenMode(e, gridCoord, activeRegion, addMode, overwrite);

            SceneView.RepaintAll();
        }

        private void DrawAllRegions()
        {
            foreach (RegionDataSO region in container.Regions)
                Logic.DrawRegion(region, region == activeRegion);
        }
    }
}