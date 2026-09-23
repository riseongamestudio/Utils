using UnityEngine;

namespace RiseOn.Utils {
    /// <summary>
    /// The same helpers on every vector type: With replaces one component, Mul and Div work component by component.<br/>
    /// Mul and Div exist on all of them even where Unity has an operator for one type and not the next, so the call looks the same everywhere.<br/>
    /// The Int types also take a float vector and return one, keeping the fraction.<br/>
    /// Swizzles such as <c>ZYX</c> live in VectorUtils.Swizzle.cs.<br/>
    /// An index counts like the vector's own indexer (0 is x, 1 y, 2 z, 3 w); any other index throws, as the indexer does.
    /// </summary>
    public static partial class VectorUtils {
        #region Vector2

        public static Vector2 With(this Vector2 value, int index, float newValue) {
            value[index] = newValue;
            return value;
        }

        public static Vector2 Mul(this Vector2 value, Vector2 other) {
            return new(value.x * other.x, value.y * other.y);
        }

        public static Vector2 Div(this Vector2 value, Vector2 other) {
            return new(value.x / other.x, value.y / other.y);
        }

        public static void SwapXY(ref this Vector2 value) {
            (value.x, value.y) = (value.y, value.x);
        }

        #endregion

        #region Vector3

        public static Vector3 With(this Vector3 value, int index, float newValue) {
            value[index] = newValue;
            return value;
        }

        public static Vector3 Mul(this Vector3 value, Vector3 other) {
            return new(value.x * other.x, value.y * other.y, value.z * other.z);
        }

        public static Vector3 Div(this Vector3 value, Vector3 other) {
            return new(value.x / other.x, value.y / other.y, value.z / other.z);
        }

        #endregion

        #region Vector4

        public static Vector4 With(this Vector4 value, int index, float newValue) {
            value[index] = newValue;
            return value;
        }

        public static Vector4 Mul(this Vector4 value, Vector4 other) {
            return new(value.x * other.x, value.y * other.y, value.z * other.z, value.w * other.w);
        }

        public static Vector4 Div(this Vector4 value, Vector4 other) {
            return new(value.x / other.x, value.y / other.y, value.z / other.z, value.w / other.w);
        }

        #endregion

        #region Vector2Int

        public static Vector2Int With(this Vector2Int value, int index, int newValue) {
            value[index] = newValue;
            return value;
        }

        public static Vector2Int Mul(this Vector2Int value, Vector2Int other) {
            return new(value.x * other.x, value.y * other.y);
        }

        /// <summary>Integer division per component: rounds toward zero, and a zero component in <paramref name="other"/> throws.</summary>
        public static Vector2Int Div(this Vector2Int value, Vector2Int other) {
            return new(value.x / other.x, value.y / other.y);
        }

        public static Vector2 Mul(this Vector2Int value, Vector2 other) {
            return new(value.x * other.x, value.y * other.y);
        }

        /// <summary>Float division per component, for when the result should keep its fraction.</summary>
        public static Vector2 Div(this Vector2Int value, Vector2 other) {
            return new(value.x / other.x, value.y / other.y);
        }

        public static void SwapXY(ref this Vector2Int value) {
            (value.x, value.y) = (value.y, value.x);
        }

        #endregion

        #region Vector3Int

        public static Vector3Int With(this Vector3Int value, int index, int newValue) {
            value[index] = newValue;
            return value;
        }

        public static Vector3Int Mul(this Vector3Int value, Vector3Int other) {
            return new(value.x * other.x, value.y * other.y, value.z * other.z);
        }

        /// <summary>Integer division per component: rounds toward zero, and a zero component in <paramref name="other"/> throws.</summary>
        public static Vector3Int Div(this Vector3Int value, Vector3Int other) {
            return new(value.x / other.x, value.y / other.y, value.z / other.z);
        }

        public static Vector3 Mul(this Vector3Int value, Vector3 other) {
            return new(value.x * other.x, value.y * other.y, value.z * other.z);
        }

        /// <summary>Float division per component, for when the result should keep its fraction.</summary>
        public static Vector3 Div(this Vector3Int value, Vector3 other) {
            return new(value.x / other.x, value.y / other.y, value.z / other.z);
        }

        #endregion
    }
}
