using UnityEngine;

namespace RiseOn.Utils {
    public static class UndoExtensions {
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
