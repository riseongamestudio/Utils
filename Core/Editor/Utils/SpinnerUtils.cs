using UnityEditor;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Unity's own wait spinner, the WaitSpin icons, beside a text, for work still running.<br/>
    /// Each draw asks the window to repaint, so the spinner turns while it shows and stops costing anything once it goes.
    /// </summary>
    /// <example><code>
    /// EditorGUILayout.LabelField(new GUIContent("Account"), checking ? SpinnerUtils.Content("Checking") : new GUIContent(account));
    /// </code></example>
    public static class SpinnerUtils {
        // Unity's twelve WaitSpin frames, at the ten a second its own spinners turn at.
        private const int Frames = 12;
        private const float FramesPerSecond = 10;

        /// <summary>The spinner's current frame and the text, for any label; the icon draws at the size the label's style gives it.</summary>
        public static GUIContent Content(string text, string tooltip = null) {
            HandleUtility.Repaint();
            var frame = (int) (EditorApplication.timeSinceStartup * FramesPerSecond) % Frames;
            return new GUIContent(text, EditorGUIUtility.IconContent($"WaitSpin{frame:00}").image, tooltip);
        }

        /// <summary>A row of a label and a text after a spinner of <see cref="Section.IconSize"/>, laid out as EditorGUILayout.LabelField.</summary>
        public static void LabelField(GUIContent label, string text) {
            EditorGUIUtility.SetIconSize(new Vector2(Section.IconSize, Section.IconSize));
            EditorGUILayout.LabelField(label, Content(text));
            EditorGUIUtility.SetIconSize(Vector2.zero);
        }
    }
}