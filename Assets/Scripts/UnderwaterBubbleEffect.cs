using UnityEngine;

namespace MultiCamCapture.Demo
{
    /// <summary>
    /// Spawns and manages prominent, glowing rising air bubble particle streams from rock vents and reef crevices.
    /// Features crisp rim lighting, specular glints, organic wobble, and adaptive height scaling.
    /// </summary>
    [ExecuteAlways]
    public class UnderwaterBubbleEffect : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AquariumBounds aquariumBounds;

        [Header("Bubble Vents")]
        [Tooltip("Number of distinct bubble vent columns across the seabed")]
        [Range(2, 12)]
        [SerializeField] private int ventCount = 8;

        [Header("Bubble Physics")]
        [SerializeField] private float minBubbleSpeed = 2.5f;
        [SerializeField] private float maxBubbleSpeed = 4.8f;
        [SerializeField] private float minBubbleSize = 0.35f;
        [SerializeField] private float maxBubbleSize = 1.05f;
        [SerializeField] private float emissionRatePerVent = 35f;

        [Header("Appearance")]
        [SerializeField] private Color bubbleColor = new Color(1f, 1f, 1f, 0.95f);

        private const string VentsContainerName = "[Bubble Vents]";

        private void Start()
        {
            if (aquariumBounds == null)
            {
                aquariumBounds = Object.FindAnyObjectByType<AquariumBounds>();
            }

            Transform container = transform.Find(VentsContainerName);
            if (container == null || container.childCount == 0)
            {
                GenerateBubbleVents();
            }
            else
            {
                PlayAllVents();
            }
        }

        private void OnEnable()
        {
            PlayAllVents();
        }

