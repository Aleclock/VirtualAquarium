using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using MultiCamCapture.Demo;

namespace MultiCamCapture.Editor
{
    /// <summary>
    /// Procedurally decorates the aquarium seabed based on the AquariumBounds volume.
    /// Exposes customizable parameters for density, horizon padding, and random seed.
    /// Includes high-performance GPU Instancing, shared material caching, shadow culling,
    /// collider stripping, and Mesh Combining (reducing 500+ draw calls to ~5).
    /// </summary>
    public static class SeabedDecorator
    {
        public const string RootName = "[Seabed Environment]";
        public const string PrefabExportPath = "Assets/Prefabs/AquariumEnvironment.prefab";

        public struct DecoratorSettings
        {
            public float globalDensity;
            public float knollMultiplier;
            public float kelpMultiplier;
            public float faunaMultiplier;
            public float horizonPadding;
            public int seed;
            public int bubbleVents;
            public bool combineMeshes;
            public bool optimizeShadows;

            public static DecoratorSettings Default => new DecoratorSettings
            {
                globalDensity = 1.0f,
                knollMultiplier = 1.0f,
                kelpMultiplier = 1.0f,
                faunaMultiplier = 1.0f,
                horizonPadding = 1.35f,
                seed = 1337,
                bubbleVents = 8,
                combineMeshes = true,
                optimizeShadows = true
            };
        }

        // Asset paths
        public static readonly string[] ReefSets = new string[]
        {
            "Assets/Resources/Seabed/Corals/Coral_Reef_Set_1/CoralReefSet1.fbx",
            "Assets/Resources/Seabed/Corals/Coral_Reef_Set_2/CoralReefSet2.fbx",
            "Assets/Resources/Seabed/Corals/Coral_Reef_Set_3/CoralReefSet3.fbx",
            "Assets/Resources/Seabed/Corals/Coral_Reef_Set_4/CoralReefSet4.fbx",
            "Assets/Resources/Seabed/Corals/Coral_Reef_Set_5/CoralReefSet5.fbx",
            "Assets/Resources/Seabed/Corals/Coral_Reef_Set_6/CoralReefSet6.fbx",
            "Assets/Resources/Seabed/Corals/Coral_Formation_Large/model.obj",
        };

        public static readonly string[] Rocks = new string[]
        {
            "Assets/Resources/Seabed/Rocks/Rock_Large/Rock_Large_1.fbx",
            "Assets/Resources/Seabed/Rocks/Rock_Medium/Rock_4.fbx",
            "Assets/Resources/Seabed/Rocks/Rock_Cluster/Rocks.fbx"
        };

        public static readonly string[] Corals = new string[]
        {
            "Assets/Resources/Seabed/Corals/Coral_Branch_Pink/Coral.obj",
            "Assets/Resources/Seabed/Corals/Coral_Branch_Orange/model.obj",
            "Assets/Resources/Seabed/Corals/Coral_Fan_Green/coral 3.obj",
            "Assets/Resources/Seabed/Corals/Coral_Cluster_Table/Coral_438.obj",
            "Assets/Resources/Seabed/Corals/Coral_Pillar/PUSHILIN_coral.obj"
        };

        public static readonly string[] KelpAndSeaweed = new string[]
        {
            "Assets/Resources/Seabed/Plants/Kelp_Tall_1/kelp 2.obj",
            "Assets/Resources/Seabed/Plants/Kelp_Tall_2/kelp 3.obj",
            "Assets/Resources/Seabed/Plants/Kelp_Tall_3/kelp 4.obj",
            "Assets/Resources/Seabed/Plants/Kelp_Tall_4/kelp 5.obj",
            "Assets/Resources/Seabed/Plants/Kelp_Bush/PUSHILIN_kelp.obj",
            "Assets/Resources/Seabed/Plants/Seaweed_Green_1/model.obj",
            "Assets/Resources/Seabed/Plants/Seaweed_Green_2/model.obj"
        };

