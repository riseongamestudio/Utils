using System;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// The margins of a settings page, as Unity's Player page keeps them: the page's title, the labels of plain rows and the edges of <see cref="Section"/> boxes share one line, and the right edge mirrors the left.<br/>
    /// Wrap a SettingsProvider's guiHandler in it.
    /// </summary>
    /// <example><code>
    /// guiHandler = _ => {
    ///     using var page = SettingsPage.Begin();
    ///     Draw();
    /// }
    /// </code></example>
    public readonly struct SettingsPage : IDisposable {
        /// <summary>The space either side of the page's content.</summary>
        public const int Margin = 10;

        private static readonly GUIStyle margins = new() { padding = new RectOffset(Margin, Margin, 0, 0) };

        // The page's width, margins and all, at the last repaint: every event of the next frame sizes the label column by it.
        private static float width;

        // False for a page made with new SettingsPage(), which C# builds without a constructor, so its Dispose leaves the layout alone.
        private readonly bool begun;
        private readonly float labelWidth;

        private SettingsPage(bool begun, float labelWidth) {
            this.begun      = begun;
            this.labelWidth = labelWidth;
        }

        /// <summary>Begins the page's margins and its label column; disposing the page ends them.</summary>
        public static SettingsPage Begin() {
            var page = EditorGUILayout.BeginVertical(margins);
            if (Event.current.type is EventType.Repaint && page.width > 1) width = page.width;

            var previous = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = LabelWidth;
            return new SettingsPage(true, previous);
        }

        /// <summary>
        /// The label column, as the Inspector sizes its own (EditorGUIUtility.labelWidth in hierarchy mode): 45% of the width rounded up, less 40, at least 120.<br/>
        /// The width is the page's, which is what Unity's context width is on a settings page, its scroll bar left out.<br/>
        /// A settings page left to Unity keeps 150 at any width, which cuts long labels in a wide window and leaves short values a narrow one.
        /// </summary>
        public static float LabelWidth => width > 0 ? Mathf.Max(Mathf.Ceil(width * 0.45f) - 40, 120) : 150;

        public void Dispose() {
            if (!begun) return;

            GUILayout.EndVertical();
            EditorGUIUtility.labelWidth = labelWidth;
        }
    }
}