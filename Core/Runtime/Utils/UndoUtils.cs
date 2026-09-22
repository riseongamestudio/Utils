using System.Diagnostics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RiseOn.Utils {
    /// <remarks>
    /// Record/MarkDirty stay static on purpose: TMPro ships a public <c>MarkDirty(this Object)</c> extension,
    /// so an extension of the same shape would be ambiguous in every file that imports TMPro.
    /// </remarks>
    public static class UndoUtils {
        [Conditional("UNITY_EDITOR")]
        public static void RecordForUndo(Object target) {
            #if UNITY_EDITOR

            if (Application.isPlaying) return;

            UnityEditor.Undo.RecordObject(target, $"Modify {target.name} {target.GetType().Name}");

            #endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void RecordForUndo(params Object[] targets) {
            foreach (var target in targets) RecordForUndo(target);
        }

        [Conditional("UNITY_EDITOR")]
        public static void MarkDirty(Object target) {
            #if UNITY_EDITOR

            if (Application.isPlaying) return;

            UnityEditor.EditorUtility.SetDirty(target);

            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(target);

            if (target is not GameObject go) {
                if (target is not Component cpn) return;
                go = cpn.gameObject;
            }

            try {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
            } catch { // Target is in current playing scene
            }

            #endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void MarkDirty(params Object[] targets) {
            foreach (var target in targets) MarkDirty(target);
        }

        public static GameObject CreateGameObjectUndo(string name) {
            var go = new GameObject(name);

            #if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(go, $"Create game object {name}");
            #endif

            return go;
        }
    }
}
