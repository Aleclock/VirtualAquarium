using System.Collections.Generic;
using UnityEngine;

namespace MultiCamCapture.Demo
{
    /// <summary>
    /// Autonomous 3D Boid agent representing a swimming fish.
    /// Handles steering (separation, alignment, cohesion, boundary avoidance),
    /// banking/roll tilt, natural wander noise, and animation synchronization.
    /// </summary>
    [SelectionBase]
    public class FishAgent : MonoBehaviour
    {
        // Global registry of active fish agents for flocking calculations
        private static readonly List<FishAgent> AllFish = new List<FishAgent>();
        public static IReadOnlyList<FishAgent> ActiveFish => AllFish;

        [Header("Flock Grouping")]
        [Tooltip("Flock/Species ID. Fish will flock predominantly with the same group.")]
        [SerializeField] private int flockId = 0;

        [Header("Speed & Movement")]
        [SerializeField] private float minSpeed = 1.5f;
        [SerializeField] private float maxSpeed = 4.0f;
        [SerializeField] private float rotationSpeed = 3.5f;
        [SerializeField] private float acceleration = 2.0f;

        [Header("Flocking Weights (Boids)")]
        [SerializeField] private float neighborRadius = 4.0f;
        [SerializeField] private float separationRadius = 1.5f;
        [SerializeField] private float separationWeight = 2.5f;
        [SerializeField] private float alignmentWeight = 1.2f;
        [SerializeField] private float cohesionWeight = 1.0f;

        [Header("Natural Swimming Behavior")]
        [Tooltip("Strength of random wandering force so fish don't swim rigidly")]
        [SerializeField] private float wanderWeight = 0.8f;
        [Tooltip("Max roll angle (banking) when turning")]
        [SerializeField] private float maxBankAngle = 25f;
        [Tooltip("Speed of banking transition")]
        [SerializeField] private float bankSpeed = 4f;

        [Header("Visual / Animation Settings")]
        [Tooltip("Optional child transform holding the mesh for banking roll tilt (leave null to use root transform)")]
        [SerializeField] private Transform visualRoot;
        [Tooltip("Animator to drive tail swimming speed based on velocity")]
        [SerializeField] private Animator fishAnimator;
        [Tooltip("Baseline animation playback speed multiplier")]
        [SerializeField] private float animSpeedMultiplier = 1.0f;
        [Tooltip("Animator float parameter name for movement speed (optional)")]
        [SerializeField] private string speedParamName = "Speed";

        [Header("Boundaries")]
        [SerializeField] private AquariumBounds aquariumBounds;

        // Runtime state
        private Vector3 currentVelocity;
        private float currentSpeed;
        private float noiseOffset;
        private int speedParamHash;
        private bool hasSpeedParam;
        private float currentBankAngle;

        public int FlockId { get => flockId; set => flockId = value; }
        public AquariumBounds AquariumBounds { get => aquariumBounds; set => aquariumBounds = value; }
        public float MinSpeed { get => minSpeed; set => minSpeed = value; }
        public float MaxSpeed { get => maxSpeed; set => maxSpeed = value; }

        private void Awake()
        {
            if (visualRoot == null)
            {
                visualRoot = transform;
            }

            if (fishAnimator == null)
            {
                fishAnimator = GetComponentInChildren<Animator>();
            }

            if (fishAnimator != null && !string.IsNullOrEmpty(speedParamName))
            {
                speedParamHash = Animator.StringToHash(speedParamName);
                // Check if parameter exists in controller
                foreach (var param in fishAnimator.parameters)
                {
                    if (param.nameHash == speedParamHash)
                    {
                        hasSpeedParam = true;
                        break;
                    }
                }
            }

            noiseOffset = Random.Range(0f, 1000f);
            currentSpeed = Random.Range(minSpeed, maxSpeed);
            currentVelocity = transform.forward * currentSpeed;
        }

        private void OnEnable()
        {
            AllFish.Add(this);

            // Randomize animation starting playback offset so all fish aren't beating tails identically
            if (fishAnimator != null && fishAnimator.runtimeAnimatorController != null)
            {
                fishAnimator.Update(Random.Range(0f, 2f));
            }

            // Auto-find bounds if not assigned
            if (aquariumBounds == null)
            {
                aquariumBounds = FindFirstObjectByType<AquariumBounds>();
            }
        }

        private void OnDisable()
        {
            AllFish.Remove(this);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector3 accelerationVector = CalculateSteeringForces();

            // Apply acceleration to velocity
            currentVelocity += accelerationVector * dt;

            // Target speed calculation (slow slightly when turning sharply)
            float targetSpeed = Mathf.Lerp(minSpeed, maxSpeed, 0.7f);
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * dt);
            currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);

