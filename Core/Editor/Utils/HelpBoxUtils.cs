using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// A help box in the size of the page's labels, rather than the smaller text of Unity's, with its icon one line of that text tall, so a one-line message takes one line rather than the height of Unity's large icon.<br/>
    /// A fix rides inside the box as a button at its right end, centered in the box's height, and the text wraps short of it; several fixes sit side by side there.
    /// Where the box would be shorter with the buttons under the text, as in a narrow window, they go there, at the box's right end, and the text takes the box's width.
    /// </summary>
    /// <example><code>
    /// HelpBoxUtils.Draw("No config file yet.", MessageType.Warning);
    /// if (HelpBoxUtils.Draw("Fake Store is on.", MessageType.Warning, new GUIContent("Turn off"))) TurnOff();
    /// switch (HelpBoxUtils.Draw("Unsaved keys.", MessageType.Warning, (new GUIContent("Apply"), true), (new GUIContent("Revert"), canRevert))) { ... }
    /// </code></example>
    public static class HelpBoxUtils {
        /// <summary>The least width of the box's button; one whose label needs more is as wide as its label.</summary>
        public const float ButtonWidth = 110;

        private const int IconGap = 2;

        // Between two buttons of one box, as between Unity's buttons in a row.
        private const int ButtonGap = 4;

        private static readonly Dictionary<int, GUIStyle> withButtons = new();
        private static readonly Dictionary<int, GUIStyle> overButtons = new();

        // Each box's width and its window's at the last repaint, by its message and buttons: the layout pass, which sets the box's height, does
        // not know the box's width yet, so it takes that one moved by as much as the window's width has since, as a window dragged narrower
        // changes it on every frame.
        private static readonly Dictionary<int, (float Box, float View)> lastWidths = new();
        private static GUIStyle plain;
        private static GUIStyle group;

        /// <summary>
        /// Draws the box, laid out as EditorGUILayout.HelpBox; true when its button is clicked.<br/>
        /// An icon on the button draws at <see cref="Section.IconSize"/>.
        /// </summary>
        public static bool Draw(string message, MessageType type, GUIContent button = null, bool enabled = true) {
            return button == null ? Draw(message, type, Array.Empty<(GUIContent, bool)>()) >= 0 : Draw(message, type, (button, enabled)) == 0;
        }

        /// <summary>
        /// Draws the box with its buttons side by side at its right end, the first leftmost; the index of the one clicked, or -1.<br/>
        /// A button that is not enabled draws off, and its tooltip should say why.
        /// </summary>
        public static int Draw(string message, MessageType type, params (GUIContent Content, bool Enabled)[] buttons) {
            var box  = EditorStyles.helpBox;
            var line = EditorStyles.label.lineHeight;
            // The text keeps clear of the icon and the button by padding rather than by an image in its content: GUILayout
            // wraps the text after this call returns, when no icon size is set, so an image there would wrap at its own width.
            plain ??= new GUIStyle(box) { fontSize = EditorStyles.label.fontSize, padding = new RectOffset(box.padding.left + (int) line + IconGap, box.padding.right, box.padding.top, box.padding.bottom) };
            // The group takes the box's margins, so the box sits where one without a button does.
            group ??= new GUIStyle { margin = box.margin };

            var count   = buttons?.Length ?? 0;
            var content = new GUIContent(message);
            var height  = EditorGUIUtility.singleLineHeight + 2;
            var widths  = new float[count];
            for (var i = 0; i < count; i++) widths[i] = Mathf.Max(ButtonWidth, Mathf.Ceil(GUI.skin.button.CalcSize(buttons[i].Content).x));
            var view    = EditorGUIUtility.currentViewWidth;
            var key     = count == 0 ? 0 : Key(message, buttons);
            var under   = count > 0 && lastWidths.TryGetValue(key, out var last) && IsShorterUnder(content, widths, height, last.Box + view - last.View);
            var style   = count == 0 ? plain : under ? OverButtons(height) : WithButtons(widths);

            // With buttons, a group holds the box at least as tall as a button with the box's own padding above and below;
            // a height on the box itself would replace the height its wrapped text needs.
            if (count > 0) GUILayout.BeginVertical(group, GUILayout.MinHeight(height + box.padding.vertical));
            var rect = GUILayoutUtility.GetRect(content, style, GUILayout.ExpandHeight(count > 0));
            if (count > 0) GUILayout.EndVertical();

            // The text's part of the box: all of it, or above the buttons when they go under it.
            var text = under ? Rect.MinMaxRect(rect.x, rect.y, rect.xMax, rect.yMax - height - box.padding.bottom) : rect;
            if (Event.current.type is EventType.Repaint) {
                style.Draw(rect, content, false, false, false, false);
                // Unity's small console icon, a line of the text tall, centered in the box as the text is.
                var icon = Icon(type);
                if (icon) GUI.DrawTexture(PixelUtils.Snap(new Rect(rect.x + box.padding.left, text.center.y - line / 2, line, line)), icon, ScaleMode.ScaleToFit);

                // A guess that turned the buttons the wrong way lays the window out again.
                if (count > 0) {
                    if (lastWidths.Count > 256) lastWidths.Clear();
                    lastWidths[key] = (rect.width, view);
                    if (IsShorterUnder(content, widths, height, rect.width) != under) HandleUtility.Repaint();
                }
            }

            if (count == 0) return -1;

            var clicked = -1;
            var right   = rect.xMax - box.padding.right;
            var middle  = under ? rect.yMax - box.padding.bottom - height / 2 : rect.center.y;
            EditorGUIUtility.SetIconSize(new Vector2(Section.IconSize, Section.IconSize));
            for (var i = count - 1; i >= 0; i--) {
                var place = new Rect(right - widths[i], middle - height / 2, widths[i], height);
                using (new EditorGUI.DisabledScope(!buttons[i].Enabled)) {
                    if (GUI.Button(PixelUtils.Snap(place), buttons[i].Content)) clicked = i;
                }

                right = place.x - ButtonGap;
            }

            EditorGUIUtility.SetIconSize(Vector2.zero);
            return clicked;
        }

        // The buttons side by side, with their gaps.
        private static int Total(float[] widths) {
            var buttons = ButtonGap * (widths.Length - 1);
            foreach (var width in widths) buttons += (int) width;
            return buttons;
        }

        // Whether the box of this width is shorter with its buttons under its text than beside it.
        private static bool IsShorterUnder(GUIContent content, float[] widths, float height, float width) {
            var beside = Mathf.Max(WithButtons(widths).CalcHeight(content, width), height + EditorStyles.helpBox.padding.vertical);
            return OverButtons(height).CalcHeight(content, width) < beside;
        }

        private static int Key(string message, (GUIContent Content, bool Enabled)[] buttons) {
            var key = message?.GetHashCode() ?? 0;
            foreach (var button in buttons) key = key * 31 + (button.Content?.text?.GetHashCode() ?? 0);
            return key;
        }

        // The text across the box, with a line for the buttons under it.
        private static GUIStyle OverButtons(float height) {
            var line = (int) height;
            if (overButtons.TryGetValue(line, out var style)) return style;

            var box = EditorStyles.helpBox;
            style = new GUIStyle(plain) { padding = new RectOffset(plain.padding.left, box.padding.right, box.padding.top, box.padding.bottom * 2 + line), margin = new RectOffset() };
            overButtons[line] = style;
            return style;
        }

        // The text wraps short of every button; one style for each width the buttons take together.
        private static GUIStyle WithButtons(float[] widths) {
            var buttons = Total(widths);
            if (withButtons.TryGetValue(buttons, out var style)) return style;

            var box = EditorStyles.helpBox;
            style = new GUIStyle(plain) { padding = new RectOffset(plain.padding.left, box.padding.right * 2 + buttons, box.padding.top, box.padding.bottom), margin = new RectOffset() };
            withButtons[buttons] = style;
            return style;
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