using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MultiCamCapture.Demo
{
    /// <summary>
    /// Standalone Third-Person Player Controller for swimming fish.
    /// Can be used as a standalone PlayerFish GameObject/Prefab with its own visual mesh and camera,
    /// or used to dynamically possess any spawned fish in the aquarium.
    /// Fully uses the Unity Input System package and updates all camera settings in real-time.
    /// </summary>
    [SelectionBase]
    [ExecuteAlways]
    public class FishPlayerController : MonoBehaviour
    {
        [Header("Target & Mode")]
        [Tooltip("If true, this controller drives the GameObject it is attached to (Standalone Player Fish mode).")]
        [SerializeField] private bool controlSelf = true;

        [Tooltip("Optional name of a specific spawned species to possess on start if not controlling self.")]
        [SerializeField] private string targetSpeciesName = "";

        [Header("Fish Mesh & Appearance")]
        [Tooltip("Assign any fish prefab (from Assets/Prefabs/Fish/...) to change the player's fish mesh and species.")]
        [SerializeField] private GameObject fishPrefab;

        [Tooltip("Visual scale multiplier for the fish model")]
        [Range(0.05f, 3.0f)]
        [SerializeField] private float meshScale = 0.35f;

        [Header("Movement & Swimming Physics")]
        [SerializeField] private float swimSpeed = 4.5f;
        [SerializeField] private float sprintMultiplier = 1.8f;
        [SerializeField] private float turnSpeed = 90.0f;     // Degrees per sec
        [SerializeField] private float pitchSpeed = 60.0f;    // Degrees per sec
        [SerializeField] private float rollBankSpeed = 40.0f; // Max roll banking on turns
        [SerializeField] private float acceleration = 3.5f;

        [Header("Camera Rig Settings (Real-Time)")]
        [Tooltip("The camera used for 3rd-person follow. If unassigned, finds child camera or Camera.main.")]
        [SerializeField] private Camera playerCamera;
        [Tooltip("Distance behind the fish")]
        [Range(0.5f, 15f)]
        [SerializeField] private float cameraDistance = 2.2f;
        [Tooltip("Height above the fish")]
        [Range(0.1f, 8f)]
        [SerializeField] private float cameraHeight = 0.65f;
        [Tooltip("Field of View")]
        [Range(30f, 110f)]
        [SerializeField] private float fieldOfView = 60f;
        [Tooltip("Look ahead distance along fish heading")]
        [SerializeField] private float cameraLookAhead = 1.5f;
        [Tooltip("Follow smoothing speed (higher = snappier)")]
        [SerializeField] private float cameraSmoothSpeed = 10.0f;

        [Header("Visual / Animation Settings")]
        [Tooltip("Child transform holding the visual mesh for banking roll tilt (optional)")]
        [SerializeField] private Transform visualRoot;
        [Tooltip("Animator to drive swimming speed")]
        [SerializeField] private Animator fishAnimator;
        [SerializeField] private string speedParamName = "Speed";

        [Header("UI Overlay")]
        [SerializeField] private bool showHUD = true;

        // Runtime state
        private GameObject currentInstantiatedPrefab;
        private Transform targetTransform;
        private FishAgent currentFishAgent;
        private float currentSpeed;
        private float targetPitch;
        private float targetYaw;
        private float currentBank;
        private int speedParamHash;
        private bool hasSpeedParam;
        private int currentFishIndex = -1;
#if UNITY_EDITOR
        private bool isMeshUpdatePending = false;
#endif

        // Camera backup for overview mode
        private Vector3 overviewCamPos;
        private Quaternion overviewCamRot;
        private bool isOverviewMode = false;

        public GameObject FishPrefab 
        { 
            get => fishPrefab; 
            set 
            { 
                fishPrefab = value; 
                UpdateFishMesh(true); 
            } 
        }
        public float MeshScale 
        { 
            get => meshScale; 
            set 
            { 
                meshScale = value; 
                ApplyMeshScale(); 
            } 
        }
        public Camera PlayerCamera { get => playerCamera; set => playerCamera = value; }
        public float CameraDistance { get => cameraDistance; set => cameraDistance = value; }
        public float CameraHeight { get => cameraHeight; set => cameraHeight = value; }
        public float FieldOfView { get => fieldOfView; set => fieldOfView = value; }
        public Transform TargetTransform => targetTransform;
        public FishAgent ControlledFish => currentFishAgent;
        public bool IsOverviewMode => isOverviewMode;
        public float CurrentSpeed => currentSpeed;

        private void Awake()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject))
            {
                return;
            }
