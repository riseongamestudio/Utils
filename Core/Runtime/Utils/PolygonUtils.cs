using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace RiseOn.Utils {
    /// <summary>Operations on a closed polygon given as its vertices in order.</summary>
    public static class PolygonUtils {
        /// <summary>
        /// Offsets every vertex of a closed path in place so each edge moves outward by <paramref name="padding"/>, keeping the edge parallel to the original one.<br/>
        /// A negative padding shrinks it instead.<br/>
        /// Vertices must be in clockwise order; counter-clockwise ones turn the offset around.
        /// </summary>
        public static void Inflate(this IList<Vector2> path, float padding) {
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
