using System.Globalization;
using UnityEngine;

namespace RiseOn.Utils {
    public static class StringUtils {
        public static string ToUpperFirst(this string value) {
            return string.IsNullOrEmpty(value) ? value : char.ToUpperInvariant(value[0]) + value[1..];
        }

        #region Rich text tags

        // Bold, italic, size and color work wherever Unity renders rich text: the Console, uGUI Text and TextMeshPro.
        // The rest are TextMeshPro tags; the Console and uGUI Text print them as plain text.
        // Numbers go out in the invariant culture, since "1,5em" from a comma-decimal locale would break the tag.

        public static string Tag(this string value, string name) => $"<{name}>{value}</{name}>";
        public static string Tag(this string value, string name, string attribute) => $"<{name}={attribute}>{value}</{name}>";

        public static string TagBold(this string value) => value.Tag("b");
        public static string TagItalic(this string value) => value.Tag("i");
        public static string TagSize(this string value, float pixels) => value.Tag("size", Format(pixels));
        public static string TagSize(this string value, string size) => value.Tag("size", size);
        public static string TagColor(this string value, Color color) => value.Tag("color", "#" + ColorUtility.ToHtmlStringRGBA(color));
        public static string TagColor(this string value, string color) => value.Tag("color", color);

        public static string TagUnderline(this string value) => value.Tag("u");
        public static string TagStrikethrough(this string value) => value.Tag("s");
        public static string TagMark(this string value, Color color) => value.Tag("mark", "#" + ColorUtility.ToHtmlStringRGBA(color));
        public static string TagSuperscript(this string value) => value.Tag("sup");
        public static string TagSubscript(this string value) => value.Tag("sub");
        public static string TagUppercase(this string value) => value.Tag("uppercase");
        public static string TagLowercase(this string value) => value.Tag("lowercase");
        public static string TagSmallCaps(this string value) => value.Tag("smallcaps");
        public static string TagNoParse(this string value) => value.Tag("noparse");
        public static string TagNoBreak(this string value) => value.Tag("nobr");
        public static string TagLink(this string value, string id) => value.Tag("link", Quote(id));
        public static string TagFont(this string value, string fontAsset) => value.Tag("font", Quote(fontAsset));
        public static string TagStyle(this string value, string style) => value.Tag("style", Quote(style));
        public static string TagAlign(this string value, string alignment) => value.Tag("align", alignment);
        public static string TagCharSpacing(this string value, float em) => value.Tag("cspace", Format(em) + "em");
        public static string TagMonospace(this string value, float em) => value.Tag("mspace", Format(em) + "em");
        public static string TagVerticalOffset(this string value, float em) => value.Tag("voffset", Format(em) + "em");

        private static string Format(float number) => number.ToString(CultureInfo.InvariantCulture);
        private static string Quote(string text) => $"\"{text}\"";

        #endregion
    }
}
