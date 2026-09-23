using UnityEngine;

namespace RiseOn.Utils {
    /// <summary>
    /// Copies of a color with one channel replaced, as VectorUtils.With does for vectors.<br/>
    /// An index counts like the color's own indexer: 0 is r, 1 g, 2 b, 3 a; any other index throws, as the indexer does.
    /// </summary>
    public static class ColorUtils {
        #region Color

        public static Color With(this Color color, int index, float value) {
            color[index] = value;
            return color;
        }

        public static Color WithR(this Color color, float r) {
            color.r = r;
            return color;
        }

        public static Color WithG(this Color color, float g) {
            color.g = g;
            return color;
        }

        public static Color WithB(this Color color, float b) {
            color.b = b;
            return color;
        }

        public static Color WithA(this Color color, float a) {
            color.a = a;
            return color;
        }

        #endregion

        #region Color32

        public static Color32 With(this Color32 color, int index, byte value) {
            color[index] = value;
            return color;
        }

        public static Color32 WithR(this Color32 color, byte r) {
            color.r = r;
            return color;
        }

        public static Color32 WithG(this Color32 color, byte g) {
            color.g = g;
            return color;
        }

        public static Color32 WithB(this Color32 color, byte b) {
            color.b = b;
            return color;
        }

        public static Color32 WithA(this Color32 color, byte a) {
            color.a = a;
            return color;
        }

        #endregion
    }
}