            if (currentVelocity.sqrMagnitude > 0.001f)
            {
                currentVelocity = currentVelocity.normalized * currentSpeed;
            }
            else
            {
                currentVelocity = transform.forward * currentSpeed;
            }

            // Update Position
            transform.position += currentVelocity * dt;

            // Update Heading Rotation (Yaw & Pitch)
            if (currentVelocity.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(currentVelocity, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * dt);
            }

            // Apply organic Banking (Roll tilt) to visual mesh based on turning angular velocity
            ApplyBanking(dt);

            // Synchronize Animator
            UpdateAnimation();
        }

        private Vector3 CalculateSteeringForces()
        {
            Vector3 separation = Vector3.zero;
            Vector3 alignment = Vector3.zero;
            Vector3 cohesion = Vector3.zero;
            Vector3 neighborCenter = Vector3.zero;

            int neighborCount = 0;
            int separationCount = 0;

            Vector3 myPos = transform.position;

            for (int i = 0; i < AllFish.Count; i++)
            {
                FishAgent other = AllFish[i];
                if (other == this) continue;

                Vector3 diff = other.transform.position - myPos;
                float sqrDist = diff.sqrMagnitude;

                // Check general flocking neighborhood
                if (sqrDist < neighborRadius * neighborRadius)
                {
                    // If same flock species, align and cohere
                    if (other.flockId == this.flockId)
                    {
                        alignment += other.currentVelocity;
                        neighborCenter += other.transform.position;
                        neighborCount++;
                    }

                    // Separation applies to all nearby fish regardless of species
                    if (sqrDist < separationRadius * separationRadius && sqrDist > 0.0001f)
                    {
                        float dist = Mathf.Sqrt(sqrDist);
                        // Repulsion vector inversely proportional to distance
                        separation -= (diff / dist) * (1.0f - (dist / separationRadius));
                        separationCount++;
                    }
                }
            }

            Vector3 totalForce = Vector3.zero;

            // 1. Separation
            if (separationCount > 0)
            {
                separation /= separationCount;
                totalForce += separation.normalized * separationWeight;
            }

            // 2. Alignment & Cohesion
            if (neighborCount > 0)
            {
                alignment /= neighborCount;
                Vector3 alignForce = (alignment - currentVelocity).normalized;
                totalForce += alignForce * alignmentWeight;

                neighborCenter /= neighborCount;
                Vector3 cohesionDir = (neighborCenter - myPos).normalized;
                totalForce += cohesionDir * cohesionWeight;
            }

            // 3. Natural Wander Noise (3D Perlin-like oscillation)
            float time = Time.time * 0.4f + noiseOffset;
            Vector3 wanderNoise = new Vector3(
                Mathf.Sin(time * 1.7f) + Mathf.Cos(time * 0.9f),
                Mathf.Sin(time * 1.1f) * 0.5f,
                Mathf.Cos(time * 1.3f) + Mathf.Sin(time * 0.6f)
            );
            totalForce += wanderNoise * wanderWeight;

            // 4. Aquarium Boundaries steering
            if (aquariumBounds != null)
            {
                Vector3 boundarySteer = aquariumBounds.CalculateBoundarySteering(myPos);
                totalForce += boundarySteer;
            }

            return totalForce;
        }

        private void ApplyBanking(float dt)
        {
            if (visualRoot == null) return;

            // Calculate horizontal turn direction relative to local space
            Vector3 localVelocity = transform.InverseTransformDirection(currentVelocity);
            float horizontalTurn = Mathf.Clamp(localVelocity.x, -1f, 1f);

            // Desired bank roll angle (tilting into turn)
            float targetBank = -horizontalTurn * maxBankAngle;
            currentBankAngle = Mathf.Lerp(currentBankAngle, targetBank, bankSpeed * dt);

            if (visualRoot != transform)
            {
                visualRoot.localRotation = Quaternion.Euler(0f, 0f, currentBankAngle);
            }
        }

        private void UpdateAnimation()
        {
            if (fishAnimator == null) return;

            // Scale animation playback rate based on travel velocity
            float normalizedSpeed = currentSpeed / Mathf.Max(0.1f, maxSpeed);
            fishAnimator.speed = normalizedSpeed * animSpeedMultiplier;

            if (hasSpeedParam)
            {
                fishAnimator.SetFloat(speedParamHash, currentSpeed);
            }
        }
    }
}
