using ShoelaceStudios.GridSystem;
using ShoelaceStudios.Utilities.Helpers;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShoelaceStudios.RegionSystem.Editor
{
    public class RegionPainterWindow : EditorWindow
    {
        private SceneRegionContainerSO container;
        private RegionDataSO activeRegion;

        public SceneRegionContainerSO Container => container;
        public RegionDataSO ActiveRegion => activeRegion;
        
        private bool painting = false;
        private bool addMode = true;
        private bool rectMode = false;
        private bool overwrite = false;
        private bool useWorldUVs = false;
        private Vector2Int? rectStart = null;

        public static RegionPainterLogic Logic;

        [MenuItem("Tools/Region Painter")]
        public static void OpenWindow() => GetWindow<RegionPainterWindow>("Region Painter");

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            Logic = new RegionPainterLogic(this);
            
            WorldGridManager.Instance.InitializeGrid();
            
            if (container == null)
                container = SceneAssetHelper.GetOrCreateAsset<SceneRegionContainerSO>(
                    SceneManager.GetActiveScene().name + "_Regions");
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            DrawContainerField();

            if (container == null) return;

            DrawRegionManagement();
            DrawActiveRegionControls();
            DrawMeshGenerationControls();
        }

        private void DrawContainerField()
        {
            GUILayout.Label("Scene Region Container", EditorStyles.boldLabel);
            container = (SceneRegionContainerSO)EditorGUILayout.ObjectField(
                "Container", container, typeof(SceneRegionContainerSO), false);
        }

        private void DrawRegionManagement()
        {
            GUILayout.Label("Regions", EditorStyles.boldLabel);

            if (GUILayout.Button("Add Region"))
                activeRegion = container.CreateRegion("Region " + container.Regions.Count);

            // FIX: Iterate backwards to avoid collection modification exception
            for (int i = container.Regions.Count - 1; i >= 0; i--)
            {
                if (i < container.Regions.Count)
                    DrawRegionRow(container.Regions[i]);
            }
        }

        private void DrawRegionRow(RegionDataSO region)
        {
            EditorGUILayout.BeginHorizontal();

            bool hasIslands = Logic.HasMultipleIslands(region.ContainedCoords);
            GUILayout.Label(
                hasIslands ? EditorGUIUtility.IconContent("console.warnicon") : GUIContent.none, 
                GUILayout.Width(20));

            string newName = EditorGUILayout.TextField(region.RegionName);
            if (newName != region.RegionName) region.SetRegionName(newName);

            Color newColor = EditorGUILayout.ColorField(region.RegionColor, GUILayout.MaxWidth(60));
            if (newColor != region.RegionColor) region.SetColor(newColor);

            if (GUILayout.Toggle(region == activeRegion, "Select", "Button"))
                activeRegion = region;

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
            
            GUI.backgroundColor = painting ? Color.green : Color.white;
            painting = GUILayout.Toggle(painting, 
                painting ? "✓ PAINTING ENABLED" : "☐ Painting Disabled", 
                "Button", GUILayout.Height(30));
            GUI.backgroundColor = Color.white;

            if (!painting)
            {
                EditorGUILayout.HelpBox("Click 'PAINTING ENABLED' to start painting!", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Click in Scene View to paint. Hold Alt to move camera.", MessageType.Info);
            }

            GUILayout.Space(5);

            addMode = GUILayout.Toggle(addMode, addMode ? "Add Mode (Paint)" : "Subtract Mode (Erase)", "Button");
            rectMode = GUILayout.Toggle(rectMode, rectMode ? "Rect Draw (Drag)" : "Pen Draw (Click)", "Button");
            overwrite = GUILayout.Toggle(overwrite, overwrite ? "Overwrite: ON" : "Overwrite: OFF", "Button");
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (container == null) return;
    
     


            WorldGridManager grid = WorldGridManager.Instance;
            if (grid == null)
            {
                Handles.BeginGUI();
                GUILayout.BeginArea(new Rect(10, 10, 300, 100));
                GUILayout.Label("WorldGridManager not found!", 
                    new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.red }, fontSize = 14 });
                GUILayout.Label("Add WorldGridManager to scene");
                GUILayout.EndArea();
                Handles.EndGUI();
                return;
            }

            Event e = Event.current;

            DrawAllRegions();

            if (activeRegion == null || !painting)
            {
                if (activeRegion == null && painting)
                {
                    Handles.BeginGUI();
                    GUILayout.BeginArea(new Rect(10, 10, 300, 60));
                    GUILayout.Label("No region selected!", 
                        new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.yellow }, fontSize = 14 });
                    GUILayout.EndArea();
                    Handles.EndGUI();
                }
                return;
            }

            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            EventType eventType = e.GetTypeForControl(controlID);

            if (eventType == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(controlID);
            }

            Vector3 worldPoint = Logic.GetMouseWorldPoint(e);
            Vector2Int gridCoord = grid.WorldToCell(worldPoint);

            if (!grid.IsValidCell(gridCoord)) return;

            Logic.DrawHoverHighlight(gridCoord, addMode);

            if (rectMode)
                Logic.HandleRectMode(e, controlID, gridCoord, activeRegion, addMode, overwrite, ref rectStart);
            else
                Logic.HandlePenMode(e, controlID, gridCoord, activeRegion, addMode, overwrite);

            if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag || e.type == EventType.MouseUp)
            {
                SceneView.RepaintAll();
            }
        }

        private void DrawAllRegions()
        {
            foreach (RegionDataSO region in container.Regions)
                Logic.DrawRegion(region, region == activeRegion);
        }

        private void DrawMeshGenerationControls()
        {
            GUILayout.Space(10);
            GUILayout.Label("Mesh Generation", EditorStyles.boldLabel);

            useWorldUVs = GUILayout.Toggle(useWorldUVs, "Use World-Space UVs");

            if (GUILayout.Button("Spawn Region Meshes in Scene"))
            {
                if (container == null)
                {
                    Debug.LogWarning("No region container assigned.");
                    return;
                }

                GameObject parent = GameObject.Find("Generated_Regions");
                if (parent == null)
                    parent = new GameObject("Generated_Regions");

                foreach (RegionDataSO region in container.Regions)
                {
                    if (region.ContainedCoords.Count == 0)
                        continue;

                    Mesh mesh = RegionMeshGenerator.GenerateRegionMesh(
                        region, WorldGridManager.Instance, useWorldUVs);

                    GameObject regionGO = new GameObject(region.RegionName + "_Mesh");
                    regionGO.transform.SetParent(parent.transform, false);

                    MeshFilter mf = regionGO.AddComponent<MeshFilter>();
                    MeshRenderer mr = regionGO.AddComponent<MeshRenderer>();

                    mf.sharedMesh = mesh;

                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = region.RegionColor;

                    mr.sharedMaterial = mat;
                    mr.sortingOrder = 100;
                }

                Debug.Log($"Spawned {container.Regions.Count} region meshes into the scene.");
            }
        }
    }
}