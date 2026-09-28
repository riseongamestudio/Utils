using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace RiseOn.Utils {
    /// <summary>
    /// Outlined lines drawn with <see cref="Gizmos"/>, so they follow <see cref="Gizmos.matrix"/> and gizmo visibility.<br/>
    /// Gizmo lines are always 1 pixel wide, so these have no width to set: the outline is two more 1-pixel lines, one pixel either side on screen.<br/>
    /// Use <see cref="HandleUtils"/> for widths.
    /// </summary>
    public static class GizmoUtils {
        private const int CIRCLE_SEGMENTS = 48;

        /// <summary>
        /// A line with an <paramref name="outlineColor"/> rim one pixel wide on each side, so it reads on any background.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void DrawOutlinedLine(Vector3 from, Vector3 to, Color color, Color outlineColor) {
            var points = ArrayPool<Vector3>.Shared.Rent(2);

            try {
                points[0] = from;
                points[1] = to;
                DrawOutlined(points, 2, false, color, outlineColor);
            } finally {
                ArrayPool<Vector3>.Shared.Return(points);
            }
        }

        /// <summary>
        /// <see cref="DrawOutlinedLine"/> through every point in turn, back to the first one when <paramref name="closed"/>.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void DrawOutlinedPath(IReadOnlyList<Vector3> points, Color color, Color outlineColor, bool closed = false) {
            if (points.Count < 2) return;

            var count = closed ? points.Count + 1 : points.Count;
            var buffer = ArrayPool<Vector3>.Shared.Rent(count);

            try {
                for (var i = 0; i < points.Count; ++i) buffer[i] = points[i];

                if (closed) buffer[points.Count] = points[0];

                DrawOutlined(buffer, count, closed, color, outlineColor);
            } finally {
                ArrayPool<Vector3>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// <see cref="DrawOutlinedLine"/> around a circle in the XY plane.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        public static void DrawOutlinedWireCircle(Vector3 center, float radius, Color color, Color outlineColor) {
            var buffer = ArrayPool<Vector3>.Shared.Rent(CIRCLE_SEGMENTS + 1);

            try {
                for (var i = 0; i < CIRCLE_SEGMENTS; ++i) {
                    var angle = i * (Mathf.PI * 2 / CIRCLE_SEGMENTS);

                    buffer[i] = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                }

                buffer[CIRCLE_SEGMENTS] = buffer[0];

                DrawOutlined(buffer, CIRCLE_SEGMENTS + 1, true, color, outlineColor);
            } finally {
                ArrayPool<Vector3>.Shared.Return(buffer);
            }
        }

        // The rim is laid out in screen pixels, so the points go out to world first and are drawn with an identity
        // matrix. The whole rim goes down before any of the line, so a neighboring segment's rim never covers it.
        // `count` includes the repeated first point of a closed path.
        private static void DrawOutlined(Vector3[] points, int count, bool closed, Color color, Color outlineColor) {
            var camera = Camera.current;
            var matrix = Gizmos.matrix;
            var oldColor = Gizmos.color;

            for (var i = 0; i < count; ++i) points[i] = matrix.MultiplyPoint3x4(points[i]);

            Gizmos.matrix = Matrix4x4.identity;

            if (camera != null) {
                var vertices = closed ? count - 1 : count;
                var screen = ArrayPool<Vector3>.Shared.Rent(vertices);

                try {
                    for (var i = 0; i < vertices; ++i) screen[i] = camera.WorldToScreenPoint(points[i]);

                    Gizmos.color = outlineColor;

                    DrawSide(camera, screen, vertices, closed, 1);
                    DrawSide(camera, screen, vertices, closed, -1);

                    if (!closed) {
                        DrawCap(camera, screen, 0, 1);
                        DrawCap(camera, screen, vertices - 1, vertices - 2);
                    }
                } finally {
                    ArrayPool<Vector3>.Shared.Return(screen);
                }
            }

            Gizmos.color = color;

            for (var i = 1; i < count; ++i) Gizmos.DrawLine(points[i - 1], points[i]);

            Gizmos.matrix = matrix;
            Gizmos.color = oldColor;

            // One side of the rim as one unbroken line a pixel out from the path, mitered at every joint so it bends
            // around corners instead of breaking at them; on an open path it also runs a pixel past each end, where
            // the cap joins the two sides.
            static void DrawSide(Camera camera, Vector3[] screen, int vertices, bool closed, float side) {
                var started = false;
                var first = Vector3.zero;
                var previous = Vector3.zero;

                for (var i = 0; i < vertices; ++i) {
                    var hasIn = closed || i > 0;
                    var hasOut = closed || i < vertices - 1;
                    var directionIn = hasIn ? Direction(screen[(i - 1 + vertices) % vertices], screen[i]) : Vector2.zero;
                    var directionOut = hasOut ? Direction(screen[i], screen[(i + 1) % vertices]) : Vector2.zero;

                    if (hasIn && hasOut) {
                        var normalIn = Vector2.Perpendicular(directionIn);
                        var normalOut = Vector2.Perpendicular(directionOut);
                        var miter = normalIn + normalOut;
                        var uTurn = miter.sqrMagnitude < 1e-6f;
                        var cos = uTurn ? 0 : Vector2.Dot(miter.normalized, normalOut);

                        // Past a 120° turn the miter would spike far off the stroke. The outer side is beveled
                        // instead, a pixel past the vertex and back across, which on a U-turn is an end's cap; the
                        // inner side stays clamped, the core covers it there.
                        if (cos >= .5f) {
                            Emit(camera, screen[i], miter.normalized * (side / cos), ref started, ref first, ref previous);
                        } else if (uTurn || Cross(directionIn, directionOut) * side < 0) {
                            Emit(camera, screen[i], normalIn * side + directionIn, ref started, ref first, ref previous);
                            Emit(camera, screen[i], normalOut * side - directionOut, ref started, ref first, ref previous);
                        } else {
                            Emit(camera, screen[i], miter.normalized * (side * 2), ref started, ref first, ref previous);
                        }
                    } else if (hasOut) {
                        Emit(camera, screen[i], Vector2.Perpendicular(directionOut) * side - directionOut, ref started, ref first, ref previous);
                    } else {
                        Emit(camera, screen[i], Vector2.Perpendicular(directionIn) * side + directionIn, ref started, ref first, ref previous);
                    }
                }

                if (closed) Gizmos.DrawLine(previous, first);

                static void Emit(Camera camera, Vector3 vertex, Vector2 offset, ref bool started, ref Vector3 first, ref Vector3 previous) {
                    var point = camera.ScreenToWorldPoint(vertex + (Vector3)offset);

                    if (started) Gizmos.DrawLine(previous, point);
                    else first = point;

                    started = true;
                    previous = point;
                }

                static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
            }

            // Across the end at `tip`, a pixel beyond it, joining the two sides where they stop.
            static void DrawCap(Camera camera, Vector3[] screen, int tip, int neighbour) {
                var outward = -Direction(screen[tip], screen[neighbour]);
                var normal = Vector2.Perpendicular(outward);
                var end = screen[tip] + (Vector3)outward;

                Gizmos.DrawLine(camera.ScreenToWorldPoint(end + (Vector3)normal), camera.ScreenToWorldPoint(end - (Vector3)normal));
            }

            static Vector2 Direction(Vector3 from, Vector3 to) {
                var direction = (Vector2)(to - from);

                return direction.sqrMagnitude < 1e-6f ? Vector2.right : direction.normalized;
            }
        }
    }
}
