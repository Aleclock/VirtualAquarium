using System.Collections.Generic;
using UnityEngine;

namespace MultiCamCapture.Demo
{
    /// <summary>
    /// Spawns and configures schools of fish within the AquariumBounds using a FishCatalogSO asset.
    /// </summary>
    public class FishSpawner : MonoBehaviour
    {
        [Header("Target Volume")]
        [SerializeField] private AquariumBounds aquariumBounds;

        [Header("Fish Catalog")]
        [Tooltip("Catalog asset containing all configured fish schools and species presets")]
        [SerializeField] private FishCatalogSO fishCatalog;

        [Header("Spawn Settings")]
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private Transform spawnedContainer;

        public FishCatalogSO Catalog { get => fishCatalog; set => fishCatalog = value; }
        public AquariumBounds Bounds { get => aquariumBounds; set => aquariumBounds = value; }

        private void Start()
        {
            if (spawnOnStart)
            {
                SpawnAll();
            }
        }

        /// <summary>
        /// Primary spawn method. Spawns all schools defined in the assigned FishCatalogSO.
        /// </summary>
        [ContextMenu("Spawn All Schools")]
        public void SpawnAll()
        {
            EnsureBoundsAssigned();

            if (fishCatalog != null)
            {
                SpawnCatalog(fishCatalog);
            }
            else
            {
                Debug.LogWarning("[FishSpawner] No FishCatalogSO assigned to spawn.", this);
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
                SpawnRawPrefabs(catalog.RawPrefabs, catalog.RawPrefabCountRange);
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
            Transform parent = spawnedContainer != null ? spawnedContainer : transform;

            for (int i = 0; i < spawnCount; i++)
            {
                Vector3 spawnPos = aquariumBounds != null
                    ? aquariumBounds.GetRandomPosition(aquariumBounds.Margin, schoolSO.depthPreference)
                    : transform.position;

                Quaternion spawnRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                GameObject instance = Instantiate(schoolSO.fishPrefab, spawnPos, spawnRot, parent);
                instance.name = $"{schoolSO.speciesName}_{i:D2}";

                // Apply random scale jitter
                float scale = Random.Range(schoolSO.scaleMultiplierRange.x, schoolSO.scaleMultiplierRange.y);
                instance.transform.localScale *= scale;

                // Configure FishAgent
                if (!instance.TryGetComponent<FishAgent>(out var agent))
                {
                    agent = instance.AddComponent<FishAgent>();
                }

                agent.FlockId = schoolSO.flockId;
                agent.AquariumBounds = aquariumBounds;
                agent.MinSpeed = schoolSO.minSpeed;
                agent.MaxSpeed = schoolSO.maxSpeed;
                agent.IsPredator = schoolSO.isPredator;
                agent.ThreatDetectionRadius = schoolSO.threatDetectionRadius;
                agent.PanicSpeedMultiplier = schoolSO.panicSpeedMultiplier;
                agent.DepthPreference = schoolSO.depthPreference;
                agent.DepthSteeringWeight = schoolSO.depthSteeringWeight;
            }
        }

        private void SpawnRawPrefabs(IReadOnlyList<GameObject> prefabs, Vector2Int countRangePerPrefab)
        {
            if (prefabs == null) return;
            EnsureBoundsAssigned();
            Transform parent = spawnedContainer != null ? spawnedContainer : transform;

            for (int p = 0; p < prefabs.Count; p++)
            {
                GameObject prefab = prefabs[p];
                if (prefab == null) continue;

                int count = Random.Range(
                    Mathf.Max(1, countRangePerPrefab.x),
                    Mathf.Max(countRangePerPrefab.x, countRangePerPrefab.y) + 1
                );

                for (int i = 0; i < count; i++)
                {
                    Vector3 spawnPos = aquariumBounds != null
                        ? aquariumBounds.GetRandomPosition(aquariumBounds.Margin, DepthZone.FullColumn)
                        : transform.position;

                    Quaternion spawnRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                    GameObject instance = Instantiate(prefab, spawnPos, spawnRot, parent);
                    instance.name = $"{prefab.name}_{i:D2}";

                    if (!instance.TryGetComponent<FishAgent>(out var agent))
                    {
                        agent = instance.AddComponent<FishAgent>();
                    }

                    agent.FlockId = 100 + p;
                    agent.AquariumBounds = aquariumBounds;
                }
            }
        }

        private void EnsureBoundsAssigned()
        {
            if (aquariumBounds == null)
            {
                aquariumBounds = FindAnyObjectByType<AquariumBounds>();
                if (aquariumBounds == null)
                {
                    Debug.LogWarning("[FishSpawner] No AquariumBounds found in scene. Spawning around transform position.", this);
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
