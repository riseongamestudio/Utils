using UnityEditor;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Puts GUI coordinates on the screen's pixel grid.<br/>
    /// Two rects that meet then neither leave a gap nor overlap by a pixel at 125% or 150%, and a one-pixel line stays sharp.<br/>
    /// A position is put on the window's grid, wherever the group drawing it starts: the same rect lands on the same pixels inside a list, a fade or a scroll view as outside.
    /// </summary>
    public static class PixelUtils {
        /// <summary>One screen pixel, in GUI points: the width of a hairline.</summary>
        public static float Pixel => 1 / EditorGUIUtility.pixelsPerPoint;

        /// <summary>A length, such as a padding, in whole pixels.</summary>
        public static float Snap(float value) {
            var ppp = EditorGUIUtility.pixelsPerPoint;
            return Mathf.Round(value * ppp) / ppp;
        }

        /// <summary>An x on the window's pixel grid.</summary>
        public static float SnapX(float x) {
            var origin = Origin().x;
            return Snap(x + origin) - origin;
        }

        /// <summary>A y on the window's pixel grid.</summary>
        public static float SnapY(float y) {
            var origin = Origin().y;
            return Snap(y + origin) - origin;
        }

        /// <summary>The rect with each edge on the window's pixel grid.</summary>
        public static Rect Snap(Rect rect) {
            var xMin = SnapX(rect.xMin);
            var yMin = SnapY(rect.yMin);
            return new Rect(xMin, yMin, SnapX(rect.xMax) - xMin, SnapY(rect.yMax) - yMin);
        }

        /// <summary>
        /// Where the group being drawn starts, on the screen: a group such as a fade or a list can start between two pixels, while a window starts on one,<br/>
        /// so a position is put on the grid with this added, and taken off after.
        /// </summary>
        public static Vector2 Origin() => GUIUtility.GUIToScreenPoint(Vector2.zero);
    }
}