#endif
            if (controlSelf && fishPrefab != null)
            {
                UpdateFishMesh(false);
            }
            else
            {
                InitializeVisuals();
            }
        }

        private void OnValidate()
        {
            if (targetTransform == null) targetTransform = transform;
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();

            if (controlSelf)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    // Never mutate persistent prefab assets on disk during OnValidate
                    if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject))
                    {
                        return;
                    }

                    if (!isMeshUpdatePending)
                    {
                        isMeshUpdatePending = true;
                        UnityEditor.EditorApplication.delayCall += () =>
                        {
                            isMeshUpdatePending = false;
                            if (this == null || gameObject == null) return;
                            UpdateFishMesh(false);
                            ApplyMeshScale();
                        };
                    }
                }
                else
                {
                    UpdateFishMesh(false);
                    ApplyMeshScale();
                }
#else
                UpdateFishMesh(false);
                ApplyMeshScale();
#endif
            }

            UpdateThirdPersonCamera();
        }

        public Transform GetOrCreateVisualSlot()
        {
            if (visualRoot != null && visualRoot != transform)
            {
                return visualRoot;
            }

            Transform slot = transform.Find("VisualSlot");
            if (slot == null)
            {
                slot = transform.Find("FishVisual");
            }

            if (slot == null)
            {
                GameObject slotObj = new GameObject("VisualSlot");
                slotObj.transform.SetParent(transform, false);
                slot = slotObj.transform;
            }

            visualRoot = slot;
            return slot;
        }

        public void ClearVisuals()
        {
            Transform slot = GetOrCreateVisualSlot();

            // Destroy all children inside visual slot
            for (int i = slot.childCount - 1; i >= 0; i--)
            {
                var child = slot.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
#if UNITY_EDITOR
                    UnityEditor.Undo.DestroyObjectImmediate(child);
#else
                    DestroyImmediate(child);
#endif
                }
            }

            // Also clean up any stray legacy visual children under root (excluding camera)
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child != slot && child.GetComponent<Camera>() == null && 
                    (child.name.StartsWith("FishVisual") || child.GetComponentInChildren<SkinnedMeshRenderer>() != null || child.GetComponentInChildren<MeshRenderer>() != null))
                {
                    if (Application.isPlaying)
                    {
                        Destroy(child.gameObject);
                    }
                    else
                    {
#if UNITY_EDITOR
                        UnityEditor.Undo.DestroyObjectImmediate(child.gameObject);
#else
                        DestroyImmediate(child.gameObject);
#endif
                    }
                }
            }
        }

        [ContextMenu("Refresh Visual Mesh")]
        public void ForceRefreshMesh()
        {
            UpdateFishMesh(true);
        }

        public void UpdateFishMesh(bool force)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject))
            {
                return;
            }
#endif
            Transform slot = GetOrCreateVisualSlot();

            if (fishPrefab == null)
            {
                ClearVisuals();
                currentInstantiatedPrefab = null;
                fishAnimator = null;
                return;
            }

            if (!force && slot.childCount > 0 && fishPrefab == currentInstantiatedPrefab)
            {
                InitializeVisuals();
                ApplyMeshScale();
                return;
            }

            currentInstantiatedPrefab = fishPrefab;
            ClearVisuals();

            // Instantiate new fish model inside slot
            GameObject newVisual = null;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                newVisual = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(fishPrefab, slot);
                if (newVisual != null)
                {
                    UnityEditor.Undo.RegisterCreatedObjectUndo(newVisual, "Instantiate Player Fish Visual");
                }
            }
