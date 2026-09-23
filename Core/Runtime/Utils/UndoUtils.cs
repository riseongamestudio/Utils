using System;
using System.Diagnostics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RiseOn.Utils {
    /// <remarks>
    /// uGUI ships a public <c>MarkDirty(this Object)</c> extension in namespace TMPro, so <c>obj.MarkDirty()</c> is ambiguous in a file that has <c>using TMPro</c>.<br/>
    /// Call <c>UndoUtils.MarkDirty(obj)</c> there instead.
    /// </remarks>
    public static class UndoUtils {
        [Conditional("UNITY_EDITOR")]
        public static void RecordForUndo(this Object target) {
            #if UNITY_EDITOR

            if (Application.isPlaying) return;

            UnityEditor.Undo.RecordObject(target, $"Modify {target.name} {target.GetType().Name}");

            #endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void RecordForUndo(params Object[] targets) {
            foreach (var target in targets) target.RecordForUndo();
        }

        [Conditional("UNITY_EDITOR")]
        public static void MarkDirty(this Object target) {
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
            foreach (var target in targets) target.MarkDirty();
        }

        /// <summary>
        /// Records <paramref name="target"/> for Undo, runs <paramref name="edit"/> on it, then marks it dirty: any edit, one call.<br/>
        /// In a build only <paramref name="edit"/> runs, since recording and marking dirty are editor-only.
        /// </summary>
        public static void EditUndo<T>(this T target, Action<T> edit) where T : Object {
            target.RecordForUndo();
            edit(target);
            target.MarkDirty();
        }

        public static GameObject CreateGameObjectUndo(string name) {
            var go = new GameObject(name);

            #if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(go, $"Create game object {name}");
            #endif

            return go;
        }

        public static void SetParentUndo(this Transform child, Transform par) {
            #if UNITY_EDITOR
            UnityEditor.Undo.SetTransformParent(child, par, $"{child.name} set parent to {par.name}");
            return;
            #endif

            #pragma warning disable CS0162 // Unreachable code detected
            child.SetParent(par);
            #pragma warning restore CS0162 // Unreachable code detected
        }

        public static TComponent AddComponentUndo<TComponent>(this GameObject go) where TComponent : Component {
            #if UNITY_EDITOR
            return UnityEditor.Undo.AddComponent<TComponent>(go);
            #endif

            #pragma warning disable CS0162 // Unreachable code detected
            return go.AddComponent<TComponent>();
            #pragma warning restore CS0162 // Unreachable code detected
        }
    }
}
