using UnityEngine;

namespace MultiCamCapture.Demo
{
    /// <summary>
    /// Defines the 3D bounding area of the aquarium tank.
    /// Provides spatial boundaries and steering forces for fish to remain inside.
    /// </summary>
    [ExecuteAlways]
    public class AquariumBounds : MonoBehaviour
    {
        [Header("Volume Dimensions")]
        [Tooltip("Size of the bounding box (width, height, depth)")]
        [SerializeField] private Vector3 size = new Vector3(20f, 8f, 15f);

        [Tooltip("Center offset relative to this GameObject's position")]
        [SerializeField] private Vector3 centerOffset = Vector3.zero;

        [Header("Boundary Repulsion")]
        [Tooltip("Distance from the edge where fish start feeling the steering force inward")]
        [SerializeField] private float margin = 2.5f;

        [Tooltip("Strength of the force pushing fish back inside")]
        [SerializeField] private float boundaryRepulsionWeight = 5f;

        [Header("Gizmo Visualization")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private Color gizmoColor = new Color(0.2f, 0.7f, 1f, 0.4f);
        [SerializeField] private Color marginColor = new Color(1f, 0.8f, 0.2f, 0.15f);

        public Bounds BoundingBox => new Bounds(transform.position + centerOffset, size);
        public Vector3 Size => size;
        public float Margin => margin;
        public float BoundaryRepulsionWeight => boundaryRepulsionWeight;

        /// <summary>
        /// Returns a random 3D position inside the bounding volume (respecting margins).
        /// </summary>
        public Vector3 GetRandomPosition(float innerMargin = 0f)
        {
            Vector3 center = transform.position + centerOffset;
            Vector3 innerSize = new Vector3(
                Mathf.Max(0.1f, size.x - (innerMargin * 2f)),
                Mathf.Max(0.1f, size.y - (innerMargin * 2f)),
                Mathf.Max(0.1f, size.z - (innerMargin * 2f))
            );

            return new Vector3(
                center.x + Random.Range(-innerSize.x * 0.5f, innerSize.x * 0.5f),
                center.y + Random.Range(-innerSize.y * 0.5f, innerSize.y * 0.5f),
                center.z + Random.Range(-innerSize.z * 0.5f, innerSize.z * 0.5f)
            );
        }

        /// <summary>
        /// Calculates a steering vector pushing an agent inward if it crosses the soft margin.
        /// </summary>
        public Vector3 CalculateBoundarySteering(Vector3 position)
        {
            Bounds b = BoundingBox;
            Vector3 steer = Vector3.zero;

            // X axis
            float distMinX = position.x - b.min.x;
            float distMaxX = b.max.x - position.x;
            if (distMinX < margin) steer.x += (margin - distMinX) / margin;
            if (distMaxX < margin) steer.x -= (margin - distMaxX) / margin;

            // Y axis
            float distMinY = position.y - b.min.y;
            float distMaxY = b.max.y - position.y;
            if (distMinY < margin) steer.y += (margin - distMinY) / margin;
            if (distMaxY < margin) steer.y -= (margin - distMaxY) / margin;

            // Z axis
            float distMinZ = position.z - b.min.z;
            float distMaxZ = b.max.z - position.z;
            if (distMinZ < margin) steer.z += (margin - distMinZ) / margin;
            if (distMaxZ < margin) steer.z -= (margin - distMaxZ) / margin;

            return steer * boundaryRepulsionWeight;
        }

        /// <summary>
        /// Checks if a given position is strictly within the box bounds.
        /// </summary>
        public bool Contains(Vector3 position)
        {
            return BoundingBox.Contains(position);
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;

            Vector3 center = transform.position + centerOffset;

            // Outer Bounds
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(center, size);

            // Margin Bounds (Soft Repulsion Zone)
            if (margin > 0)
            {
                Gizmos.color = marginColor;
                Vector3 innerSize = new Vector3(
                    Mathf.Max(0.1f, size.x - (margin * 2f)),
                    Mathf.Max(0.1f, size.y - (margin * 2f)),
                    Mathf.Max(0.1f, size.z - (margin * 2f))
                );
                Gizmos.DrawWireCube(center, innerSize);
            }
        }
    }
}