#endif
            if (newVisual == null)
            {
                newVisual = Instantiate(fishPrefab, slot);
            }

            newVisual.name = fishPrefab.name;
            newVisual.transform.localPosition = Vector3.zero;
            newVisual.transform.localRotation = Quaternion.identity;
            newVisual.transform.localScale = Vector3.one;

            // Strip any nested FishAgent or Camera on visual instance
            var childAgent = newVisual.GetComponent<FishAgent>();
            if (childAgent != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(childAgent);
                }
                else
                {
#if UNITY_EDITOR
                    UnityEditor.Undo.DestroyObjectImmediate(childAgent);
#else
                    DestroyImmediate(childAgent);
#endif
                }
            }

            ApplyMeshScale();
            InitializeVisuals();
        }

        private void ApplyMeshScale()
        {
            Transform slot = GetOrCreateVisualSlot();
            if (slot != null)
            {
                slot.localScale = Vector3.one * meshScale;
            }
        }

        private void InitializeVisuals()
        {
            Transform slot = GetOrCreateVisualSlot();
            visualRoot = slot;

            if (fishAnimator == null && slot != null)
            {
                fishAnimator = slot.GetComponentInChildren<Animator>();
            }

            if (fishAnimator == null)
            {
                fishAnimator = GetComponentInChildren<Animator>();
            }

            if (fishAnimator != null && !string.IsNullOrEmpty(speedParamName))
            {
                speedParamHash = Animator.StringToHash(speedParamName);
                hasSpeedParam = false;
                foreach (var param in fishAnimator.parameters)
                {
                    if (param.nameHash == speedParamHash)
                    {
                        hasSpeedParam = true;
                        break;
                    }
                }
            }

            OptimizeMeshMaterials(slot != null ? slot.gameObject : gameObject);
        }

        private static readonly int SmoothnessProp = Shader.PropertyToID("_Smoothness");
        private static readonly int MetallicProp = Shader.PropertyToID("_Metallic");
        private static MaterialPropertyBlock sharedPropertyBlock;

        private static void OptimizeMeshMaterials(GameObject target)
        {
            if (target == null) return;
            if (sharedPropertyBlock == null)
            {
                sharedPropertyBlock = new MaterialPropertyBlock();
                sharedPropertyBlock.SetFloat(SmoothnessProp, 0.05f);
                sharedPropertyBlock.SetFloat(MetallicProp, 0.0f);
            }

            var renderers = target.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                var rend = renderers[r];
                if (rend == null) continue;

                rend.SetPropertyBlock(sharedPropertyBlock);

                var mats = rend.sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    var mat = mats[m];
                    if (mat != null && mat.HasProperty("_EmissionColor"))
                    {
                        Color emission = mat.GetColor("_EmissionColor");
                        if (emission.r <= 0.001f && emission.g <= 0.001f && emission.b <= 0.001f)
                        {
                            mat.DisableKeyword("_EMISSION");
                        }
                    }
                }
            }
        }

        private void Start()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
                if (playerCamera == null)
                {
                    playerCamera = Camera.main;
                }
            }

            if (playerCamera != null)
            {
                playerCamera.depth = 100;
                playerCamera.fieldOfView = fieldOfView;
                overviewCamPos = playerCamera.transform.position;
                overviewCamRot = playerCamera.transform.rotation;

                if (Application.isPlaying)
                {
                    // Unparent child camera so it tracks smoothly in world space
                    if (playerCamera.transform.parent != null)
                    {
                        playerCamera.transform.SetParent(null);
                    }
                }
            }

            if (controlSelf)
            {
                targetTransform = transform;
                targetYaw = transform.eulerAngles.y;
                targetPitch = transform.eulerAngles.x;

                currentFishAgent = GetComponent<FishAgent>();
                if (currentFishAgent != null)
                {
                    currentFishAgent.IsPlayerControlled = true;
                }
            }
            else if (Application.isPlaying)
            {
                Invoke(nameof(PossessInitialFish), 0.35f);
            }
        }

        private void PossessInitialFish()
        {
            var activeFish = FishAgent.ActiveFish;
            if (activeFish == null || activeFish.Count == 0)
            {
                Invoke(nameof(PossessInitialFish), 0.5f);
                return;
            }

            if (!string.IsNullOrEmpty(targetSpeciesName))
            {
                for (int i = 0; i < activeFish.Count; i++)
                {
                    if (activeFish[i].gameObject.name.Contains(targetSpeciesName, System.StringComparison.OrdinalIgnoreCase))
                    {
                        currentFishIndex = i;
                        PossessFish(activeFish[i]);
                        return;
                    }
                }
            }

            currentFishIndex = 0;
            PossessFish(activeFish[0]);
        }

        public void PossessFish(FishAgent fish)
        {
            if (fish == null) return;

            if (currentFishAgent != null)
            {
                currentFishAgent.IsPlayerControlled = false;
            }

            currentFishAgent = fish;
            currentFishAgent.IsPlayerControlled = true;
            targetTransform = currentFishAgent.transform;
            fishAnimator = currentFishAgent.GetComponentInChildren<Animator>();
            visualRoot = currentFishAgent.transform;

            targetYaw = targetTransform.eulerAngles.y;
            targetPitch = targetTransform.eulerAngles.x;
            currentSpeed = 0f;
            isOverviewMode = false;

            Debug.Log($"[FishPlayerController] Possessed fish: {fish.gameObject.name}");
        }

        public void CycleNextFish()
        {
            var activeFish = FishAgent.ActiveFish;
            if (activeFish == null || activeFish.Count == 0) return;

            currentFishIndex = (currentFishIndex + 1) % activeFish.Count;
            PossessFish(activeFish[currentFishIndex]);
        }

        public void CyclePrevFish()
        {
            var activeFish = FishAgent.ActiveFish;
            if (activeFish == null || activeFish.Count == 0) return;

            currentFishIndex--;
            if (currentFishIndex < 0) currentFishIndex = activeFish.Count - 1;
            PossessFish(activeFish[currentFishIndex]);
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                if (targetTransform == null) targetTransform = transform;
                return;
            }

            ReadInputHotkeys();

            if (targetTransform == null || isOverviewMode) return;

            HandleSwimmingMovement();
        }

        private void LateUpdate()
        {
            if (targetTransform == null || isOverviewMode || playerCamera == null) return;

            UpdateThirdPersonCamera();
        }

        private void ReadInputHotkeys()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.tabKey.wasPressedThisFrame)
                {
                    CycleNextFish();
                }

                if (kb.cKey.wasPressedThisFrame)
                {
                    ToggleOverviewCamera();
                }
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    cameraDistance = Mathf.Clamp(cameraDistance - (scroll * 0.003f), 0.5f, 15f);
                }
            }
