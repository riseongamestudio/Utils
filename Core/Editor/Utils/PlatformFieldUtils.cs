using System.Collections.Generic;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// A text value for Android and one for iOS under one label: both on the label's row, the two fields as wide as each other, while each is at least <see cref="FieldMinWidth"/> wide, else a row each, as Unity drops a vector's fields to a row of their own in a narrow Inspector.<br/>
    /// Between one row and two the iOS field slides to its place and the rows below with it, as <see cref="SectionTabs"/> slides, repainting every frame of it on any window; a window opening shows the pair in its place at once.<br/>
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

        // As long as SectionTabs' slide, and eased as it is.
        private const float Duration = 0.25f;

        // A pair in one window: the value column's width at its last repaint, which the next frame lays out one row or two by, and how
        // far it is from one row (0) to two (1), with where its move started.
        private sealed class Pair {
            public float  Width = -1;
            public bool   Shown;
            public bool   Settle;
            public float  From;
            public float  To;
            public double Started = -1;
            public float  Progress = 1;

            public float Value => Mathf.Lerp(From, To, 1 - (1 - Progress) * (1 - Progress));
        }

        // By window, control and label: a window opening, or a page of it, starts its own pairs.
        private static readonly Dictionary<(int window, int control, string label), Pair> pairs = new();

        // By label, the width a pair had last in any window: a pair a window has not drawn yet starts as wide as it, which a window
        // opened again at the size it was left at is. By label, as a control's id differs from one window to the next.
        private static readonly Dictionary<string, float> lastWidths = new();

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
            bool androidOn = false, iosOn = false;
            return Draw(label, ref androidValue, ref iosValue, null, ref androidOn, ref iosOn);
        }

        /// <summary>
        /// The pair with <paramref name="toggle"/> after each field, a switch each platform has of its own, such as an ad unit's Test; true when
        /// anything changed. A null <paramref name="toggle"/> draws none.
        /// </summary>
        public static bool Draw(GUIContent label, ref string androidValue, ref string iosValue, GUIContent toggle, ref bool androidOn, ref bool iosOn) {
            var id     = GUIUtility.GetControlID(FocusType.Passive);
            var window = GUIHelper.CurrentWindow;
            var name   = label?.text ?? "";
            var at     = (window ? window.GetInstanceID() : 0, id, name);
            if (!pairs.TryGetValue(at, out var pair)) pairs[at] = pair = new Pair { Width = lastWidths.TryGetValue(name, out var last) ? last : -1 };

            var extra   = ToggleWidth(toggle);
            var stacked = StackOf(pair, pair.Width >= 0 && !Fits(pair.Width, extra));
            var line    = EditorGUIUtility.singleLineHeight;
            var below   = line + EditorGUIUtility.standardVerticalSpacing;
            var row     = EditorGUILayout.GetControlRect(label != null, line + stacked * below);
            var first   = new Rect(row.x, row.y, row.width, line);
            var value   = label == null ? first : EditorGUI.PrefixLabel(first, label);
            if (Event.current.type is EventType.Repaint) {
                // The first repaint knows the width the window really gives; a pair laid out by another window's width takes its place
                // at once on the next frame, rather than sliding to it as the window opens.
                if (!pair.Shown && (pair.Width < 0 || Fits(pair.Width, extra) != Fits(value.width, extra))) {
                    pair.Settle = true;
                    HandleUtility.Repaint();
                }

                pair.Width = lastWidths[name] = value.width;
                pair.Shown = true;
            }

            // On one row each name as wide as its own text, as a vector's X, Y and Z are, so it sits by its field, the fields as wide
            // as each other; stacked, the Android field takes the row and the iOS one the row below, its name as wide as the longer, so
            // the two fields start in line.
            Split(value, out var left, out var right);
            var under = new Rect(value.x, value.y + below, value.width, line);

            EditorGUI.BeginChangeCheck();
            androidValue = Field(Lerp(left, value, stacked), android, Width(android), androidValue, toggle, ref androidOn);
            iosValue     = Field(Lerp(right, under, stacked), ios, Mathf.Lerp(Width(ios), Width(android), stacked), iosValue, toggle, ref iosOn);
            return EditorGUI.EndChangeCheck();

            // Where the pair is between one row and two, moving toward stacked or not. It changes only at layout, so every event of
            // a frame lays out the same; the clock starts with the first frame shown, so a move begun while the window waits is still seen.
            // Until its first repaint a pair is where its width puts it, with no move: the window is only opening.
            static float StackOf(Pair stack, bool stacked) {
                var to = stacked ? 1f : 0f;
                if (Event.current.type is EventType.Layout) {
                    if (!stack.Shown || stack.Settle) {
                        stack.From     = stack.To = to;
                        stack.Progress = 1;
                        stack.Settle   = false;
                    } else if (!Mathf.Approximately(stack.To, to)) {
                        stack.From     = stack.Value;
                        stack.To       = to;
                        stack.Started  = -1;
                        stack.Progress = 0;
                    } else if (stack.Started >= 0) {
                        stack.Progress = Mathf.Clamp01((float) ((EditorApplication.timeSinceStartup - stack.Started) / Duration));
                    }
                }

                if (stack.Progress < 1) {
                    if (Event.current.type is EventType.Repaint && stack.Started < 0) stack.Started = EditorApplication.timeSinceStartup;
                    HandleUtility.Repaint();
                }

                return stack.Value;
            }

            static Rect Lerp(Rect from, Rect to, float t) {
                return Rect.MinMaxRect(Mathf.Lerp(from.xMin, to.xMin, t), Mathf.Lerp(from.yMin, to.yMin, t), Mathf.Lerp(from.xMax, to.xMax, t), Mathf.Lerp(from.yMax, to.yMax, t));
            }
        }

        /// <summary>The pair in one row of <paramref name="value"/>, whatever its width: for a row laid out by hand, such as a section's bar.</summary>
        public static bool Draw(Rect value, ref string androidValue, ref string iosValue) {
            bool androidOn = false, iosOn = false;
            return Draw(value, ref androidValue, ref iosValue, null, ref androidOn, ref iosOn);
        }

        /// <summary>The pair in one row of <paramref name="value"/>, with <paramref name="toggle"/> after each field as the layout overload draws it.</summary>
        public static bool Draw(Rect value, ref string androidValue, ref string iosValue, GUIContent toggle, ref bool androidOn, ref bool iosOn) {
            EditorGUI.BeginChangeCheck();
            Split(value, out var left, out var right);
            androidValue = Field(left, android, Width(android), androidValue, toggle, ref androidOn);
            iosValue     = Field(right, ios, Width(ios), iosValue, toggle, ref iosOn);
            return EditorGUI.EndChangeCheck();
        }

        // A name's text and Unity's space between a label and its field.
        private static float Width(GUIContent name) => EditorStyles.label.CalcSize(name).x + Section.LabelGap;

        // Between the Android field and the iOS name: Unity's space between the fields of one row, as in Vector2Field
        // (EditorGUI.kSpacingSubLabel).
        private const float Gap = 4;

        private static bool Fits(float width, float extra) {
            Split(new Rect(0, 0, width, 0), out var left, out _);
            return left.width - Width(android) - extra >= FieldMinWidth;
        }

        // A toggle after a field: a button joined to the field's right edge, as one control.
        private static float ToggleWidth(GUIContent toggle) => toggle == null ? 0 : EditorStyles.miniButtonRight.CalcSize(toggle).x;

        // The row's value column in two, each field after its name and as wide as the other. Too narrow for the names, they share
        // what there is, so neither covers the other.
        private static void Split(Rect value, out Rect left, out Rect right) {
            var names = Width(android) + Width(ios) + Gap;
            var width = value.width >= names ? Width(android) + (value.width - names) / 2 : Mathf.Max(0, value.width - Gap) * Width(android) / (names - Gap);
            left  = new Rect(value.x, value.y, width, value.height);
            right = Rect.MinMaxRect(Mathf.Min(left.xMax + Gap, value.xMax), value.y, value.xMax, value.yMax);
        }

        private static string Field(Rect rect, GUIContent name, float nameWidth, string value, GUIContent toggle, ref bool on) {
            var label = new Rect(rect.x, rect.y, Mathf.Min(nameWidth, rect.width), rect.height);
            GUI.Label(label, name, EditorStyles.label);
            var end = Mathf.Max(label.xMax, rect.xMax - ToggleWidth(toggle));
            // The field runs a pixel under the button, so their borders meet as one.
            var text = EditorGUI.TextField(Rect.MinMaxRect(label.xMax, rect.y, toggle == null ? end : end + 1, rect.yMax), value ?? "");
            if (toggle != null) on = GUI.Toggle(Rect.MinMaxRect(end, rect.y, rect.xMax, rect.yMax), on, toggle, EditorStyles.miniButtonRight);
            return text;
        }
    }
}