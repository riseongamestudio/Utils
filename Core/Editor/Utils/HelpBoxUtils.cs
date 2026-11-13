using UnityEditor;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// A help box in the size of the page's labels, rather than the smaller text of Unity's, with its icon one line of that text tall, so a one-line message takes one line rather than the height of Unity's large icon.<br/>
    /// A fix rides inside the box as a button at its right end, centered in the box's height, and the text wraps short of it.
    /// </summary>
    /// <example><code>
    /// HelpBoxUtils.Draw("No config file yet.", MessageType.Warning);
    /// if (HelpBoxUtils.Draw("Fake Store is on.", MessageType.Warning, new GUIContent("Turn off"))) TurnOff();
    /// </code></example>
    public static class HelpBoxUtils {
        /// <summary>The width of the box's button.</summary>
        public const float ButtonWidth = 110;

        private const int IconGap = 2;

        private static GUIStyle plain;
        private static GUIStyle withButton;
        private static GUIStyle group;

        /// <summary>
        /// Draws the box, laid out as EditorGUILayout.HelpBox; true when its button is clicked.<br/>
        /// An icon on the button draws at <see cref="SettingsSection.IconSize"/>.
        /// </summary>
        public static bool Draw(string message, MessageType type, GUIContent button = null, bool enabled = true) {
            var box  = EditorStyles.helpBox;
            var line = EditorStyles.label.lineHeight;
            // The text keeps clear of the icon and the button by padding rather than by an image in its content: GUILayout
            // wraps the text after this call returns, when no icon size is set, so an image there would wrap at its own width.
            plain      ??= new GUIStyle(box) { fontSize = EditorStyles.label.fontSize, padding = new RectOffset(box.padding.left + (int) line + IconGap, box.padding.right, box.padding.top, box.padding.bottom) };
            withButton ??= new GUIStyle(plain) { padding = new RectOffset(plain.padding.left, box.padding.right * 2 + (int) ButtonWidth, box.padding.top, box.padding.bottom), margin = new RectOffset() };
            // The group takes the box's margins, so the box sits where one without a button does.
            group      ??= new GUIStyle { margin = box.margin };

            var content = new GUIContent(message);
            var height  = EditorGUIUtility.singleLineHeight + 2;
            var style   = button == null ? plain : withButton;

            // With a button, a group holds the box at least as tall as the button with the box's own padding above and below;
            // a height on the box itself would replace the height its wrapped text needs.
            if (button != null) GUILayout.BeginVertical(group, GUILayout.MinHeight(height + box.padding.vertical));
            var rect = GUILayoutUtility.GetRect(content, style, GUILayout.ExpandHeight(button != null));
            if (button != null) GUILayout.EndVertical();

            if (Event.current.type is EventType.Repaint) {
                style.Draw(rect, content, false, false, false, false);
                // Unity's small console icon, a line of the text tall, centered in the box as the text is.
                var icon = Icon(type);
                if (icon) GUI.DrawTexture(PixelUtils.Snap(new Rect(rect.x + box.padding.left, rect.center.y - line / 2, line, line)), icon, ScaleMode.ScaleToFit);
            }

            if (button == null) return false;

            var place = new Rect(rect.xMax - box.padding.right - ButtonWidth, rect.center.y - height / 2, ButtonWidth, height);
            using (new EditorGUI.DisabledScope(!enabled)) {
                EditorGUIUtility.SetIconSize(new Vector2(SettingsSection.IconSize, SettingsSection.IconSize));
                var clicked = GUI.Button(PixelUtils.Snap(place), button);
                EditorGUIUtility.SetIconSize(Vector2.zero);
                return clicked;
            }
        }

        private static Texture Icon(MessageType type) {
            return type switch {
                MessageType.Info    => EditorGUIUtility.IconContent("console.infoicon.sml").image
              , MessageType.Warning => EditorGUIUtility.IconContent("console.warnicon.sml").image
              , MessageType.Error   => EditorGUIUtility.IconContent("console.erroricon.sml").image
              , _                   => null
            };
        }
    }
}