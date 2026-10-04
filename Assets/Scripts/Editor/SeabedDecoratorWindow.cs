using UnityEditor;
using UnityEngine;
using MultiCamCapture.Demo;

namespace MultiCamCapture.Editor
{
    /// <summary>
    /// Interactive Editor Window to customize and generate the procedural seabed environment.
    /// Exposes sliders for decoration density, reef knolls, kelp forests, fauna, horizon extension, and random seed.
    /// Includes real-time Performance Optimization controls (GPU Instancing, Mesh Combining, Shadow Culling).
    /// </summary>
    public class SeabedDecoratorWindow : EditorWindow
    {
        [SerializeField] private float globalDensity = 1.0f;
        [SerializeField] private float reefKnollMultiplier = 1.0f;
        [SerializeField] private float kelpGroveMultiplier = 1.0f;
        [SerializeField] private float faunaMultiplier = 1.0f;
        [SerializeField] private float horizonExtension = 1.35f;
        [SerializeField] private int randomSeed = 1337;
        [SerializeField] private int bubbleVentCount = 8;
        [SerializeField] private bool combineMeshes = false;
        [SerializeField] private bool optimizeShadows = true;

        private Vector2 scrollPos;

        [MenuItem("Aquarium/Seabed Decorator Settings...")]
        public static void ShowWindow()
        {
            var win = GetWindow<SeabedDecoratorWindow>("Seabed Decorator");
            win.minSize = new Vector2(350, 480);
            win.Show();
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Procedural Seabed Decorator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure density, clustering, and horizon padding. Assets dynamically scale and ground to the active AquariumBounds volume.", MessageType.Info);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Performance & Optimization", EditorStyles.boldLabel);
            combineMeshes = EditorGUILayout.Toggle(new GUIContent("Combine Meshes (High FPS)", "Merges individual props by material, reducing hundreds of draw calls to ~5."), combineMeshes);
            optimizeShadows = EditorGUILayout.Toggle(new GUIContent("Optimize Shadows", "Disables real-time shadow casting on small foliage and starfish."), optimizeShadows);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Density & Distribution", EditorStyles.boldLabel);
            globalDensity = EditorGUILayout.Slider("Global Density", globalDensity, 0.2f, 3.0f);
            reefKnollMultiplier = EditorGUILayout.Slider("Reef Knolls", reefKnollMultiplier, 0.2f, 3.0f);
            kelpGroveMultiplier = EditorGUILayout.Slider("Kelp Groves", kelpGroveMultiplier, 0.2f, 3.0f);
            faunaMultiplier = EditorGUILayout.Slider("Fauna & Shells", faunaMultiplier, 0.2f, 3.0f);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Endless Horizon & Vents", EditorStyles.boldLabel);
            horizonExtension = EditorGUILayout.Slider("Horizon Padding", horizonExtension, 1.1f, 2.2f);
            bubbleVentCount = EditorGUILayout.IntSlider("Bubble Vents", bubbleVentCount, 2, 12);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Randomization", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            randomSeed = EditorGUILayout.IntField("Seed", randomSeed);
            if (GUILayout.Button("Randomize", GUILayout.Width(85)))
            {
                randomSeed = Random.Range(1, 99999);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(18);
            GUI.backgroundColor = new Color(0.3f, 0.85f, 0.4f);
            if (GUILayout.Button("Decorate Seabed (Generate)", GUILayout.Height(38)))
            {
                ApplyDecoration();
            }

            GUI.backgroundColor = new Color(0.4f, 0.7f, 1.0f);
            EditorGUILayout.Space(4);
            if (GUILayout.Button("Optimize Existing Seabed Meshes", GUILayout.Height(28)))
            {
                SeabedDecorator.OptimizeExistingSeabed();
            }

            GUI.backgroundColor = new Color(1.0f, 0.4f, 0.4f);
            EditorGUILayout.Space(4);
            if (GUILayout.Button("Clear Seabed Environment", GUILayout.Height(26)))
            {
                ClearSeabed();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndScrollView();
        }

        private void ApplyDecoration()
        {
            var boundsComp = Object.FindAnyObjectByType<AquariumBounds>();
            Vector3 center = boundsComp != null ? boundsComp.transform.position : Vector3.zero;
            Vector3 size = boundsComp != null ? boundsComp.Size : new Vector3(50f, 18f, 30f);

            SeabedDecorator.DecoratorSettings settings = new SeabedDecorator.DecoratorSettings
            {
                globalDensity = globalDensity,
                knollMultiplier = reefKnollMultiplier,
                kelpMultiplier = kelpGroveMultiplier,
                faunaMultiplier = faunaMultiplier,
                horizonPadding = horizonExtension,
                seed = randomSeed,
                bubbleVents = bubbleVentCount,
                combineMeshes = combineMeshes,
                optimizeShadows = optimizeShadows
            };

            SeabedDecorator.DecorateSeabedWithSettings(center, size, settings);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            Debug.Log($"[SeabedDecoratorWindow] Successfully decorated optimized seabed with seed {randomSeed} and density {globalDensity:F2}x!");
        }

        private void ClearSeabed()
        {
            GameObject existingRoot = GameObject.Find(SeabedDecorator.RootName);
            if (existingRoot != null)
            {
                Undo.DestroyObjectImmediate(existingRoot);
                Debug.Log("[SeabedDecoratorWindow] Cleared seabed environment.");
            }
        }
    }
}
