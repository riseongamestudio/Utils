using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace RiseOn.Utils {
    public static class HandleUtils {
        private const float DEFAULT_WIDTH = 3;
        private const float DEFAULT_OUTLINE_WIDTH = 3;

        // DrawAAPolyLine scales alpha by 0.75 and clamps only after that, so this brings a color back to full.
        // Measured: nothing more to gain past it, the line's tapered profile is applied after the clamp.
        private const float AA_ALPHA_COMPENSATION = 1 / .75f;

        /// <inheritdoc cref="UnityEditor.Handles.matrix"/>
        public static Matrix4x4 matrix {
            get {
                #if UNITY_EDITOR
                return UnityEditor.Handles.matrix;
                #endif

                #pragma warning disable CS0162 // Unreachable code detected
                return Matrix4x4.identity;
                #pragma warning restore CS0162 // Unreachable code detected
            }
            set {
                #if UNITY_EDITOR
                UnityEditor.Handles.matrix = value;
                #endif
            }
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
            Vector3 corner_0
          , Vector3 corner_1
          , Vector3 corner_2
          , Vector3 corner_3
          , Color faceColor
          , Color outlineColor) {
            #if UNITY_EDITOR
            // Only the first four are read, so a longer rented array is fine.
            var corners = ArrayPool<Vector3>.Shared.Rent(4);

            try {
                corners[0] = corner_0;
                corners[1] = corner_1;
                corners[2] = corner_2;
                corners[3] = corner_3;

                UnityEditor.Handles.DrawSolidRectangleWithOutline(corners, faceColor, outlineColor);
            } finally {
                ArrayPool<Vector3>.Shared.Return(corners);
            }
            #endif
        }

        /// <inheritdoc cref="UnityEditor.Handles.DrawSolidRectangleWithOutline(Vector3[], Color, Color)"/>
        [Conditional("UNITY_EDITOR")]
        public static void DrawRect(Vector3[] corners, Color faceColor, Color outlineColor) {
            #if UNITY_EDITOR
            UnityEditor.Handles.DrawSolidRectangleWithOutline(corners, faceColor, outlineColor);
            #endif
        }

        /// <inheritdoc cref="UnityEditor.Handles.DrawSolidRectangleWithOutline(Rect, Color, Color)"/>
        [Conditional("UNITY_EDITOR")]
        public static void DrawRect(Rect rect, Color faceColor, Color outlineColor) {
            DrawRect(
                new(rect.xMin, rect.yMin)
              , new(rect.xMax, rect.yMin)
              , new(rect.xMax, rect.yMax)
              , new(rect.xMin, rect.yMax)
              , faceColor
              , outlineColor);
        }

        /// <summary>
        /// An anti-aliased line, placed in world space through <see cref="UnityEditor.Handles.matrix"/>, not <see cref="Gizmos.matrix"/>; <paramref name="width"/> is screen pixels like any other handle line.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void DrawLine(Vector3 from, Vector3 to, Color color, float width = DEFAULT_WIDTH) {
            #if UNITY_EDITOR
            var points = RentLine(from, to);

            try {
                Draw(points, 2, color, width);
            } finally {
                ArrayPool<Vector3>.Shared.Return(points);
            }
            #endif
        }

        /// <summary>
        /// <see cref="DrawLine"/> through every point in turn, back to the first one when <paramref name="closed"/>.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void DrawPath(IReadOnlyList<Vector3> points, Color color, float width = DEFAULT_WIDTH, bool closed = false) {
            #if UNITY_EDITOR
            if (points.Count < 2) return;

            var buffer = RentPath(points, closed, out var count);

            try {
                Draw(buffer, count, color, width);
            } finally {
                ArrayPool<Vector3>.Shared.Return(buffer);
            }
            #endif
        }

        /// <summary>
        /// <see cref="DrawLine"/> around a circle in the XY plane, with a horizontal and a vertical diameter when <paramref name="cross"/>, the way <see cref="Gizmos.DrawWireSphere"/> looks head-on.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void DrawWireCircle(Vector3 center, float radius, Color color, float width = DEFAULT_WIDTH, bool cross = false) {
            #if UNITY_EDITOR
            var buffer = RentCircle(center, radius, out var count);

            try {
                Draw(buffer, count, color, width);
            } finally {
                ArrayPool<Vector3>.Shared.Return(buffer);
            }

            if (!cross) return;

            DrawLine(center + Vector3.left * radius, center + Vector3.right * radius, color, width);
            DrawLine(center + Vector3.down * radius, center + Vector3.up * radius, color, width);
            #endif
        }

        /// <summary>
        /// <see cref="DrawLine"/> around every point within <paramref name="radius"/> of the segment <paramref name="from"/> → <paramref name="to"/>, in the XY plane: two half circles joined by straight sides. A segment of no length draws a whole circle.<br/>
        /// With <paramref name="cross"/>, also one line along the capsule from tip to tip and one across it at each end of the segment, the way <see cref="DrawWireCircle"/> crosses a circle.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void DrawWireCapsule(Vector3 from, Vector3 to, float radius, Color color, float width = DEFAULT_WIDTH, bool cross = false) {
            #if UNITY_EDITOR
            var direction = ((Vector2)(to - from)).normalized;

            if (direction == Vector2.zero) {
                DrawWireCircle(from, radius, color, width, cross);

                return;
            }

            var buffer = RentCapsule(from, to, radius, out var count);

            try {
                Draw(buffer, count, color, width);
            } finally {
                ArrayPool<Vector3>.Shared.Return(buffer);
            }

            if (!cross) return;

            var along = (Vector3)(direction * radius);
            var across = (Vector3)(Vector2.Perpendicular(direction) * radius);

            DrawLine(from - along, to + along, color, width);
            DrawLine(from - across, from + across, color, width);
            DrawLine(to - across, to + across, color, width);
            #endif
        }

        /// <summary>
        /// <see cref="DrawLine"/> over a wider one in <paramref name="outlineColor"/>, so it reads on any background; <paramref name="outlineWidth"/> is added on each side.
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
            var points = RentLine(from, to);

            try {
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

            var buffer = RentPath(points, closed, out var count);

            try {
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
            var buffer = RentCircle(center, radius, out var count);

            try {
                DrawOutlined(buffer, count, true, color, outlineColor, width, outlineWidth);
            } finally {
                ArrayPool<Vector3>.Shared.Return(buffer);
            }
            #endif
        }

        private static Vector3[] RentLine(Vector3 from, Vector3 to) {
            var buffer = ArrayPool<Vector3>.Shared.Rent(2);

            buffer[0] = from;
            buffer[1] = to;

            return buffer;
        }

        // A closed path gets its first point again at the end, so it draws as one polyline.
        private static Vector3[] RentPath(IReadOnlyList<Vector3> points, bool closed, out int count) {
            count = closed ? points.Count + 1 : points.Count;

            var buffer = ArrayPool<Vector3>.Shared.Rent(count);

            for (var i = 0; i < points.Count; ++i) buffer[i] = points[i];

            if (closed) buffer[points.Count] = points[0];

            return buffer;
        }

        private static Vector3[] RentCircle(Vector3 center, float radius, out int count) {
            const int SEGMENTS = 48;

            count = SEGMENTS + 1;

            var buffer = ArrayPool<Vector3>.Shared.Rent(count);

            for (var i = 0; i < SEGMENTS; ++i) {
                var angle = i * (Mathf.PI * 2 / SEGMENTS);

                buffer[i] = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }

            buffer[SEGMENTS] = buffer[0];

            return buffer;
        }

        // One closed polyline, around the `to` end and back around the `from` end, so the sides join the arcs without a
        // seam.
        private static Vector3[] RentCapsule(Vector3 from, Vector3 to, float radius, out int count) {
            const int ARC_SEGMENTS = 24;

            var direction = ((Vector2)(to - from)).normalized;

            if (direction == Vector2.zero) return RentCircle(from, radius, out count);

            var normal = Vector2.Perpendicular(direction);

            count = (ARC_SEGMENTS + 1) * 2 + 1;

            var buffer = ArrayPool<Vector3>.Shared.Rent(count);

            AddArc(buffer, 0, to, normal, direction, radius);
            AddArc(buffer, ARC_SEGMENTS + 1, from, -normal, -direction, radius);

            buffer[count - 1] = buffer[0];

            return buffer;

            // Half a circle around `center`, from `start` through `apex` to `-start`.
            static void AddArc(Vector3[] buffer, int offset, Vector3 center, Vector2 start, Vector2 apex, float radius) {
                for (var i = 0; i <= ARC_SEGMENTS; ++i) {
                    var angle = i * (Mathf.PI / ARC_SEGMENTS);

                    buffer[offset + i] = center + (Vector3)((start * Mathf.Cos(angle) + apex * Mathf.Sin(angle)) * radius);
                }
            }
        }

        private static void Draw(Vector3[] points, int count, Color color, float width) {
            #if UNITY_EDITOR
            var oldColor = UnityEditor.Handles.color;

            UnityEditor.Handles.color = color.WithA(color.a * AA_ALPHA_COMPENSATION);
            UnityEditor.Handles.DrawAAPolyLine(width, count, points);
            UnityEditor.Handles.color = oldColor;
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

            UnityEditor.Handles.color = outlineColor.WithA(outlineColor.a * AA_ALPHA_COMPENSATION);
            UnityEditor.Handles.DrawAAPolyLine(width + outlineWidth * 2, count, points);

            points[0] = first;
            points[count - 1] = last;

            UnityEditor.Handles.color = color.WithA(color.a * AA_ALPHA_COMPENSATION);
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