        public static readonly string[] Anemones = new string[]
        {
            "Assets/Resources/Seabed/Anemones/Anemone_Pink_Tentacles/anemone.obj",
            "Assets/Resources/Seabed/Anemones/Anemone_Large/Anemone_1.obj",
            "Assets/Resources/Seabed/Anemones/Anemone_Red_Bulb/CHAHIN_ANEMONE.obj",
            "Assets/Resources/Seabed/Anemones/Anemone_Spotted/2_Anemone.obj"
        };

        public static readonly string[] Fauna = new string[]
        {
            "Assets/Resources/Seabed/Creatures/Starfish/starfish.obj",
            "Assets/Resources/Seabed/Creatures/Sea_Urchin/Mesh_SeaUrchin.obj",
            "Assets/Resources/Seabed/Creatures/Crab/Mesh_Crab.obj",
            "Assets/Resources/Seabed/Creatures/Turtle/NOVELO_TURTLE.obj",
            "Assets/Resources/Seabed/Creatures/Octopus/Octopus.obj",
            "Assets/Resources/Seabed/Creatures/Seahorse_Striped/seahorse.obj",
            "Assets/Resources/Seabed/Creatures/Seahorse_Yellow/model.obj",
            "Assets/Resources/Seabed/Shells/Seashell/seaShell2.obj"
        };

        public static readonly Color[] RockColors = new Color[]
        {
            new Color(0.42f, 0.45f, 0.48f),
            new Color(0.48f, 0.44f, 0.38f),
            new Color(0.36f, 0.39f, 0.37f),
            new Color(0.50f, 0.47f, 0.42f)
        };

        public static readonly Color[] CoralColors = new Color[]
        {
            new Color(0.96f, 0.38f, 0.44f),
            new Color(1.00f, 0.55f, 0.22f),
            new Color(0.70f, 0.38f, 0.88f),
            new Color(0.18f, 0.82f, 0.72f),
            new Color(0.98f, 0.82f, 0.25f),
            new Color(0.92f, 0.28f, 0.58f),
            new Color(0.38f, 0.82f, 0.46f)
        };

        public static readonly Color[] PlantColors = new Color[]
        {
            new Color(0.16f, 0.48f, 0.20f),
            new Color(0.22f, 0.55f, 0.18f),
            new Color(0.10f, 0.42f, 0.24f),
            new Color(0.30f, 0.62f, 0.24f)
        };

        public static readonly Color[] AnemoneColors = new Color[]
        {
            new Color(0.96f, 0.20f, 0.58f),
            new Color(0.99f, 0.42f, 0.30f),
            new Color(0.58f, 0.18f, 0.88f),
            new Color(0.25f, 0.82f, 0.92f)
        };

        public enum Category
        {
            Rock,
            CoralReef,
            Coral,
            Plant,
            Anemone,
            Fauna
        }

        // Shared material palette cache for GPU Instancing & SRP Batcher
        private static readonly Dictionary<string, Material> SharedMaterials = new Dictionary<string, Material>();

        [MenuItem("Aquarium/Decorate Seabed")]
        public static void Decorate()
        {
            var boundsComp = Object.FindAnyObjectByType<AquariumBounds>();
            Vector3 center = boundsComp != null ? boundsComp.transform.position : Vector3.zero;
            Vector3 size = boundsComp != null ? boundsComp.Size : new Vector3(50f, 18f, 30f);

            DecorateSeabedWithSettings(center, size, DecoratorSettings.Default);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            Debug.Log($"[SeabedDecorator] Successfully generated optimized seabed for bounds size {size}!");
        }

        [MenuItem("Aquarium/Optimize & Combine Seabed Meshes")]
        public static void OptimizeExistingSeabed()
        {
            GameObject seabed = GameObject.Find(RootName);
            if (seabed == null)
            {
                Debug.LogWarning("[SeabedDecorator] No seabed environment found to optimize.");
                return;
            }

            CombineSeabedSubgroups(seabed.transform);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
            );
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            Debug.Log("[SeabedDecorator] Successfully optimized and combined seabed meshes into high-performance batches!");
        }