#endif
        }

        private void ToggleOverviewCamera()
        {
            isOverviewMode = !isOverviewMode;
            if (isOverviewMode && playerCamera != null)
            {
                playerCamera.transform.position = overviewCamPos;
                playerCamera.transform.rotation = overviewCamRot;
            }
        }

        private void HandleSwimmingMovement()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            float forwardInput = 0f;
            float turnInput = 0f;
            float verticalInput = 0f;
            bool isSprinting = false;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                // Forward / Backward
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) forwardInput += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) forwardInput -= 1f;

                // Turn Left / Right
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) turnInput += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) turnInput -= 1f;

                // Vertical Ascend / Descend
                if (kb.spaceKey.isPressed || kb.eKey.isPressed) verticalInput += 1f;
                if (kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed || kb.qKey.isPressed) verticalInput -= 1f;

                // Sprint Boost
                if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed) isSprinting = true;
            }

            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.rightButton.isPressed)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    turnInput += delta.x * 0.1f;
                    verticalInput += delta.y * 0.1f;
                }
            }

            var gp = Gamepad.current;
            if (gp != null)
            {
                Vector2 stick = gp.leftStick.ReadValue();
                turnInput += stick.x;
                forwardInput += stick.y;

                if (gp.rightTrigger.isPressed || gp.buttonSouth.isPressed) verticalInput += 1f;
                if (gp.leftTrigger.isPressed || gp.buttonEast.isPressed) verticalInput -= 1f;
                if (gp.leftShoulder.isPressed || gp.rightShoulder.isPressed) isSprinting = true;
            }