        public void PlayAllVents()
        {
            Transform container = transform.Find(VentsContainerName);
            if (container != null)
            {
                var particleSystems = container.GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in particleSystems)
                {
                    if (ps != null && !ps.isPlaying)
                    {
                        ps.Play();
                    }
                }
            }
        }

        [ContextMenu("Regenerate Bubble Vents")]
        public void GenerateBubbleVents()
        {
            if (aquariumBounds == null)
            {
                aquariumBounds = Object.FindAnyObjectByType<AquariumBounds>();
            }

            Vector3 center = aquariumBounds != null ? aquariumBounds.transform.position : Vector3.zero;
            Vector3 size = aquariumBounds != null ? aquariumBounds.Size : new Vector3(50f, 18f, 30f);

            float floorY = center.y - (size.y * 0.5f);
            float surfaceY = center.y + (size.y * 0.5f);
            float totalHeight = Mathf.Max(2f, surfaceY - floorY);

            // Clean up existing container
            Transform existing = transform.Find(VentsContainerName);
            if (existing != null)
            {
                if (Application.isPlaying)
                    Destroy(existing.gameObject);
                else
                    DestroyImmediate(existing.gameObject);
            }

            GameObject container = new GameObject(VentsContainerName);
            container.transform.SetParent(transform, false);

            Material bubbleMat = GetOrCreateBubbleMaterial();

            // Prominent vent locations distributed across foreground, midground, and background
            Vector3[] ventOffsets = new Vector3[]
            {
                // Foreground / Midground (Highly visible to camera)
                new Vector3(-size.x * 0.20f, 0f, -size.z * 0.22f),
                new Vector3( size.x * 0.18f, 0f, -size.z * 0.18f),
                new Vector3(-size.x * 0.02f, 0f, -size.z * 0.30f),

                // Midground reefs
                new Vector3(-size.x * 0.32f, 0f,  size.z * 0.05f),
                new Vector3( size.x * 0.28f, 0f,  size.z * 0.10f),
                new Vector3( 0.05f,          0f,  size.z * 0.18f),

                // Background ridge
                new Vector3(-size.x * 0.16f, 0f,  size.z * 0.38f),
                new Vector3( size.x * 0.14f, 0f,  size.z * 0.35f)
            };

            int count = Mathf.Min(ventCount, ventOffsets.Length);
            for (int i = 0; i < count; i++)
            {
                Vector3 ventPos = new Vector3(
                    center.x + ventOffsets[i].x,
                    floorY + 0.15f,
                    center.z + ventOffsets[i].z
                );

                GameObject ventObj = new GameObject($"Bubble_Vent_{i + 1}");
                ventObj.transform.SetParent(container.transform, false);
                ventObj.transform.position = ventPos;

                CreateBubbleParticleSystem(ventObj, bubbleMat, totalHeight);
            }
        }

        private void CreateBubbleParticleSystem(GameObject parent, Material mat, float columnHeight)
        {
            ParticleSystem ps = parent.AddComponent<ParticleSystem>();
            ParticleSystemRenderer psRenderer = parent.GetComponent<ParticleSystemRenderer>();

            if (mat != null && psRenderer != null)
            {
                psRenderer.sharedMaterial = mat;
                psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                psRenderer.sortMode = ParticleSystemSortMode.Distance;
            }

            // 1. Main Module
            var main = ps.main;
            float avgSpeed = (minBubbleSpeed + maxBubbleSpeed) * 0.5f;
            float lifetime = columnHeight / avgSpeed;

            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.85f, lifetime * 1.15f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(minBubbleSpeed, maxBubbleSpeed);
            main.startSize = new ParticleSystem.MinMaxCurve(minBubbleSize, maxBubbleSize);
            main.startColor = bubbleColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = true;
            main.maxParticles = 500;

            // 2. Emission Module
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = emissionRatePerVent + Random.Range(-4f, 8f);

            // 3. Shape Module (circular vent on rock base)
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.40f;
            shape.rotation = new Vector3(-90f, 0f, 0f); // Point upwards

            // 4. Velocity over Lifetime (Wobble drift - all axes share TwoConstants mode)
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
            velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);

            // 5. Size over Lifetime (grows as it ascends towards surface)
            var sizeModule = ps.sizeOverLifetime;
            sizeModule.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.70f);
            sizeCurve.AddKey(0.85f, 1.30f);
            sizeCurve.AddKey(1f, 0f); // Pops cleanly at surface
            sizeModule.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // 6. Color over Lifetime (Bright luminous ascent, fade out at water surface)
            var colorModule = ps.colorOverLifetime;
            colorModule.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.90f, 0.98f, 1.0f), 0.5f),
                    new GradientColorKey(Color.white, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.0f, 0.0f),
                    new GradientAlphaKey(0.98f, 0.06f),
                    new GradientAlphaKey(0.95f, 0.90f),
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            colorModule.color = grad;

            // 7. Noise Module (Organic underwater wobble)
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.45f;
            noise.frequency = 0.5f;
            noise.scrollSpeed = 1.0f;
            noise.damping = true;
            noise.quality = ParticleSystemNoiseQuality.Medium;

            ps.Play();
        }

        private static Material GetOrCreateBubbleMaterial()
        {
            string matPath = "Assets/Materials/mBubble.mat";
            string texPath = "Assets/Materials/tBubble.png";

#if UNITY_EDITOR
            // Generate high-contrast crisp bubble texture
            Texture2D bubbleTex = CreateBubbleTexture();
            if (bubbleTex != null)
            {
                byte[] pngData = bubbleTex.EncodeToPNG();
                System.IO.File.WriteAllBytes(texPath, pngData);
                UnityEditor.AssetDatabase.ImportAsset(texPath);

                // Ensure alpha transparency is enabled in importer
                UnityEditor.TextureImporter importer = UnityEditor.AssetImporter.GetAtPath(texPath) as UnityEditor.TextureImporter;
                if (importer != null)
                {
                    importer.alphaIsTransparency = true;
                    importer.alphaSource = UnityEditor.TextureImporterAlphaSource.FromInput;
                    importer.SaveAndReimport();
                }
            }

            Material mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                    ?? Shader.Find("Universal Render Pipeline/Particles/Lit")
                    ?? Shader.Find("Particles/Standard Unlit");

            if (mat == null && s != null)
            {
                mat = new Material(s);
                mat.name = "mBubble";
                UnityEditor.AssetDatabase.CreateAsset(mat, matPath);
            }

            if (mat != null)
            {
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // Transparent
                if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);     // Alpha blend
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 1f));
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(1f, 1f, 1f, 1f));

                Texture2D loadedTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                if (loadedTex != null)
                {
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", loadedTex);
                    if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", loadedTex);
                }

                UnityEditor.EditorUtility.SetDirty(mat);
                UnityEditor.AssetDatabase.SaveAssets();
                return mat;
            }
#endif
            return null;
        }

#if UNITY_EDITOR
        private static Texture2D CreateBubbleTexture()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size * 0.5f;
            float radius = size * 0.46f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist > radius)
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        float normDist = dist / radius;

                        // Distinct crisp bright outer rim
                        float ringDist = Mathf.Abs(normDist - 0.88f);
                        float ring = Mathf.Clamp01(1f - (ringDist / 0.12f)) * 1.0f;

                        // Translucent glass interior
                        float interior = Mathf.Pow(1f - normDist, 1.5f) * 0.35f;

                        // Primary specular highlight (top-left)
                        float glintDist1 = Vector2.Distance(new Vector2(x, y), new Vector2(center - radius * 0.38f, center + radius * 0.38f));
                        float glint1 = Mathf.Clamp01(1f - (glintDist1 / (radius * 0.28f)));
                        glint1 = Mathf.Pow(glint1, 2.0f);

                        // Secondary subtle glint (bottom-right)
                        float glintDist2 = Vector2.Distance(new Vector2(x, y), new Vector2(center + radius * 0.38f, center - radius * 0.38f));
                        float glint2 = Mathf.Clamp01(1f - (glintDist2 / (radius * 0.20f))) * 0.65f;

                        float alpha = Mathf.Clamp01(ring + interior + glint1 + glint2);
                        Color col = new Color(1f, 1f, 1f, alpha);
                        tex.SetPixel(x, y, col);
                    }
                }
            }
            tex.Apply();
            return tex;
        }
#endif
    }
}
