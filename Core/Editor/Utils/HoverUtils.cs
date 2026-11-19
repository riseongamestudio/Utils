using System.Collections.Generic;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Hover for IMGUI controls: the view is asked for mouse moves and repainted as the mouse moves onto or off a control.<br/>
    /// Unity sends a view mouse moves only when it asks for them, and Project Settings does not: its controls see the mouse only at a click
    /// or a scroll, and its repaints keep where they saw it last (measured with Unity 6.3), so a lit control would stay lit or stay dark.
    /// </summary>
    public static class HoverUtils {
        // Every control under the mouse, as rects inside one another, such as an item and the item of a list inside it, are under it together.
        private static readonly HashSet<int> hovered = new();

        /// <summary>
        /// Whether the mouse is over <paramref name="rect"/>, repainting the view as it moves onto or off it.<br/>
        /// Call it on every event, so the control keeps one id from layout to repaint.
        /// </summary>
        public static bool Track(Rect rect) {
            var window = GUIHelper.CurrentWindow;
            if (window && !window.wantsMouseMove) window.wantsMouseMove = true;

            var id   = GUIUtility.GetControlID(FocusType.Passive);
            var type = Event.current.type;
            var over = type is not EventType.MouseLeaveWindow && rect.Contains(Event.current.mousePosition);
            if (type is EventType.MouseMove or EventType.MouseDrag or EventType.MouseLeaveWindow && over != hovered.Contains(id)) {
                if (over) hovered.Add(id);
                else hovered.Remove(id);
                HandleUtility.Repaint();
            }

            return over;
        }
    }
}