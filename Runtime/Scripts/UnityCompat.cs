using UnityEngine;

namespace MultiCamCapture.Demo
{
    /// <summary>
    /// Cross-version compatibility helpers supporting Unity 2022.3 LTS, Unity 2023, and Unity 6+.
    /// </summary>
    public static class UnityCompat
    {
        public static T FindAny<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER || UNITY_2022_3_OR_NEWER
            return Object.FindAnyObjectByType<T>();
#else
            return Object.FindObjectOfType<T>();
#endif
        }

        public static T[] FindAll<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER || UNITY_2022_3_OR_NEWER
            return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#else
            return Object.FindObjectsOfType<T>();
#endif
        }
    }
}
