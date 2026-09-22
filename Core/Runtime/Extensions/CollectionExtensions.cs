using System.Collections.Generic;
using UnityEngine;

namespace RiseOn.Utils {
    public static class CollectionExtensions {
        public static ref T ItemAt<T>(this T[,] list, Vector2Int id) {
            return ref list[id.x, id.y];
        }

        public static T PopFront<T>(this IList<T> list) {
            var result = list[^1];
            list.RemoveAt(list.Count - 1);
            return result;
        }

        public static bool IsEmpty<T>(this IReadOnlyCollection<T> list) {
            return list.Count == 0;
        }

        public static IEnumerable<Vector2Int> IEIndex2D<T>(this T[,] list) {
            return MathHelper.IEIndex2D(
                0, list.GetLength(0)
              , 0, list.GetLength(1));
        }
    }
}