        [MenuItem("Aquarium/Export Aquarium Prefab")]
        public static void ExportAquariumPrefab()
        {
            const string PrefabRootName = "AquariumEnvironment";
            GameObject existingPrefabRoot = GameObject.Find(PrefabRootName);
            if (existingPrefabRoot != null)
            {
                Undo.DestroyObjectImmediate(existingPrefabRoot);
            }

            GameObject prefabRoot = new GameObject(PrefabRootName);
            Undo.RegisterCreatedObjectUndo(prefabRoot, "Create Aquarium Prefab Root");

            GameObject controller = GameObject.Find("Aquarium Controller");
            if (controller != null)
            {
                controller.transform.SetParent(prefabRoot.transform, true);
            }

            GameObject seabed = GameObject.Find(RootName);
            if (seabed != null)
            {
                seabed.transform.SetParent(prefabRoot.transform, true);
            }

            GameObject sandFloor = GameObject.Find("Sand Floor");
            if (sandFloor != null)
            {
                sandFloor.transform.SetParent(prefabRoot.transform, true);
            }

            if (!Directory.Exists("Assets/Prefabs"))
            {
                Directory.CreateDirectory("Assets/Prefabs");
                AssetDatabase.Refresh();
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabExportPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[SeabedDecorator] Successfully created drag & drop Aquarium Prefab at: {PrefabExportPath}");
        }

        public static GameObject DecorateSeabedWithSettings(Vector3 center, Vector3 size, DecoratorSettings settings)
        {
            float floorY = center.y - (size.y * 0.5f);
            float swimW = size.x * 0.5f;
            float swimD = size.z * 0.5f;

            float extMultiplier = Mathf.Max(1.1f, settings.horizonPadding);
            float extW = swimW * extMultiplier;
            float extD = swimD * extMultiplier;

            SharedMaterials.Clear();
            UpdateMaterialsAndEnvironment();

            // 1. Ensure Sand Floor plane extends far beyond the horizon
            EnsureEndlessSandFloor(center, floorY, size);

            // 2. Setup Underwater Bubble Vents on Aquarium Controller
            GameObject controllerObj = GameObject.Find("Aquarium Controller");
            if (controllerObj != null)
            {
                var bubbleEffect = controllerObj.GetComponent<UnderwaterBubbleEffect>();
                if (bubbleEffect == null) bubbleEffect = controllerObj.AddComponent<UnderwaterBubbleEffect>();
                bubbleEffect.GenerateBubbleVents();
            }

            // 3. Remove existing seabed root
            GameObject existingRoot = GameObject.Find(RootName);
            if (existingRoot != null)
            {
                Undo.DestroyObjectImmediate(existingRoot);
            }

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Seabed Environment");

            Random.InitState(settings.seed);

            float d = Mathf.Max(0.1f, settings.globalDensity);

            // 4. Sub-groups for modularity
            Transform knollsGroup = CreateSubGroup(root.transform, "ReefKnolls");
            Transform coralsGroup = CreateSubGroup(root.transform, "CoralColonies");
            Transform kelpGroup = CreateSubGroup(root.transform, "KelpGroves");
            Transform anemoneGroup = CreateSubGroup(root.transform, "AnemoneFields");
            Transform faunaGroup = CreateSubGroup(root.transform, "BenthicFauna");
            Transform horizonGroup = CreateSubGroup(root.transform, "DistantHorizonReefs");

            // 5. Generate Organic Reef Knolls
            int baseKnollCount = Mathf.Max(3, Mathf.RoundToInt((size.x * size.z) / 140f * settings.knollMultiplier * d));
            List<Vector3> knollCenters = GenerateKnollPositions(center, swimW, swimD, baseKnollCount);

            foreach (var knollPos in knollCenters)
            {
                BuildReefKnoll(knollPos, floorY, knollsGroup, coralsGroup, anemoneGroup, settings.optimizeShadows);
            }

            // 6. Generate Kelp Groves
            int baseGroveCount = Mathf.Max(2, Mathf.RoundToInt((size.x * size.z) / 180f * settings.kelpMultiplier * d));
            for (int g = 0; g < baseGroveCount; g++)
            {
                Vector3 grovePos = new Vector3(
                    center.x + Random.Range(-swimW * 0.85f, swimW * 0.85f),
                    floorY,
                    center.z + Random.Range(-swimD * 0.85f, swimD * 0.85f)
                );
                BuildKelpGrove(grovePos, floorY, kelpGroup, settings.optimizeShadows);
            }

            // 7. Scatter Solitary Corals & Anemones across the open sand
            int openCoralCount = Mathf.RoundToInt(20 * d);
            for (int i = 0; i < openCoralCount; i++)
            {
                Vector3 pos = new Vector3(
                    center.x + Random.Range(-swimW * 0.9f, swimW * 0.9f),
                    floorY,
                    center.z + Random.Range(-swimD * 0.9f, swimD * 0.9f)
                );
                string coralPath = Corals[Random.Range(0, Corals.Length)];
                PlaceGroundedAsset(coralPath, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), Random.Range(1.2f, 2.2f), floorY, coralsGroup, Category.Coral, settings.optimizeShadows);
            }

            // 8. Scatter Benthic Fauna (Starfish, Crabs, Shells)
            int faunaCount = Mathf.RoundToInt(25 * settings.faunaMultiplier * d);
            for (int i = 0; i < faunaCount; i++)
            {
                Vector3 pos = new Vector3(
                    center.x + Random.Range(-swimW * 0.9f, swimW * 0.9f),
                    floorY,
                    center.z + Random.Range(-swimD * 0.9f, swimD * 0.9f)
                );
                string faunaPath = Fauna[Random.Range(0, Fauna.Length)];
                PlaceGroundedAsset(faunaPath, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), Random.Range(0.4f, 0.9f), floorY, faunaGroup, Category.Fauna, true);
            }

