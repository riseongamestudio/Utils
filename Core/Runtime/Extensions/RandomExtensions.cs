using System.Collections.Generic;
using UnityEngine;

namespace RiseOn.Utils {
    public static class RandomExtensions {
        public static float RandomInside(this Vector2 range) {
            if (range.x > range.y) range.SwapXY();
            return Random.Range(range.x, range.y);
        }

        public static Vector2 RandomInside(this BoxCollider2D range) {
            return range.transform.TransformPoint(range.offset + range.size / 2 - new Vector2(
                range.size.x * Random.value
              , range.size.y * Random.value));
        }

        public static T RandomInside<T>(this IList<T> list) {
            return list[Random.Range(0, list.Count)];
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
