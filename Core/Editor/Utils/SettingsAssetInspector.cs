using UnityEditor;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// The Inspector of a settings asset that edits on its Project Settings page only, as Input System's settings asset does, so one place holds its edits: a button that opens the page.
    /// </summary>
    /// <example><code>
    /// [CustomEditor(typeof(MySettings))]
    /// internal sealed class MySettingsInspector : SettingsAssetInspector {
    ///     protected override string PagePath => "Project/RiseOn/My Pack";
    /// }
    /// </code></example>
    public abstract class SettingsAssetInspector : UnityEditor.Editor {
        /// <summary>The page's SettingsProvider path, such as "Project/RiseOn/Ads".</summary>
        protected abstract string PagePath { get; }

        public override void OnInspectorGUI() {
            var title = "Project Settings > " + PagePath.Substring(PagePath.IndexOf('/') + 1).Replace("/", " > ");
            if (GUILayout.Button("Open " + title, GUILayout.Height(30))) SettingsService.OpenProjectSettings(PagePath);
        }
    }
}