using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiCamCapture.Demo
{
    /// <summary>
    /// Spawns and configures schools of fish within the AquariumBounds.
    /// Supports ScriptableObject catalogs (FishCatalogSO), individual school assets (FishSchoolSO),
    /// raw prefabs, and inline inspector configurations.
    /// </summary>
    public class FishSpawner : MonoBehaviour
    {
        [Serializable]
        public class FishSchoolConfig
        {
            [Tooltip("Descriptive name for this flock / species")]
            public string schoolName = "Fish School";

            [Tooltip("Fish prefab with FishAgent component (or raw fish FBX prefab)")]
            public GameObject fishPrefab;

            [Tooltip("Population range (min, max) to spawn for this school. Use (1,1) for individual fish.")]
            public Vector2Int countRange = new Vector2Int(15, 30);

            [Tooltip("Flock ID. Fish with the same Flock ID will school together.")]
            public int flockId = 0;

            [Header("Speed Overrides")]
            public float minSpeed = 1.5f;
            public float maxSpeed = 3.5f;

            [Header("Scale Variation")]
            public Vector2 scaleMultiplierRange = new Vector2(0.85f, 1.15f);

            public int GetRandomCount()
            {
                int min = Mathf.Max(1, countRange.x);
                int max = Mathf.Max(min, countRange.y);
                return UnityEngine.Random.Range(min, max + 1);
            }
        }

        [Header("Target Volume")]
        [SerializeField] private AquariumBounds aquariumBounds;

        [Header("ScriptableObject Catalog (Recommended)")]
        [Tooltip("Optional preset catalog asset containing fish school ScriptableObjects and raw prefabs")]
        [SerializeField] private FishCatalogSO fishCatalog;

        [Tooltip("Optional list of standalone FishSchoolSO assets to spawn")]
        [SerializeField] private List<FishSchoolSO> schoolAssets = new List<FishSchoolSO>();

        [Header("Inline Schools (Quick Setup)")]
        [SerializeField] private List<FishSchoolConfig> inlineSchools = new List<FishSchoolConfig>();

        [Header("Spawn Settings")]
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private Transform spawnedContainer;

        public FishCatalogSO Catalog { get => fishCatalog; set => fishCatalog = value; }
        public List<FishSchoolSO> SchoolAssets => schoolAssets;
        public AquariumBounds Bounds { get => aquariumBounds; set => aquariumBounds = value; }

        private void Start()
        {
            if (spawnOnStart)
            {
                SpawnAll();
            }
        }

        /// <summary>
        /// Primary spawn method. Spawns assigned catalog, school assets, and inline school configs.
        /// </summary>
        [ContextMenu("Spawn All Schools")]
        public void SpawnAll()
        {
            EnsureBoundsAssigned();

            // 1. Spawn Catalog if assigned
            if (fishCatalog != null)
            {
                SpawnCatalog(fishCatalog);
            }

            // 2. Spawn Standalone School Assets
            for (int i = 0; i < schoolAssets.Count; i++)
            {
                if (schoolAssets[i] != null)
                {
                    SpawnSchool(schoolAssets[i]);
                }
            }

            // 3. Spawn Inline Inspector Configurations
            for (int i = 0; i < inlineSchools.Count; i++)
            {
                SpawnInlineSchool(inlineSchools[i]);
            }
        }

        /// <summary>
        /// Programmatically spawns all schools and raw prefabs defined in a FishCatalogSO asset.
        /// </summary>
        public void SpawnCatalog(FishCatalogSO catalog)
        {
            if (catalog == null) return;
            EnsureBoundsAssigned();

            // Spawn configured schools
            for (int i = 0; i < catalog.Schools.Count; i++)
            {
                FishSchoolSO school = catalog.Schools[i];
                if (school != null)
                {
                    SpawnSchool(school);
                }
            }

            // Spawn raw prefabs using catalog's default count range
            if (catalog.RawPrefabs.Count > 0)
            {
                SpawnPrefabs(catalog.RawPrefabs, catalog.RawPrefabCountRange);
            }
        }

        /// <summary>
        /// Programmatically spawns a single FishSchoolSO asset within the aquarium volume.
        /// </summary>
        public void SpawnSchool(FishSchoolSO schoolSO)
        {
            if (schoolSO == null || schoolSO.fishPrefab == null) return;
            EnsureBoundsAssigned();

            int spawnCount = schoolSO.GetRandomCount();
            SpawnFishGroup(
                schoolSO.fishPrefab,
                schoolSO.speciesName,
                spawnCount,
                schoolSO.flockId,
                schoolSO.minSpeed,
                schoolSO.maxSpeed,
                schoolSO.scaleMultiplierRange
            );
        }

        /// <summary>
        /// Spawns a collection of raw fish prefabs with a specified population count range.
        /// </summary>
        public void SpawnPrefabs(IEnumerable<GameObject> prefabs, Vector2Int countRangePerPrefab, int flockId = 0)
        {
            if (prefabs == null) return;
            EnsureBoundsAssigned();

            int index = 0;
            foreach (var prefab in prefabs)
            {
                if (prefab == null) continue;

                int count = UnityEngine.Random.Range(
                    Mathf.Max(1, countRangePerPrefab.x),
                    Mathf.Max(countRangePerPrefab.x, countRangePerPrefab.y) + 1
                );

                SpawnFishGroup(
                    prefab,
                    prefab.name,
                    count,
                    flockId + index,
                    1.5f,
                    3.5f,
                    new Vector2(0.85f, 1.15f)
                );
                index++;
            }
        }

        private void SpawnInlineSchool(FishSchoolConfig config)
        {
            if (config == null || config.fishPrefab == null) return;

            int count = config.GetRandomCount();
            SpawnFishGroup(
                config.fishPrefab,
                config.schoolName,
                count,
                config.flockId,
                config.minSpeed,
                config.maxSpeed,
                config.scaleMultiplierRange
            );
        }

        private void SpawnFishGroup(
            GameObject prefab,
            string groupName,
            int count,
            int flockId,
            float minSpeed,
            float maxSpeed,
            Vector2 scaleRange)
        {
            Transform parent = spawnedContainer != null ? spawnedContainer : transform;

            for (int i = 0; i < count; i++)
            {
                Vector3 spawnPos = aquariumBounds != null 
                    ? aquariumBounds.GetRandomPosition(aquariumBounds.Margin) 
                    : transform.position;

                Quaternion spawnRot = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

                GameObject instance = Instantiate(prefab, spawnPos, spawnRot, parent);
                instance.name = $"{groupName}_{i:D2}";

                // Apply random scale jitter
                float scale = UnityEngine.Random.Range(scaleRange.x, scaleRange.y);
                instance.transform.localScale *= scale;

                // Ensure FishAgent component is attached and initialized
                if (!instance.TryGetComponent<FishAgent>(out var agent))
                {
                    agent = instance.AddComponent<FishAgent>();
                }

                agent.FlockId = flockId;
                agent.AquariumBounds = aquariumBounds;
                agent.MinSpeed = minSpeed;
                agent.MaxSpeed = maxSpeed;
            }
        }

        private void EnsureBoundsAssigned()
        {
            if (aquariumBounds == null)
            {
                aquariumBounds = FindFirstObjectByType<AquariumBounds>();
                if (aquariumBounds == null)
                {
                    Debug.LogWarning("[FishSpawner] No AquariumBounds found in scene. Spawning around transform position.");
                }
            }
        }

        [ContextMenu("Clear Spawned Fish")]
        public void ClearSpawnedFish()
        {
            Transform parent = spawnedContainer != null ? spawnedContainer : transform;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }
    }
}
