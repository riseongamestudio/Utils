namespace RiseOn.Utils {
    public static class StringExtensions {
        public static string ToUpperFirst(this string value) {
            return string.IsNullOrEmpty(value) ? value : char.ToUpperInvariant(value[0]) + value[1..];
        }
    }
}
