namespace RiseOn.Utils {
    /// <summary>
    /// Helpers that work on a value of any type, taking the variables by ref.<br/>
    /// Extensions called on a struct variable itself are in <see cref="StructUtils"/>.
    /// </summary>
    public static class ValueUtils {
        public static void Swap<T>(ref T left, ref T right) {
            (left, right) = (right, left);
        }

        /// <summary>
        /// Sets <paramref name="target"/> to <paramref name="value"/> and returns what it held before, like C++ std::exchange.<br/>
        /// Takes any field or local of any type; a property cannot be passed by ref.
        /// </summary>
        public static T Exchange<T>(ref T target, T value) {
            var previous = target;
            target = value;
            return previous;
        }
    }
}
