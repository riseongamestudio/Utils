using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace RiseOn.Utils {
    public static class HandleUtils {
        private const float DEFAULT_WIDTH = 1;
        private const float DEFAULT_OUTLINE_WIDTH = 1;

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
        /// A line drawn over a wider one in <paramref name="outlineColor"/>, so it reads on any background.<br/>
        /// Placed in world space through <see cref="UnityEditor.Handles.matrix"/>, not <see cref="Gizmos.matrix"/>; widths are screen pixels like any other handle line, and <paramref name="outlineWidth"/> is added on each side.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void DrawOutlinedLine(
            Vector3 from
          , Vector3 to
          , Color color
          , Color outlineColor
          , float width = DEFAULT_WIDTH
          , float outlineWidth = DEFAULT_OUTLINE_WIDTH) {
            #if UNITY_EDITOR
            var points = ArrayPool<Vector3>.Shared.Rent(2);

            try {
                points[0] = from;
                points[1] = to;
                DrawOutlined(points, 2, false, color, outlineColor, width, outlineWidth);
            } finally {
                ArrayPool<Vector3>.Shared.Return(points);
            }
            #endif
        }

        /// <summary>
        /// <see cref="DrawOutlinedLine"/> through every point in turn, back to the first one when <paramref name="closed"/>.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void DrawOutlinedPath(
            IReadOnlyList<Vector3> points
          , Color color
          , Color outlineColor
          , float width = DEFAULT_WIDTH
          , float outlineWidth = DEFAULT_OUTLINE_WIDTH
          , bool closed = false) {
            #if UNITY_EDITOR
            if (points.Count < 2) return;

            var count = closed ? points.Count + 1 : points.Count;
            var buffer = ArrayPool<Vector3>.Shared.Rent(count);

            try {
                for (var i = 0; i < points.Count; ++i) buffer[i] = points[i];

                if (closed) buffer[points.Count] = points[0];

                DrawOutlined(buffer, count, closed, color, outlineColor, width, outlineWidth);
            } finally {
                ArrayPool<Vector3>.Shared.Return(buffer);
            }
            #endif
        }

        /// <summary>
        /// <see cref="DrawOutlinedLine"/> around a circle in the XY plane.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void DrawOutlinedWireCircle(
            Vector3 center
          , float radius
          , Color color
          , Color outlineColor
          , float width = DEFAULT_WIDTH
          , float outlineWidth = DEFAULT_OUTLINE_WIDTH) {
            #if UNITY_EDITOR
            const int CIRCLE_SEGMENTS = 48;

            var buffer = ArrayPool<Vector3>.Shared.Rent(CIRCLE_SEGMENTS + 1);

            try {
                for (var i = 0; i < CIRCLE_SEGMENTS; ++i) {
                    var angle = i * (Mathf.PI * 2 / CIRCLE_SEGMENTS);

                    buffer[i] = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                }

                buffer[CIRCLE_SEGMENTS] = buffer[0];

                DrawOutlined(buffer, CIRCLE_SEGMENTS + 1, true, color, outlineColor, width, outlineWidth);
            } finally {
                ArrayPool<Vector3>.Shared.Return(buffer);
            }
            #endif
        }

        // Outline first, the line over it: the wider stroke only shows as a rim. Both are one polyline each, so no rim
        // cuts across a joint; an open path's outline also runs past both ends by its own width, so the rim wraps the
        // ends too. That run is measured in screen pixels, hence the points go out to world and the matrix steps aside.
        private static void DrawOutlined(Vector3[] points, int count, bool closed, Color color, Color outlineColor, float width, float outlineWidth) {
            #if UNITY_EDITOR
            var camera = Camera.current;
            var matrix = UnityEditor.Handles.matrix;
            var oldColor = UnityEditor.Handles.color;

            for (var i = 0; i < count; ++i) points[i] = matrix.MultiplyPoint3x4(points[i]);

            UnityEditor.Handles.matrix = Matrix4x4.identity;

            var first = points[0];
            var last = points[count - 1];

            if (!closed && camera != null) {
                points[0] = Extend(camera, points[1], first, outlineWidth);
                points[count - 1] = Extend(camera, points[count - 2], last, outlineWidth);
            }

            UnityEditor.Handles.color = outlineColor;
            UnityEditor.Handles.DrawAAPolyLine(width + outlineWidth * 2, count, points);

            points[0] = first;
            points[count - 1] = last;

            UnityEditor.Handles.color = color;
            UnityEditor.Handles.DrawAAPolyLine(width, count, points);

            UnityEditor.Handles.matrix = matrix;
            UnityEditor.Handles.color = oldColor;

            // `to` pushed on along from → to by the given number of screen pixels.
            static Vector3 Extend(Camera camera, Vector3 from, Vector3 to, float pixels) {
                var screenFrom = camera.WorldToScreenPoint(from);
                var screenTo = camera.WorldToScreenPoint(to);
                var direction = (Vector2)(screenTo - screenFrom);

                if (direction.sqrMagnitude < 1e-6f) return to;

                return camera.ScreenToWorldPoint(screenTo + (Vector3)(direction.normalized * pixels));
            }
            #endif
        }

        /// <summary>
        /// Like <see cref="UnityEditor.Handles.Label(Vector3, string)"/>, except the text is centered on <paramref name="position"/> instead of hanging off it by its top-left corner.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void Label(Vector3 position, string text, Color color) {
            #if UNITY_EDITOR
            // Drawn by hand inside the GUI pass rather than through Handles.Label: that one pins the text box's
            // top-left to the point, and no style setting moves it, since the box is cut to fit the text. Centering
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
