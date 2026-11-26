using System;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using Object = UnityEngine.Object;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// A settings page's Odin tree over its settings asset: made for the asset, made again when another asset takes its place, each edit recorded for undo and written to disk through <see cref="DeferredSave"/>.<br/>
    /// Keep one per page and dispose it in the provider's deactivateHandler.
    /// </summary>
    /// <example><code>
    /// private static readonly SettingsTree tree = new();
    ///
    /// guiHandler = _ => {
    ///     using var page = SettingsPage.Begin();
    ///     tree.Draw(MySettings.LoadOrCreate());
    /// }
    /// , deactivateHandler = tree.Dispose
    /// </code></example>
    public sealed class SettingsTree : IDisposable {
        private PropertyTree tree;
        private Object asset;

        /// <summary>Draws every member the asset shows.</summary>
        public void Draw(Object asset) {
            Begin(asset);
            InspectorUtilities.DrawPropertiesInTree(tree);
            End();
        }

        /// <summary>
        /// Begins a draw that places the members itself, between other GUI, and returns the root: draw a member with root.Children["field"].Draw().<br/>
        /// <see cref="End"/> ends it.
        /// </summary>
        public InspectorProperty Begin(Object asset) {
            if (tree == null || this.asset != asset) {
                tree?.Dispose();
                tree       = PropertyTree.Create(asset);
                this.asset = asset;
            }

            SettingsFocus.Open(tree, asset);
            // Odin records undo only when asked.
            tree.BeginDraw(true);
            return tree.RootProperty;
        }

        public void End() {
            tree.EndDraw();
            SettingsFocus.Scroll(tree, asset);
            if (tree.ApplyChanges()) EditorUtility.SetDirty(asset);
            // Every draw, so an undo reaches the disk as an edit does.
            DeferredSave.Request(asset);
            // A drawer asks Odin for one more repaint when what it drew is a pass behind, such as a field laid out in the last repaint's
            // rect; Odin's own windows answer that, a settings page has to, or the page waits for the next move of the mouse.
            var window = GUIHelper.CurrentWindow;
            if (window) GUIHelper.RepaintIfRequested(window);
        }

        public void Dispose() {
            tree?.Dispose();
            tree  = null;
            asset = null;
        }
    }
}