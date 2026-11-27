using System.Collections.Generic;
using UnityEditor;
using Object = UnityEngine.Object;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Writes a settings asset to disk once no text field is being edited and no drop-down is open: a save writes and reimports the file, some 60 ms, which every
    /// keystroke would stall on, and every toggle or move in a drop-down that stays open.<br/>
    /// The edit itself is in the asset at once, for everything that reads it; any other edit, a toggle on the page or a pick, saves at once.
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

        /// <summary>Saves <paramref name="asset"/> if dirty: now, or as the text field being edited ends or the drop-down closes. Call it every draw, so an undo reaches the disk too.</summary>
        public static void Request(Object asset) {
            if (!asset || !EditorUtility.IsDirty(asset)) return;

            if (!Busy) {
                AssetDatabase.SaveAssetIfDirty(asset);
                return;
            }

            if (pending.Add(asset) && pending.Count == 1) EditorApplication.update += Flush;
        }

        private static bool Busy => EditorGUIUtility.editingTextField || SearchWindow.SearchWindow.IsDropDownOpen;

        private static void Flush() {
            if (!Busy) Save();
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