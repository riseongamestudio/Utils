using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace RiseOn.Utils {
    public static class VectorExtensions {
        public static float Get(this Vector3 value, VecAxis axis) => axis switch {
            VecAxis.X => value.x
          , VecAxis.Y => value.y
          , VecAxis.Z => value.z

          , _ => throw new ArgumentOutOfRangeException(nameof(axis), axis, null)
        };

        public static void Set(ref this Vector3 value, VecAxis axis, float newValue) {
            switch (axis) {
                case VecAxis.X: value.x = newValue; break;
                case VecAxis.Y: value.y = newValue; break;
                case VecAxis.Z: value.z = newValue; break;

                default: throw new ArgumentOutOfRangeException(nameof(axis), axis, null);
            }
        }

        public static Vector3 With(this Vector3 value, VecAxis axis, float newValue) {
            value.Set(axis, newValue);
            return value;
        }

        public static Vector3 Div(this Vector3 value, Vector3 div) {
            return new(
                value.x / div.x
              , value.y / div.y
              , value.z / div.z);
        }

        public static Vector2Int YX(this Vector2Int value) {
            return new(value.y, value.x);
        }

        public static Vector2 YX(this Vector2 value) {
            return new(value.y, value.x);
        }

        public static void SwapXY(ref this Vector2 value) => (value.x, value.y) = (value.y, value.x);

        /// <summary>
        /// Offsets every vertex of a closed path in place so each edge moves outward by <see cref="padding"/>,
        /// keeping the edge parallel to the original one.<br/>
        /// Vertices must be in clockwise order (counter-clockwise shrinks the path instead).
        /// </summary>
        public static void ExpandPath(this IList<Vector2> path, float padding) {
            var pathPadded = ListPool<Vector2>.Get();

            try {
                for (var i = 0; i < path.Count; ++i) {
                    var vertex     = path[i];
                    var prev       = path[(i + path.Count - 1) % path.Count];
                    var next       = path[(i + 1) % path.Count];
                    var normalPrev = new Vector2(prev.y - vertex.y, vertex.x - prev.x).normalized;
                    var normalNext = new Vector2(vertex.y - next.y, next.x - vertex.x).normalized;
                    var bisector   = (normalPrev + normalNext).normalized;

                    pathPadded.Add(vertex + bisector * (padding / Mathf.Max(Vector2.Dot(bisector, normalNext), .001f)));
                }

                for (var i = 0; i < path.Count; ++i) path[i] = pathPadded[i];
            } finally {
                ListPool<Vector2>.Release(pathPadded);
            }
        }
    }
}
