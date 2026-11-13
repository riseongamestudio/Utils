using UnityEditor;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Puts GUI coordinates on the screen's pixel grid.<br/>
    /// Two rects that meet then neither leave a gap nor overlap by a pixel at 125% or 150%, and a one-pixel line stays sharp.
    /// </summary>
    public static class PixelUtils {
        /// <summary>One screen pixel, in GUI points: the width of a hairline.</summary>
        public static float Pixel => 1 / EditorGUIUtility.pixelsPerPoint;

        /// <summary>The nearest point on the pixel grid.</summary>
        public static float Snap(float value) {
            var ppp = EditorGUIUtility.pixelsPerPoint;
            return Mathf.Round(value * ppp) / ppp;
        }

        /// <summary>The rect with each edge on the pixel grid.</summary>
        public static Rect Snap(Rect rect) {
            var xMin = Snap(rect.xMin);
            var yMin = Snap(rect.yMin);
            return new Rect(xMin, yMin, Snap(rect.xMax) - xMin, Snap(rect.yMax) - yMin);
        }
    }
}