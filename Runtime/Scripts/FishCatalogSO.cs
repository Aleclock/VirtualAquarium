using System.Collections.Generic;
using UnityEngine;

namespace MultiCamCapture.Demo
{
    /// <summary>
    /// ScriptableObject container grouping multiple FishSchoolSO templates or raw prefabs.
    /// Acts as a full aquarium population preset (e.g., Tropical Reef Catalog, Deep Sea Catalog).
    /// </summary>
    [CreateAssetMenu(fileName = "NewFishCatalog", menuName = "MultiCamCapture/Fish Catalog")]
    public class FishCatalogSO : ScriptableObject
    {
        [Header("Catalog Information")]
        [Tooltip("Descriptive name of this catalog / ecosystem preset")]
        public string catalogName = "Aquarium Catalog";

        [Header("Configured Schools & Individual Species")]
        [Tooltip("List of FishSchoolSO assets defining configured species and their population ranges")]
        [SerializeField] private List<FishSchoolSO> schools = new List<FishSchoolSO>();

        [Header("Quick Raw Prefabs (Auto-Configured)")]
        [Tooltip("Raw fish prefabs to spawn using default count range and behavior")]
        [SerializeField] private List<GameObject> rawPrefabs = new List<GameObject>();

        [Tooltip("Default population range applied per raw prefab when spawned")]
        [SerializeField] private Vector2Int rawPrefabCountRange = new Vector2Int(5, 15);

        public IReadOnlyList<FishSchoolSO> Schools => schools;
        public IReadOnlyList<GameObject> RawPrefabs => rawPrefabs;
        public Vector2Int RawPrefabCountRange => rawPrefabCountRange;

        public void AddSchool(FishSchoolSO school)
        {
            if (school != null && !schools.Contains(school))
            {
                schools.Add(school);
            }
        }

        public void AddRawPrefab(GameObject prefab)
        {
            if (prefab != null && !rawPrefabs.Contains(prefab))
            {
                rawPrefabs.Add(prefab);
            }
        }
    }
}