#endif

            // Speed calculation
            float maxMoveSpeed = isSprinting ? swimSpeed * sprintMultiplier : swimSpeed;
            float desiredSpeed = Mathf.Abs(forwardInput) > 0.05f 
                ? Mathf.Sign(forwardInput) * maxMoveSpeed 
                : (Mathf.Abs(verticalInput) > 0.05f ? maxMoveSpeed * 0.5f : 0f);

            currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, acceleration * dt);

            // Turn (Yaw) & Pitch
            targetYaw += turnInput * turnSpeed * dt;
            targetPitch -= verticalInput * pitchSpeed * dt;
            targetPitch = Mathf.Clamp(targetPitch, -60f, 60f);

            // Natural roll banking on turns
            float targetBank = -turnInput * rollBankSpeed;
            currentBank = Mathf.Lerp(currentBank, targetBank, dt * 5.0f);

            // Apply rotation to target fish
            Quaternion targetRot = Quaternion.Euler(targetPitch, targetYaw, currentBank);
            targetTransform.rotation = Quaternion.Slerp(targetTransform.rotation, targetRot, dt * 10f);

            // Apply forward velocity
            Vector3 movement = targetTransform.forward * currentSpeed * dt;
            targetTransform.position += movement;

            // Clamp inside bounds if AquariumBounds exists
            var bounds = Object.FindAnyObjectByType<AquariumBounds>();
            if (bounds != null)
            {
                Bounds b = bounds.BoundingBox;
                Vector3 pos = targetTransform.position;
                pos.x = Mathf.Clamp(pos.x, b.min.x + 0.5f, b.max.x - 0.5f);
                pos.y = Mathf.Clamp(pos.y, b.min.y + 0.3f, b.max.y - 0.3f);
                pos.z = Mathf.Clamp(pos.z, b.min.z + 0.5f, b.max.z - 0.5f);
                targetTransform.position = pos;
            }

            // Update Animator
            if (fishAnimator != null)
            {
                float animSpeedNorm = Mathf.Abs(currentSpeed) / (swimSpeed * sprintMultiplier);
                float animVal = animSpeedNorm > 0.6f ? 2.0f : (animSpeedNorm > 0.05f ? 1.0f : 0.0f);
                
                if (hasSpeedParam)
                {
                    fishAnimator.SetFloat(speedParamHash, animVal);
                }
                else
                {
                    fishAnimator.speed = Mathf.Max(0.5f, animSpeedNorm * 1.5f);
                }
            }
        }

        private void UpdateThirdPersonCamera()
        {
            if (playerCamera == null || targetTransform == null) return;

            playerCamera.fieldOfView = fieldOfView;

            // Compute desired 3rd-person camera position based on real-time inspector settings
            Vector3 targetCamPos = targetTransform.position 
                - (targetTransform.forward * cameraDistance) 
                + (Vector3.up * cameraHeight);

            Vector3 lookTarget = targetTransform.position 
                + (targetTransform.forward * cameraLookAhead) 
                + (Vector3.up * (cameraHeight * 0.25f));

            if (Application.isPlaying && cameraSmoothSpeed > 0f)
            {
                // Smooth position follow
                playerCamera.transform.position = Vector3.Lerp(
                    playerCamera.transform.position,
                    targetCamPos,
                    Time.deltaTime * cameraSmoothSpeed
                );

                // Smooth look orientation
                Quaternion targetLookRot = Quaternion.LookRotation(lookTarget - playerCamera.transform.position, Vector3.up);
                playerCamera.transform.rotation = Quaternion.Slerp(
                    playerCamera.transform.rotation,
                    targetLookRot,
                    Time.deltaTime * cameraSmoothSpeed * 1.5f
                );
            }
            else
            {
                // Instant update in editor or zero smoothing
                playerCamera.transform.position = targetCamPos;
                playerCamera.transform.rotation = Quaternion.LookRotation(lookTarget - targetCamPos, Vector3.up);
            }
        }

        private void OnGUI()
        {
            if (!showHUD || targetTransform == null || !Application.isPlaying) return;

            GUI.color = Color.white;
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.fontSize = 13;
            boxStyle.alignment = TextAnchor.UpperLeft;
            boxStyle.normal.textColor = Color.white;

            string fishName = targetTransform.gameObject.name.Replace("(Clone)", "").Trim();
            string modeText = isOverviewMode ? "[Overview Camera Mode]" : "[3rd-Person Fish Camera]";

            string hud = $"<b>Virtual Aquarium Pilot</b>\n" +
                         $"Mode: <color=#64D2FF>{modeText}</color>\n" +
                         $"Active Fish: <color=#FFD60A>{fishName}</color>\n" +
                         $"Speed: {currentSpeed:F1} m/s | Depth: {targetTransform.position.y:F1}m\n" +
                         $"Cam Dist: {cameraDistance:F1}m | Height: {cameraHeight:F1}m\n\n" +
                         $"<b>Controls (Input System):</b>\n" +
                         $"• <b>W / S / A / D</b>: Swim & Turn\n" +
                         $"• <b>Space / Ctrl (or Q/E)</b>: Ascend / Descend\n" +
                         $"• <b>Left Shift</b>: Sprint Boost\n" +
                         $"• <b>Mouse Scroll</b>: Zoom Cam Distance\n" +
                         $"• <b>Tab</b>: Cycle Next Fish\n" +
                         $"• <b>C</b>: Toggle 3rd-Person / Overview Cam\n" +
                         $"• <b>Right Mouse Drag</b>: Steering";

            GUI.Box(new Rect(15, 15, 290, 210), hud, boxStyle);
        }
    }
}
