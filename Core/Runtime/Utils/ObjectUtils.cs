using System.Diagnostics;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace RiseOn.Utils {
    public static class ObjectUtils {
        /// <returns>True if <see cref="go"/> is actually in playing mode.<br/>(some case like: open prefab stage when in playing mode)</returns>
        public static bool IsPlaying(this GameObject go) {
            #if UNITY_EDITOR
            return
                !UnityEditor.EditorUtility.IsPersistent(go)
             && null == UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(go)
             && Application.isPlaying;
            #endif

            #pragma warning disable CS0162 // Unreachable code detected
            return true;
            #pragma warning restore CS0162 // Unreachable code detected
        }

        /// <inheritdoc cref="IsPlaying(GameObject)"/>
        public static bool IsPlaying(this Component cpn) {
            return cpn.gameObject.IsPlaying();
        }

        /// <inheritdoc cref="UnityEditor.EditorGUIUtility.PingObject(Object)"/>
        [Conditional("UNITY_EDITOR")]
        public static void PingObject(this Object obj) {
            #if UNITY_EDITOR
            UnityEditor.EditorGUIUtility.PingObject(obj);
            #endif
        }

        public static string GetPath(this Object obj, bool withSceneName = true) {
            if (obj == null) return null;

            if (obj is Component cpn) return $"{cpn.gameObject.GetPath(withSceneName)}<{cpn.GetType().Name}>";

            #if UNITY_EDITOR
            if (UnityEditor.EditorUtility.IsPersistent(obj)) return UnityEditor.AssetDatabase.GetAssetPath(obj);
            #endif

            if (obj is GameObject go) return $"{(withSceneName ? GetScenePrefixPath(go) : string.Empty)}{GetPathTF(go.transform)}";

            return $"[Unknown]/{obj.name}";

            static string GetScenePrefixPath(GameObject go) {
                #if UNITY_EDITOR
                if (UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(go) != null) return string.Empty;
                #endif
                return (go.scene.IsValid() ? go.scene.name : "[Unknown]") + "/";
            }
        }

        /// <summary>
        /// Names from the root down to <paramref name="tf"/>, joined once at the end.<br/>
        /// Prepending one name at a time would build a new string for every level.
        /// </summary>
        private static string GetPathTF(Transform tf) {
            if (tf == null) return string.Empty;

            var names = ListPool<string>.Get();
            try {
                for (var current = tf; current != null; current = current.parent) names.Add(current.name);

                names.Reverse();
                return string.Join("/", names);
            } finally {
                ListPool<string>.Release(names);
            }
        }
    }
}
