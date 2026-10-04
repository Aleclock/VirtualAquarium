using System.IO;
using UnityEditor;
using UnityEngine;
using MultiCamCapture.Demo;

namespace MultiCamCapture.Editor
{
    /// <summary>
    /// Editor utility to generate and export a standalone Player Fish GameObject and Prefab.
    /// </summary>
    public static class PlayerFishBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/PlayerFish.prefab";
        private const string DefaultSpeciesPrefab = "Assets/Prefabs/Fish/Clownfish.prefab";

        [MenuItem("Aquarium/Create Player Fish in Scene", false, 110)]
        public static GameObject CreatePlayerFishInScene()
        {
            // Determine spawn position inside bounds if available
            Vector3 spawnPos = new Vector3(0f, 4f, 0f);
            var bounds = Object.FindAnyObjectByType<AquariumBounds>();
            if (bounds != null)
            {
                spawnPos = bounds.BoundingBox.center;
            }

            GameObject playerRoot = CreatePlayerFishHierarchy();
            playerRoot.transform.position = spawnPos;

            Selection.activeGameObject = playerRoot;
            EditorGUIUtility.PingObject(playerRoot);

            Debug.Log("[PlayerFishBuilder] Created standalone PlayerFish GameObject in scene.");
            return playerRoot;
        }

        [MenuItem("Aquarium/Export Player Fish Prefab", false, 111)]
        public static void ExportPlayerFishPrefab()
        {
            string dir = Path.GetDirectoryName(PrefabPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            GameObject go = CreatePlayerFishHierarchy();
            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[PlayerFishBuilder] Successfully exported standalone Player Fish Prefab to '{PrefabPath}'!");
        }

        private static GameObject CreatePlayerFishHierarchy()
        {
            GameObject playerRoot = new GameObject("PlayerFish");
            playerRoot.transform.position = Vector3.zero;
            playerRoot.transform.rotation = Quaternion.identity;

            // Dedicated visual slot container
            GameObject visualSlotObj = new GameObject("VisualSlot");
            visualSlotObj.transform.SetParent(playerRoot.transform, false);
            visualSlotObj.transform.localPosition = Vector3.zero;
            visualSlotObj.transform.localRotation = Quaternion.identity;
            visualSlotObj.transform.localScale = Vector3.one * 0.35f;

            // Create dedicated 3rd-Person Camera
            GameObject camObj = new GameObject("PlayerThirdPersonCamera");
            camObj.tag = "MainCamera";
            camObj.transform.SetParent(playerRoot.transform, false);
            camObj.transform.localPosition = new Vector3(0f, 0.65f, -2.2f);
            camObj.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);

            var cam = camObj.AddComponent<Camera>();
            cam.depth = 10;
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 300f;
            camObj.AddComponent<AudioListener>();

            // Add FishPlayerController
            var controller = playerRoot.AddComponent<FishPlayerController>();
            controller.PlayerCamera = cam;
            GameObject defaultSpecies = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultSpeciesPrefab);
            controller.FishPrefab = defaultSpecies;
            controller.MeshScale = 0.35f;

            return playerRoot;
        }
    }
}
