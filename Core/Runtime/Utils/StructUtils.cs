using System.Collections.Generic;

namespace RiseOn.Utils {
    /// <summary>
    /// Extensions that change a struct in place, called on the variable itself: <c>height.RaiseTo(next)</c>.<br/>
    /// Only structs can take a ref receiver; <see cref="ValueUtils"/> has the static forms for any type.
    /// </summary>
    public static class StructUtils {
        /// <summary>
        /// Sets <paramref name="target"/> to <paramref name="value"/> and hands back what it held before, like C++ std::exchange.<br/>
        /// For a reference type, use <see cref="ValueUtils.Exchange{T}"/>.
        /// </summary>
        public static void Exchange<T>(this ref T target, T value, out T previous) where T : struct {
            previous = target;
            target   = value;
        }

        /// <summary>
        /// Raises <paramref name="target"/> to <paramref name="value"/> when it is lower, like target = max(target, value).<br/>
        /// Works on anything <see cref="Comparer{T}.Default"/> can order: numbers, enums, IComparable structs; any other type throws.<br/>
        /// Returns true when the target changed.
        /// </summary>
        public static bool RaiseTo<T>(this ref T target, T value) where T : struct {
            if (Comparer<T>.Default.Compare(target, value) >= 0) return false;

            target = value;
            return true;
        }

        /// <summary>
        /// Lowers <paramref name="target"/> to <paramref name="value"/> when it is higher, like target = min(target, value).<br/>
        /// Works on anything <see cref="Comparer{T}.Default"/> can order: numbers, enums, IComparable structs; any other type throws.<br/>
        /// Returns true when the target changed.
        /// </summary>
        public static bool LowerTo<T>(this ref T target, T value) where T : struct {
            if (Comparer<T>.Default.Compare(target, value) <= 0) return false;

            target = value;
            return true;
        }
    }
}
