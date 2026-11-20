using System;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Takes a settings page to one of its fields, as an issue's Edit button does: opens every fold above it, scrolls to it, and clicks it as the user would: into a text or number field, so typing goes there, and on the label of any other, which selects it without changing its value.<br/>
    /// The field must be drawn by a <see cref="SettingsTree"/> over <c>asset</c>, or report its place through <see cref="Drawn"/>; the request waits for that draw, and a newer one replaces it.
    /// </summary>
    public static class SettingsFocus {
        private static Object       asset;

        /// <summary>The asset a request waits to reach, for a page that folds what its trees do not, such as a list's rows; null when none waits.</summary>
        public static Object Asset => asset;
        private static string       path;
        private static EditorWindow window;
        private static bool         scrolled;

        /// <param name="unityPath">The field's serialized path, as SerializedProperty.propertyPath writes it: "devKey", "products.Array.data[2].id".</param>
        public static void Request(Object asset, string unityPath) {
            SettingsFocus.asset = asset;
            path                = unityPath;
            // The page asking, as its button was just clicked.
            window   = EditorWindow.focusedWindow;
            scrolled = false;
            InternalEditorUtility.RepaintAllViews();
        }

        /// <summary>For a text field a page draws itself: call after drawing it, with the rect it took, label left out.</summary>
        public static void Drawn(Object asset, string unityPath, Rect rect) {
            // At the value's left end: a row may end in buttons, such as a key picker.
            Reach(asset, unityPath, rect, new Vector2(Mathf.Min(rect.x + 6, rect.xMax - 2), rect.center.y));
        }

        // The first repaint scrolls to the field; the next one clicks where the field then is.
        private static void Reach(Object asset, string unityPath, Rect rect, Vector2 click) {
            if (path == null || asset != SettingsFocus.asset || unityPath != path || Event.current.type is not EventType.Repaint || rect.height <= 0) return;

            if (!scrolled) {
                GUI.ScrollTo(rect);
                scrolled = true;
                window?.Repaint();
                return;
            }

            if (window) Click(window, GUIUtility.GUIToScreenPoint(click));
            SettingsFocus.asset = null;
            path                = null;
            window              = null;
        }

        // Before the tree draws: the folds above the field open in time for this draw.
        internal static void Open(PropertyTree tree, Object target) {
            if (path == null || target != asset || tree.GetPropertyAtUnityPath(path) is not { } property) return;

            for (var parent = property.Parent; parent != null; parent = parent.Parent) Section.Unfold(parent);
        }

        // After the tree drew.
        internal static void Scroll(PropertyTree tree, Object target) {
            if (path == null || target != asset || tree.GetPropertyAtUnityPath(path) is not { } property) return;

            var type = property.ValueEntry?.TypeOfValue;
            var text = type == typeof(string) || type == typeof(int) || type == typeof(long) || type == typeof(float) || type == typeof(double);
            // Odin keeps the whole row, label included, the value past the label column; a row too narrow for one has no label to click.
            var row      = property.LastDrawnValueRect;
            var labelled = row.width > EditorGUIUtility.labelWidth + 20;
            var line     = row.y + EditorGUIUtility.singleLineHeight / 2;
            var click    = text && labelled ? new Vector2(row.x + EditorGUIUtility.labelWidth + 6, line) : new Vector2(row.x + 6, line);
            if (text || labelled) Reach(target, path, row, click);
            else Reach(target, path, row, new Vector2(float.NaN, float.NaN));
        }

        // A press and release at the field's right end, past any label, as the user's own click: Unity gives the field the keyboard on the press.
        private static void Click(EditorWindow target, Vector2 screen) {
            // A field with no label but its value, such as a toggle alone in a list's row: reaching it is all.
            if (float.IsNaN(screen.x)) return;

            var local = screen - target.position.position;
            EditorApplication.delayCall += () => {
                if (!target) return;

                target.SendEvent(new Event { type = EventType.MouseDown, mousePosition = local, button = 0, clickCount = 1 });
                target.SendEvent(new Event { type = EventType.MouseUp, mousePosition = local, button = 0, clickCount = 1 });
            };
        }
    }
}