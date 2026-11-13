using UnityEditor;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Hover for IMGUI controls in windows that do not repaint as the mouse moves.<br/>
    /// The Inspector and Project Settings send mouse moves but repaint only for clicks and keys, so a lit control would stay lit or stay dark.
    /// </summary>
    public static class HoverUtils {
        private static int hovered;

        /// <summary>
        /// Whether the mouse is over <paramref name="rect"/>, repainting the view as it moves onto or off it.<br/>
        /// Call it on every event, so the control keeps one id from layout to repaint.
        /// </summary>
        public static bool Track(Rect rect) {
            var id   = GUIUtility.GetControlID(FocusType.Passive);
            var type = Event.current.type;
            var over = type is not EventType.MouseLeaveWindow && rect.Contains(Event.current.mousePosition);
            if (type is EventType.MouseMove or EventType.MouseDrag or EventType.MouseLeaveWindow && over != (hovered == id)) {
                hovered = over ? id : 0;
                HandleUtility.Repaint();
            }

            return over;
        }
    }
}