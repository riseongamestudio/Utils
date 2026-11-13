using System.Collections.Generic;
using UnityEditor;
using Object = UnityEngine.Object;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Writes a settings asset to disk once no text field is being edited: a save writes and reimports the file, some 60 ms, which every keystroke would stall on.<br/>
    /// The edit itself is in the asset at once, for everything that reads it; an edit outside a text field, a toggle or a pick, saves at once.
    /// </summary>
    /// <example><code>
    /// asset.ApplyModifiedProperties();
    /// DeferredSave.Request(asset.targetObject);
    /// </code></example>
    public static class DeferredSave {
        private static readonly HashSet<Object> pending = new();

        static DeferredSave() {
            AssemblyReloadEvents.beforeAssemblyReload += Save;
            EditorApplication.quitting                += Save;
        }

        /// <summary>Saves <paramref name="asset"/> if dirty: now, or as the text field being edited ends. Call it every draw, so an undo reaches the disk too.</summary>
        public static void Request(Object asset) {
            if (!asset || !EditorUtility.IsDirty(asset)) return;

            if (!EditorGUIUtility.editingTextField) {
                AssetDatabase.SaveAssetIfDirty(asset);
                return;
            }

            if (pending.Add(asset) && pending.Count == 1) EditorApplication.update += Flush;
        }

        private static void Flush() {
            if (!EditorGUIUtility.editingTextField) Save();
        }

        private static void Save() {
            EditorApplication.update -= Flush;
            foreach (var asset in pending) {
                if (asset) AssetDatabase.SaveAssetIfDirty(asset);
            }

            pending.Clear();
        }
    }
}