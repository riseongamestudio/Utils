using System.Diagnostics;
using UnityEngine;

namespace RiseOn.Utils {
    public static class HandlesUtils {
        private static Vector3[] rectCorners;

        // Reused by every DrawRect call, so drawing gizmos allocates nothing.
        private static Vector3[] RectCorners => rectCorners ??= new Vector3[4];

        /// <inheritdoc cref="UnityEditor.Handles.matrix"/>
        public static Matrix4x4 GetMatrix() {
            #if UNITY_EDITOR
            return UnityEditor.Handles.matrix;
            #endif

            #pragma warning disable CS0162 // Unreachable code detected
            return Matrix4x4.identity;
            #pragma warning restore CS0162 // Unreachable code detected
        }

        /// <inheritdoc cref="UnityEditor.Handles.matrix"/>
        [Conditional("UNITY_EDITOR")]
        public static void SetMatrix(Matrix4x4 matrix) {
            #if UNITY_EDITOR
            UnityEditor.Handles.matrix = matrix;
            #endif
        }

        /// <inheritdoc cref="UnityEditor.HandleUtility.GetHandleSize(Vector3)"/>
        public static float GetSize(Vector3 position) {
            #if UNITY_EDITOR
            return UnityEditor.HandleUtility.GetHandleSize(position);
            #endif

            #pragma warning disable CS0162 // Unreachable code detected
            return 1;
            #pragma warning restore CS0162 // Unreachable code detected
        }

        /// <inheritdoc cref="UnityEditor.Handles.DrawSolidRectangleWithOutline(Vector3[], Color, Color)"/>
        [Conditional("UNITY_EDITOR")]
        public static void DrawRect(
            Color faceColor
          , Color outlineColor
          , Vector3 corner_0
          , Vector3 corner_1
          , Vector3 corner_2
          , Vector3 corner_3) {
            #if UNITY_EDITOR
            var corners = RectCorners;
            corners[0] = corner_0;
            corners[1] = corner_1;
            corners[2] = corner_2;
            corners[3] = corner_3;

            UnityEditor.Handles.DrawSolidRectangleWithOutline(corners, faceColor, outlineColor);
            #endif
        }

        /// <summary>
        /// Like <see cref="UnityEditor.Handles.Label(Vector3, string)"/>, except the text is centred on
        /// <paramref name="position"/> instead of hanging off it by its top-left corner.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Label(Vector3 position, string text, Color color) {
            #if UNITY_EDITOR
            // Drawn by hand inside the GUI pass rather than through Handles.Label: that one pins the text box's
            // top-left to the point, and no style setting moves it, since the box is cut to fit the text. Centring
            // means knowing how wide the text comes out, and CalcSize only answers once the skin is up.
            UnityEditor.Handles.BeginGUI();

            var style = GUI.skin.label;
            var size = style.CalcSize(new GUIContent(text));
            var point = UnityEditor.HandleUtility.WorldToGUIPoint(position);
            var oldColor = GUI.color;

            GUI.color = color;
            GUI.Label(new Rect(point.x - size.x / 2, point.y - size.y / 2, size.x, size.y), text, style);
            GUI.color = oldColor;

            UnityEditor.Handles.EndGUI();
            #endif
        }
    }
}
