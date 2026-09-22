using System.Diagnostics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RiseOn.Utils {
    public static class ObjectExtensions {
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

        private static string GetPathTF(Transform tf) {
            if (tf == null) return string.Empty;
            var result = tf.name;
            while ((tf = tf.parent) != null)
                result = $"{tf.name}/{result}";
            return result;
        }
    }
}
