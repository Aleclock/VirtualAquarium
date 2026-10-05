using System.IO;
using UnityEditor;
using UnityEngine;

namespace MultiCamCapture.Editor
{
    /// <summary>
    /// Resolves asset paths flexibly whether the package is loaded as a local package,
    /// a Git UPM package (under Packages/com.aleclock.virtualaquarium/...), or directly in Assets/.
    /// </summary>
    public static class PackageAssetUtility
    {
        public const string PackageId = "com.aleclock.virtualaquarium";

        public static string ResolvePath(string relativePath)
        {
            string rel = relativePath.Replace('\\', '/').TrimStart('/');

            // 1. Check direct package root
            string p1 = $"Packages/{PackageId}/{rel}";
            if (File.Exists(p1) || Directory.Exists(p1)) return p1;

            // 2. Check package Runtime subfolder
            string p2 = $"Packages/{PackageId}/Runtime/{rel}";
            if (File.Exists(p2) || Directory.Exists(p2)) return p2;

            // 3. Check Assets/
            string p3 = $"Assets/{rel}";
            if (File.Exists(p3) || Directory.Exists(p3)) return p3;

            // 4. Check if already absolute or formatted
            if (File.Exists(relativePath) || Directory.Exists(relativePath)) return relativePath;

            return p3;
        }

        public static T LoadAsset<T>(string relativePath) where T : Object
        {
            string resolved = ResolvePath(relativePath);
            T asset = AssetDatabase.LoadAssetAtPath<T>(resolved);
            if (asset != null) return asset;

            // Fallback by filename search
            string filename = Path.GetFileNameWithoutExtension(relativePath);
            string[] guids = AssetDatabase.FindAssets($"{filename} t:{typeof(T).Name}");
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<T>(path);
            }

            return null;
        }
    }
}
