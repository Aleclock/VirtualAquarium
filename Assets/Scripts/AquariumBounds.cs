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
        /// Returns the min and max Y bounds for a specific depth zone.
        /// </summary>
        public Vector2 GetDepthRange(DepthZone zone)
        {
            Bounds b = BoundingBox;
            float minY = b.min.y;
            float maxY = b.max.y;
            float totalH = maxY - minY;

            switch (zone)
            {
                case DepthZone.Surface:
                    return new Vector2(minY + totalH * 0.70f, maxY);
                case DepthZone.MidWater:
                    return new Vector2(minY + totalH * 0.25f, minY + totalH * 0.75f);
                case DepthZone.Bottom:
                    return new Vector2(minY, minY + totalH * 0.30f);
                case DepthZone.FullColumn:
                default:
                    return new Vector2(minY, maxY);
            }
        }

        /// <summary>
        /// Returns a random 3D position inside the bounding volume (respecting margins and optional depth zone).
        /// </summary>
        public Vector3 GetRandomPosition(float innerMargin = 0f, DepthZone zone = DepthZone.FullColumn)
        {
            Vector3 center = transform.position + centerOffset;
            Vector3 innerSize = new Vector3(
                Mathf.Max(0.1f, size.x - (innerMargin * 2f)),
                Mathf.Max(0.1f, size.y - (innerMargin * 2f)),
                Mathf.Max(0.1f, size.z - (innerMargin * 2f))
            );

            float posX = center.x + Random.Range(-innerSize.x * 0.5f, innerSize.x * 0.5f);
            float posZ = center.z + Random.Range(-innerSize.z * 0.5f, innerSize.z * 0.5f);

            float posY;
            if (zone == DepthZone.FullColumn)
            {
                posY = center.y + Random.Range(-innerSize.y * 0.5f, innerSize.y * 0.5f);
            }
            else
            {
                Vector2 yRange = GetDepthRange(zone);
                float clampedMinY = Mathf.Min(yRange.x + innerMargin, yRange.y);
                float clampedMaxY = Mathf.Max(yRange.y - innerMargin, clampedMinY);
                posY = Random.Range(clampedMinY, clampedMaxY);
            }

            return new Vector3(posX, posY, posZ);
        }

        /// <summary>
        /// Calculates vertical steering force to keep an agent within its preferred depth zone.
        /// </summary>
        public Vector3 CalculateDepthSteering(Vector3 position, DepthZone zone, float weight)
        {
            if (zone == DepthZone.FullColumn || weight <= 0f) return Vector3.zero;

            Vector2 range = GetDepthRange(zone);
            Vector3 steer = Vector3.zero;

            if (position.y < range.x)
            {
                float diff = range.x - position.y;
                steer.y = Mathf.Clamp01(diff / margin) * weight;
            }
            else if (position.y > range.y)
            {
                float diff = position.y - range.y;
                steer.y = -Mathf.Clamp01(diff / margin) * weight;
            }

            return steer;
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
