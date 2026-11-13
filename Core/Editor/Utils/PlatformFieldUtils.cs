using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// A text value for Android and one for iOS under one label: both on the label's row, the two fields as wide as each other, while each is at least <see cref="FieldMinWidth"/> wide, else a row each, as Unity drops a vector's fields to a row of their own in a narrow Inspector.<br/>
    /// Every Android and iOS pair of a settings page draws with it.
    /// </summary>
    /// <example><code>
    /// PlatformFieldUtils.Draw(new GUIContent("App Key"), settings.FindProperty("androidKey"), settings.FindProperty("iosKey"));
    /// </code></example>
    public static class PlatformFieldUtils {
        /// <summary>The least width of each field for both to share a row: an AdMob ad unit id, the longest these pairs hold, shows whole.</summary>
        public const float FieldMinWidth = 250;

        private static readonly GUIContent android = new("Android");
        private static readonly GUIContent ios     = new("iOS");

        // The value column's width at the last repaint, by control: the next frame lays out one row or two by it.
        private static readonly Dictionary<int, float> widths = new();

        /// <summary>Draws the pair of string properties.</summary>
        public static void Draw(GUIContent label, SerializedProperty androidValue, SerializedProperty iosValue) {
            var a = androidValue.stringValue;
            var i = iosValue.stringValue;
            if (!Draw(label, ref a, ref i)) return;

            androidValue.stringValue = a;
            iosValue.stringValue     = i;
        }

        /// <summary>Draws the pair; true when either changed. A null label gives the values the whole row.</summary>
        public static bool Draw(GUIContent label, ref string androidValue, ref string iosValue) {
            var id    = GUIUtility.GetControlID(FocusType.Passive);
            var wide  = !widths.TryGetValue(id, out var width) || Fits(width);
            var row   = EditorGUILayout.GetControlRect(label != null);
            var value = label == null ? row : EditorGUI.PrefixLabel(row, label);
            if (Event.current.type is EventType.Repaint) widths[id] = value.width;

            EditorGUI.BeginChangeCheck();
            if (wide) {
                // Each name as wide as its own text, as a vector's X, Y and Z are, so it sits by its field; the fields as wide as each other.
                Split(value, out var left, out var right);
                androidValue = Field(left, android, Width(android), androidValue);
                iosValue     = Field(right, ios, Width(ios), iosValue);
            } else {
                // Stacked, both names as wide as the longer, so the two fields start in line.
                androidValue = Field(value, android, Width(android), androidValue);
                var next = EditorGUILayout.GetControlRect(label != null);
                iosValue = Field(new Rect(value.x, next.y, value.width, next.height), ios, Width(android), iosValue);
            }

            return EditorGUI.EndChangeCheck();
        }

        /// <summary>The pair in one row of <paramref name="value"/>, whatever its width: for a row laid out by hand, such as a section's bar.</summary>
        public static bool Draw(Rect value, ref string androidValue, ref string iosValue) {
            EditorGUI.BeginChangeCheck();
            Split(value, out var left, out var right);
            androidValue = Field(left, android, Width(android), androidValue);
            iosValue     = Field(right, ios, Width(ios), iosValue);
            return EditorGUI.EndChangeCheck();
        }

        // A name's text and Unity's space between a label and its field.
        private static float Width(GUIContent name) => EditorStyles.label.CalcSize(name).x + SettingsSection.LabelGap;

        // Between the Android field and the iOS name, as RectFieldUtils.Split leaves between its halves.
        private const float Gap = 6;

        private static bool Fits(float width) {
            Split(new Rect(0, 0, width, 0), out var left, out _);
            return left.width - Width(android) >= FieldMinWidth;
        }

        // The row's value column in two, each field after its name and as wide as the other. Too narrow for the names, they share
        // what there is, so neither covers the other.
        private static void Split(Rect value, out Rect left, out Rect right) {
            var names = Width(android) + Width(ios) + Gap;
            var width = value.width >= names ? Width(android) + (value.width - names) / 2 : Mathf.Max(0, value.width - Gap) * Width(android) / (names - Gap);
            left  = new Rect(value.x, value.y, width, value.height);
            right = Rect.MinMaxRect(Mathf.Min(left.xMax + Gap, value.xMax), value.y, value.xMax, value.yMax);
        }

        private static string Field(Rect rect, GUIContent name, float nameWidth, string value) {
            var label = new Rect(rect.x, rect.y, Mathf.Min(nameWidth, rect.width), rect.height);
            GUI.Label(label, name, EditorStyles.label);
            return EditorGUI.TextField(Rect.MinMaxRect(label.xMax, rect.y, rect.xMax, rect.yMax), value ?? "");
        }
    }
}