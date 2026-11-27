using System.Collections.Generic;
using System.Linq;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine.UIElements;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Draws the window the GUI is drawing in again on the next editor tick, whatever hosts that GUI: for an animation, a spinner or a hover that moves on by itself.<br/>
    /// HandleUtility.Repaint and Odin's GUIHelper.RequestRepaint reach a plain IMGUI window, but a window built with UI Toolkit, such as Project Settings, draws its IMGUI
    /// content again only once that content is marked dirty; asked only those ways, an animation there stood still until the mouse moved over it.
    /// </summary>
    /// <example><code>
    /// if (progress &lt; 1) RepaintUtils.Request();
    /// </code></example>
    public static class RepaintUtils {
        private static readonly HashSet<EditorWindow> pending = new();

        /// <summary>Call it in every draw while the thing moves; one request a tick reaches the window however many ask.</summary>
        public static void Request() {
            var window = GUIHelper.CurrentWindow;
            if (!window) {
                HandleUtility.Repaint();
                return;
            }

            if (pending.Add(window) && pending.Count == 1) EditorApplication.update += Tick;
        }

        private static void Tick() {
            EditorApplication.update -= Tick;
            var windows = pending.ToArray();
            pending.Clear();
            foreach (var window in windows) {
                if (!window) continue;

                window.rootVisualElement.Query<IMGUIContainer>().ForEach(container => container.MarkDirtyRepaint());
                window.Repaint();
            }
        }
    }
}