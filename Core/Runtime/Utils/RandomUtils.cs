using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RiseOn.Utils {
    public static class RandomUtils {
        public static float RandomInside(this Vector2 range) {
            if (range.x > range.y) range.SwapXY();
            return Random.Range(range.x, range.y);
        }

        public static Vector2 RandomInside(this BoxCollider2D range) {
            return range.transform.TransformPoint(range.offset + range.size / 2 - new Vector2(
                range.size.x * Random.value
              , range.size.y * Random.value));
        }

        /// <summary>
        /// A random item, each equally likely; throws when there is none.<br/>
        /// Takes any sequence: a list is indexed directly in O(1), a plain sequence is walked twice, once to count it and once to reach the pick.<br/>
        /// Two walks measured faster than sampling in one walk (reservoir), which needs a random number per item.
        /// </summary>
        public static T RandomInside<T>(this IEnumerable<T> source) {
            switch (source) {
                case IList<T> list when list.Count > 0:         return list[Random.Range(0, list.Count)];
                case IReadOnlyList<T> list when list.Count > 0: return list[Random.Range(0, list.Count)];
            }

            var count = source.Count();
            if (count == 0) throw new System.InvalidOperationException("The sequence is empty.");

            return source.ElementAt(Random.Range(0, count));
        }

        public static float RandomSign(this float value) {
            if (Random.value > .5f) value *= -1;
            return value;
        }

        public static float RandomSign(this int value) {
            if (Random.value > .5f) value *= -1;
            return value;
        }

        public static Vector2 RandomInCircle(this Vector2 center, float radius) {
            var angle = Random.value * 2 * Mathf.PI;
            return center + Mathf.Sqrt(Random.value) * radius * new Vector2(
                Mathf.Cos(angle)
              , Mathf.Sin(angle));
        }
    }
}