            // 9. Scatter Distant Horizon Silhouettes
            int horizonCount = Mathf.RoundToInt(18 * d);
            for (int i = 0; i < horizonCount; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float radiusW = Random.Range(swimW * 1.05f, extW);
                float radiusD = Random.Range(swimD * 1.05f, extD);

                Vector3 pos = new Vector3(
                    center.x + Mathf.Cos(angle) * radiusW,
                    floorY,
                    center.z + Mathf.Sin(angle) * radiusD
                );

                string asset = (i % 2 == 0) ? ReefSets[Random.Range(0, ReefSets.Length)] : Rocks[Random.Range(0, Rocks.Length)];
                PlaceGroundedAsset(asset, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), Random.Range(4.5f, 9.0f), floorY, horizonGroup, Category.Rock, settings.optimizeShadows);
            }

            // 10. Combine Meshes for Maximum Performance
            if (settings.combineMeshes)
            {
                CombineSeabedSubgroups(root.transform);
            }

            return root;
        }

        private static void BuildReefKnoll(Vector3 pos, float floorY, Transform knollParent, Transform coralParent, Transform anemoneParent, bool optimizeShadows)
        {
            int rockCount = Random.Range(2, 5);
            for (int i = 0; i < rockCount; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-1.8f, 1.8f), 0, Random.Range(-1.8f, 1.8f));
                string rockPath = Rocks[Random.Range(0, Rocks.Length)];
                float size = (i == 0) ? Random.Range(3.5f, 6.0f) : Random.Range(2.0f, 4.0f);
                PlaceGroundedAsset(rockPath, pos + offset, Quaternion.Euler(Random.Range(-10, 10), Random.Range(0, 360), Random.Range(-10, 10)), size, floorY, knollParent, Category.Rock, false);
            }

            if (Random.value < 0.7f)
            {
                string reefPath = ReefSets[Random.Range(0, ReefSets.Length)];
                PlaceGroundedAsset(reefPath, pos + new Vector3(Random.Range(-0.8f, 0.8f), 0, Random.Range(-0.8f, 0.8f)), Quaternion.Euler(0, Random.Range(0, 360), 0), Random.Range(3.0f, 5.0f), floorY, knollParent, Category.CoralReef, optimizeShadows);
            }

            int coralCount = Random.Range(3, 7);
            for (int i = 0; i < coralCount; i++)
            {
                Vector3 cOffset = new Vector3(Random.Range(-2.2f, 2.2f), 0, Random.Range(-2.2f, 2.2f));
                string coralPath = Corals[Random.Range(0, Corals.Length)];
                PlaceGroundedAsset(coralPath, pos + cOffset, Quaternion.Euler(Random.Range(-15, 15), Random.Range(0, 360), Random.Range(-15, 15)), Random.Range(1.0f, 2.2f), floorY, coralParent, Category.Coral, optimizeShadows);
            }

            int anemoneCount = Random.Range(1, 4);
            for (int i = 0; i < anemoneCount; i++)
            {
                Vector3 aOffset = new Vector3(Random.Range(-1.5f, 1.5f), 0, Random.Range(-1.5f, 1.5f));
                string anemonePath = Anemones[Random.Range(0, Anemones.Length)];
                PlaceGroundedAsset(anemonePath, pos + aOffset, Quaternion.Euler(0, Random.Range(0, 360), 0), Random.Range(0.6f, 1.2f), floorY, anemoneParent, Category.Anemone, true);
            }
        }

        private static void BuildKelpGrove(Vector3 pos, float floorY, Transform kelpParent, bool optimizeShadows)
        {
            int kelpCount = Random.Range(6, 12);
            for (int i = 0; i < kelpCount; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-2.2f, 2.2f), 0, Random.Range(-2.2f, 2.2f));
                string kelpPath = KelpAndSeaweed[Random.Range(0, KelpAndSeaweed.Length)];
                float size = Random.Range(3.5f, 7.5f);
                PlaceGroundedAsset(kelpPath, pos + offset, Quaternion.Euler(Random.Range(-5, 5), Random.Range(0, 360), Random.Range(-5, 5)), size, floorY, kelpParent, Category.Plant, optimizeShadows);
            }
        }

        private static List<Vector3> GenerateKnollPositions(Vector3 center, float halfW, float halfD, int count)
        {
            List<Vector3> positions = new List<Vector3>();
            int maxAttempts = count * 15;
            float minSpacing = Mathf.Min(halfW, halfD) * 0.45f;

            for (int attempt = 0; attempt < maxAttempts && positions.Count < count; attempt++)
            {
                Vector3 candidate = new Vector3(
                    center.x + Random.Range(-halfW * 0.85f, halfW * 0.85f),
                    0,
                    center.z + Random.Range(-halfD * 0.85f, halfD * 0.85f)
                );

                bool valid = true;
                foreach (var existing in positions)
                {
                    if (Vector3.Distance(candidate, existing) < minSpacing)
                    {
                        valid = false;
                        break;
                    }
                }

                if (valid) positions.Add(candidate);
            }

            return positions;
        }

        private static void EnsureEndlessSandFloor(Vector3 center, float floorY, Vector3 size)
        {
            const string SandFloorName = "Sand Floor";
            GameObject sandFloor = GameObject.Find(SandFloorName);
            if (sandFloor == null)
            {
                sandFloor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                sandFloor.name = SandFloorName;
                Undo.RegisterCreatedObjectUndo(sandFloor, "Create Sand Floor");
            }

            sandFloor.transform.position = new Vector3(center.x, floorY - 0.05f, center.z);
            float maxDim = Mathf.Max(size.x, size.z);
            float planeScale = (maxDim * 5.0f) / 10f;
            sandFloor.transform.localScale = new Vector3(planeScale, 1f, planeScale);

            string groundMatPath = "Assets/Materials/mGroundSand.mat";
            Material groundMat = AssetDatabase.LoadAssetAtPath<Material>(groundMatPath);
            if (groundMat != null)
            {
                groundMat.enableInstancing = true;
                sandFloor.GetComponent<MeshRenderer>().sharedMaterial = groundMat;
            }

            // Strip unnecessary colliders for performance
            var colliders = sandFloor.GetComponents<Collider>();
            foreach (var col in colliders) Object.DestroyImmediate(col);
        }

        private static void UpdateMaterialsAndEnvironment()
        {
            string sandMatPath = "Assets/Materials/mGroundSand.mat";
            Material sandMat = AssetDatabase.LoadAssetAtPath<Material>(sandMatPath);
            if (sandMat != null)
            {
                sandMat.enableInstancing = true;
                Color warmSand = new Color(0.78f, 0.72f, 0.58f);
                if (sandMat.HasProperty("_BaseColor")) sandMat.SetColor("_BaseColor", warmSand);
                if (sandMat.HasProperty("_Color")) sandMat.SetColor("_Color", warmSand);
                if (sandMat.HasProperty("_Smoothness")) sandMat.SetFloat("_Smoothness", 0.12f);
                EditorUtility.SetDirty(sandMat);
            }

            string skyMatPath = "Assets/Materials/mSkyboxMarine.mat";
            Material skyMat = AssetDatabase.LoadAssetAtPath<Material>(skyMatPath);
            if (skyMat != null)
            {
                if (skyMat.HasProperty("_TopColor")) skyMat.SetColor("_TopColor", new Color(0.12f, 0.45f, 0.72f));
                if (skyMat.HasProperty("_MiddleColor")) skyMat.SetColor("_MiddleColor", new Color(0.06f, 0.32f, 0.52f));
                if (skyMat.HasProperty("_BottomColor")) skyMat.SetColor("_BottomColor", new Color(0.04f, 0.20f, 0.32f));
                EditorUtility.SetDirty(skyMat);
            }

            AssetDatabase.SaveAssets();
        }

        private static Transform CreateSubGroup(Transform parent, string name)
        {
            GameObject sub = new GameObject(name);
            sub.transform.SetParent(parent, false);
            return sub.transform;
        }

        private static GameObject PlaceGroundedAsset(string assetPath, Vector3 targetPos, Quaternion rotation, float targetSize, float floorY, Transform parent, Category category, bool disableShadows)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null) return null;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (instance == null) instance = Object.Instantiate(prefab, parent);

            instance.name = prefab.name;
            instance.transform.position = targetPos;
            instance.transform.rotation = rotation;

            ApplyOptimizedStyling(instance, assetPath, category, disableShadows);

            // 1. Calculate unscaled true bounds
            Bounds initialBounds = CalculateTrueWorldBounds(instance);
            float maxDim = Mathf.Max(initialBounds.size.x, Mathf.Max(initialBounds.size.y, initialBounds.size.z));
            if (maxDim > 0.001f)
            {
                float scaleRatio = targetSize / maxDim;
                instance.transform.localScale = instance.transform.localScale * scaleRatio;
            }
            else
            {
                instance.transform.localScale = Vector3.one * targetSize;
            }

            // 2. Calculate true world bounds after scaling
            Bounds scaledBounds = CalculateTrueWorldBounds(instance);

            // Snug embedding depth into the sand so nothing floats or hovers
            float embedDepth = (category == Category.Rock || category == Category.CoralReef) ? 0.15f : 0.05f;
            float desiredBottomY = floorY - embedDepth;

            float yOffset = desiredBottomY - scaledBounds.min.y;
            instance.transform.position += new Vector3(0f, yOffset, 0f);

            // Remove physics colliders for performance
            var colliders = instance.GetComponentsInChildren<Collider>();
            foreach (var col in colliders) Object.DestroyImmediate(col);

            return instance;
        }

        private static Bounds CalculateTrueWorldBounds(GameObject go)
        {
            Bounds b = new Bounds();
            bool hasBounds = false;

            MeshFilter[] filters = go.GetComponentsInChildren<MeshFilter>();
            foreach (var mf in filters)
            {
                if (mf == null || mf.sharedMesh == null) continue;
                Bounds localB = mf.sharedMesh.bounds;
                Vector3 min = localB.min;
                Vector3 max = localB.max;

                Vector3[] corners = new Vector3[]
                {
                    new Vector3(min.x, min.y, min.z),
                    new Vector3(min.x, min.y, max.z),
                    new Vector3(min.x, max.y, min.z),
                    new Vector3(min.x, max.y, max.z),
                    new Vector3(max.x, min.y, min.z),
                    new Vector3(max.x, min.y, max.z),
                    new Vector3(max.x, max.y, min.z),
                    new Vector3(max.x, max.y, max.z),
                };

                Transform t = mf.transform;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 worldCorner = t.TransformPoint(corners[i]);
                    if (!hasBounds)
                    {
                        b = new Bounds(worldCorner, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        b.Encapsulate(worldCorner);
                    }
                }
            }

            return b;
        }

        private static void ApplyOptimizedStyling(GameObject instance, string assetPath, Category category, bool disableShadows)
        {
            string dir = Path.GetDirectoryName(assetPath);
            Texture2D foundTex = FindTextureInDirectory(dir);

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0) return;

            foreach (var rend in renderers)
            {
                if (disableShadows)
                {
                    rend.shadowCastingMode = ShadowCastingMode.Off;
                }

                Material[] mats = rend.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material m = mats[i];
                    bool isDefaultOrMissing = m == null || m.name.Contains("Default") || m.name.Contains("No Name") || m.shader == null || m.shader.name == "Hidden/InternalErrorShader";

                    if (foundTex != null)
                    {
                        mats[i] = GetOrCreateSharedTexturedMaterial(instance.name, foundTex);
                    }
                    else if (isDefaultOrMissing)
                    {
                        mats[i] = GetOrCreateSharedColoredMaterial(category, PickCategoryColor(category));
                    }
                }
                rend.sharedMaterials = mats;
            }
        }

        private static Material GetOrCreateSharedTexturedMaterial(string name, Texture2D texture)
        {
            string key = "Tex_" + texture.name;
            if (SharedMaterials.TryGetValue(key, out var mat) && mat != null)
            {
                return mat;
            }

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material newMat = new Material(urpLit);
            newMat.name = "M_Shared_" + name;
            newMat.enableInstancing = true;

            if (newMat.HasProperty("_BaseMap")) newMat.SetTexture("_BaseMap", texture);
            if (newMat.HasProperty("_MainTex")) newMat.SetTexture("_MainTex", texture);
            if (newMat.HasProperty("_Smoothness")) newMat.SetFloat("_Smoothness", 0.2f);

            SharedMaterials[key] = newMat;
            return newMat;
        }

        private static Material GetOrCreateSharedColoredMaterial(Category category, Color color)
        {
            string key = $"Col_{category}_{color.r:F2}_{color.g:F2}_{color.b:F2}";
            if (SharedMaterials.TryGetValue(key, out var mat) && mat != null)
            {
                return mat;
            }

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material newMat = new Material(urpLit);
            newMat.name = $"M_Shared_{category}_{SharedMaterials.Count}";
            newMat.enableInstancing = true;

            if (newMat.HasProperty("_BaseColor")) newMat.SetColor("_BaseColor", color);
            if (newMat.HasProperty("_Color")) newMat.SetColor("_Color", color);
            if (newMat.HasProperty("_Smoothness")) newMat.SetFloat("_Smoothness", category == Category.Rock ? 0.1f : 0.35f);

            SharedMaterials[key] = newMat;
            return newMat;
        }

        /// <summary>
        /// Combines all static meshes sharing materials into unified meshes per sub-group,
        /// dramatically reducing draw calls from hundreds down to ~5.
        /// </summary>
        public static void CombineSeabedSubgroups(Transform seabedRoot)
        {
            if (seabedRoot == null) return;

            for (int i = 0; i < seabedRoot.childCount; i++)
            {
                Transform subGroup = seabedRoot.GetChild(i);
                CombineGroupMeshes(subGroup);
            }
        }

        private static void CombineGroupMeshes(Transform group)
        {
            MeshFilter[] meshFilters = group.GetComponentsInChildren<MeshFilter>();
            if (meshFilters == null || meshFilters.Length <= 1) return;

            // Group meshes by shared material
            Dictionary<Material, List<CombineInstance>> materialToCombines = new Dictionary<Material, List<CombineInstance>>();

            Matrix4x4 groupWorldToLocal = group.worldToLocalMatrix;

            foreach (var mf in meshFilters)
            {
                if (mf == null || mf.sharedMesh == null) continue;
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || !mr.enabled) continue;

                Material mat = mr.sharedMaterial;
                if (mat == null) continue;

                if (!materialToCombines.ContainsKey(mat))
                {
                    materialToCombines[mat] = new List<CombineInstance>();
                }

                CombineInstance ci = new CombineInstance
                {
                    mesh = mf.sharedMesh,
                    transform = groupWorldToLocal * mf.transform.localToWorldMatrix
                };
                materialToCombines[mat].Add(ci);
            }

            // Destroy individual child objects
            List<GameObject> childrenToDestroy = new List<GameObject>();
            for (int i = 0; i < group.childCount; i++)
            {
                childrenToDestroy.Add(group.GetChild(i).gameObject);
            }
            foreach (var child in childrenToDestroy)
            {
                Object.DestroyImmediate(child);
            }

            // Create combined sub-mesh objects per material
            int batchIdx = 0;
            foreach (var kvp in materialToCombines)
            {
                Material mat = kvp.Key;
                List<CombineInstance> combines = kvp.Value;
                if (combines.Count == 0) continue;

                GameObject combinedObj = new GameObject($"Batch_{group.name}_{mat.name}_{batchIdx++}");
                combinedObj.transform.SetParent(group, false);
                combinedObj.transform.localPosition = Vector3.zero;
                combinedObj.transform.localRotation = Quaternion.identity;
                combinedObj.transform.localScale = Vector3.one;

                var mf = combinedObj.AddComponent<MeshFilter>();
                var mr = combinedObj.AddComponent<MeshRenderer>();

                Mesh combinedMesh = new Mesh();
                combinedMesh.name = $"Mesh_{combinedObj.name}";
                combinedMesh.indexFormat = IndexFormat.UInt32; // Support large mesh batches
                combinedMesh.CombineMeshes(combines.ToArray(), true, true);

                mf.sharedMesh = combinedMesh;
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = mat.name.Contains("Plant") || mat.name.Contains("Fauna")
                    ? ShadowCastingMode.Off
                    : ShadowCastingMode.On;
            }
        }

        private static Color PickCategoryColor(Category category)
        {
            switch (category)
            {
                case Category.Rock:
                    return RockColors[Random.Range(0, RockColors.Length)];
                case Category.CoralReef:
                case Category.Coral:
                    return CoralColors[Random.Range(0, CoralColors.Length)];
                case Category.Plant:
                    return PlantColors[Random.Range(0, PlantColors.Length)];
                case Category.Anemone:
                    return AnemoneColors[Random.Range(0, AnemoneColors.Length)];
                case Category.Fauna:
                    return new Color(0.9f, 0.5f, 0.3f);
                default:
                    return Color.gray;
            }
        }

        private static Texture2D FindTextureInDirectory(string dir)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return null;

            string[] exts = new string[] { "*.png", "*.jpg", "*.jpeg", "*.tga" };
            foreach (var ext in exts)
            {
                string[] files = Directory.GetFiles(dir, ext, SearchOption.TopDirectoryOnly);
                if (files != null && files.Length > 0)
                {
                    string unityPath = files[0].Replace('\\', '/');
                    Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(unityPath);
                    if (tex != null) return tex;
                }
            }
            return null;
        }
    }
}
