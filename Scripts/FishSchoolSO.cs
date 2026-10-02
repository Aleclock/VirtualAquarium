using UnityEngine;

namespace MultiCamCapture.Demo
{
    /// <summary>
    /// ScriptableObject template defining a fish species or school configuration.
    /// Supports both schooling species (count range > 1) and solo/individual fish (count range = [1,1]).
    /// </summary>
    [CreateAssetMenu(fileName = "NewFishSchool", menuName = "MultiCamCapture/Fish School")]
    public class FishSchoolSO : ScriptableObject
    {
        [Header("Species Identity")]
        [Tooltip("Descriptive name for this fish species / school")]
        public string speciesName = "Fish Species";

        [Tooltip("Fish prefab with FishAgent component (or raw fish FBX prefab)")]
        public GameObject fishPrefab;

        [Header("Population Range")]
        [Tooltip("Min and Max number of fish to spawn. Use (1, 1) for solo/individual fish, or (15, 40) for schools.")]
        public Vector2Int countRange = new Vector2Int(15, 30);

        [Header("Flocking & Speed Settings")]
        [Tooltip("Flock ID. Fish with the same Flock ID school together.")]
        public int flockId = 0;

        [Tooltip("Minimum swim speed for fish in this school")]
        public float minSpeed = 1.5f;

        [Tooltip("Maximum swim speed for fish in this school")]
        public float maxSpeed = 3.5f;

        [Header("Scale Variation")]
        [Tooltip("Random scale multiplier range applied upon spawning")]
        public Vector2 scaleMultiplierRange = new Vector2(0.85f, 1.15f);

        /// <summary>
        /// Evaluates a random count within [countRange.x, countRange.y] (inclusive).
        /// </summary>
        public int GetRandomCount()
        {
            int min = Mathf.Max(1, countRange.x);
            int max = Mathf.Max(min, countRange.y);
            return Random.Range(min, max + 1);
        }
    }
}
