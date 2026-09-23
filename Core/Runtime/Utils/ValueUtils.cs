using System.Collections.Generic;

namespace RiseOn.Utils {
    /// <summary>Helpers that work on a value of any type.</summary>
    public static class ValueUtils {
        public static void Swap<T>(ref T left, ref T right) {
            (left, right) = (right, left);
        }

        /// <summary>
        /// Raises <paramref name="target"/> to <paramref name="value"/> when it is lower, like target = max(target, value).<br/>
        /// Works on anything <see cref="Comparer{T}.Default"/> can order: numbers, enums, IComparable structs; any other type throws.<br/>
        /// Returns true when the target changed.
        /// </summary>
        public static bool RaiseTo<T>(ref this T target, T value) where T : struct {
            if (Comparer<T>.Default.Compare(target, value) >= 0) return false;

            target = value;
            return true;
        }

        /// <summary>
        /// Lowers <paramref name="target"/> to <paramref name="value"/> when it is higher, like target = min(target, value).<br/>
        /// Works on anything <see cref="Comparer{T}.Default"/> can order: numbers, enums, IComparable structs; any other type throws.<br/>
        /// Returns true when the target changed.
        /// </summary>
        public static bool LowerTo<T>(ref this T target, T value) where T : struct {
            if (Comparer<T>.Default.Compare(target, value) <= 0) return false;

            target = value;
            return true;
        }
    }
}
