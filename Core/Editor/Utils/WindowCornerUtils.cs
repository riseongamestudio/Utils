using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Rounds chosen corners of a borderless editor window, a drop-down's, by <see cref="SettingsSection.Radius"/>, as the fields and boxes in it round.<br/>
    /// The window is cut to the shape, and its root's border and content follow the curve.<br/>
    /// Only the corners asked for round, so two windows that meet along an edge round as one, where Windows' own rounding (DWM) takes all four or none; Windows 10 rounds the same.<br/>
    /// The cut keeps every pixel whose middle lies inside the curve, so the border shows whole and only the outermost pixels step.<br/>
    /// On another editor platform the window stays square (<see cref="IsSupported"/>).
    /// </summary>
    /// <example><code>
    /// WindowCornerUtils.Show(window, window.ShowPopup);
    /// WindowCornerUtils.Round(window, WindowCornerUtils.Corners.All);
    /// </code></example>
    public static class WindowCornerUtils {
        /// <summary>Which corners round.</summary>
        [Flags]
        public enum Corners {
            None        = 0,
            TopLeft     = 1,
            TopRight    = 2,
            BottomLeft  = 4,
            BottomRight = 8,
            Top         = TopLeft | TopRight,
            Bottom      = BottomLeft | BottomRight,
            All         = Top | Bottom,
        }

        private sealed class Shape {
            public IntPtr     Handle;
            public Corners    Corners;
            public bool       Hidden;
            public Vector2Int Size;
        }

        private static readonly Dictionary<EditorWindow, Shape> shapes = new();
        private static bool tracking;

        /// <summary>Whether this editor's windows can be cut: Windows'. Elsewhere <see cref="Round"/> leaves a window as it is, square and shown.</summary>
        public static bool IsSupported => Application.platform is RuntimePlatform.WindowsEditor;

        /// <summary>Runs <paramref name="show"/>, which opens <paramref name="window"/>, and keeps the native window it makes, for <see cref="Round"/>.</summary>
        public static void Show(EditorWindow window, Action show) {
            if (!IsSupported) {
                show();
                return;
            }

            var before = ThreadWindows();
            show();
            var made = ThreadWindows().FirstOrDefault(handle => !before.Contains(handle));
            if (made != IntPtr.Zero) shapes[window] = new Shape { Handle = made };
        }

        /// <summary>
        /// Rounds <paramref name="corners"/> of a window opened through <see cref="Show"/>, the others square; <paramref name="hidden"/> cuts it away whole, for a window that has nothing to show.<br/>
        /// The cut follows the window as it changes size.
        /// </summary>
        public static void Round(EditorWindow window, Corners corners, bool hidden = false) {
            if (!shapes.TryGetValue(window, out var shape)) return;

            shape.Corners = corners;
            shape.Hidden  = hidden;
            shape.Size    = default;

            var root = window.rootVisualElement;
            root.style.borderTopLeftRadius     = (corners & Corners.TopLeft) != 0 ? SettingsSection.Radius : 0;
            root.style.borderTopRightRadius    = (corners & Corners.TopRight) != 0 ? SettingsSection.Radius : 0;
            root.style.borderBottomLeftRadius  = (corners & Corners.BottomLeft) != 0 ? SettingsSection.Radius : 0;
            root.style.borderBottomRightRadius = (corners & Corners.BottomRight) != 0 ? SettingsSection.Radius : 0;
            root.style.overflow                = Overflow.Hidden;

            Apply(shape);
            if (tracking) return;

            tracking = true;
            EditorApplication.update += Update;
        }

        // The screen gives a window its new size a tick after the editor sets it, so the cut is made again whenever the size it has changed.
        private static void Update() {
            foreach (var (window, shape) in shapes.ToArray()) {
                if (window == null || !IsWindow(shape.Handle)) {
                    shapes.Remove(window);
                    continue;
                }

                Apply(shape);
            }

            if (shapes.Count > 0) return;

            tracking = false;
            EditorApplication.update -= Update;
        }

        private static void Apply(Shape shape) {
            if (!GetWindowRect(shape.Handle, out var rect)) return;

            var size = new Vector2Int(rect.Right - rect.Left, rect.Bottom - rect.Top);
            if (size == shape.Size) return;

            shape.Size = size;
            // The window owns the region from here, and frees it.
            SetWindowRgn(shape.Handle, shape.Hidden ? CreateRectRgn(0, 0, 0, 0) : Region(shape.Handle, size, shape.Corners), true);
        }

        // The window less the pixels whose middle lies outside the root's rounded corners: the border, a pixel inside the curve, shows whole,
        // and no pixel stays that the curve covers less than half of, where what lies behind the window would show through its smoothing.
        // In pixels at the window's own scale, as its monitor sets it.
        private static IntPtr Region(IntPtr handle, Vector2Int size, Corners corners) {
            var scale  = GetDpiForWindow(handle) / 96f;
            var radius = SettingsSection.Radius * (scale > 0 ? scale : 1);
            var reach  = Mathf.CeilToInt(radius);
            var region = CreateRectRgn(0, 0, size.x, size.y);
            for (var i = 0; i < reach; i++) {
                for (var j = 0; j < reach; j++) {
                    var dx = radius - (i + .5f);
                    var dy = radius - (j + .5f);
                    if (dx <= 0 || dy <= 0 || Mathf.Sqrt(dx * dx + dy * dy) < radius) continue;

                    if ((corners & Corners.TopLeft) != 0) Cut(i, j);
                    if ((corners & Corners.TopRight) != 0) Cut(size.x - 1 - i, j);
                    if ((corners & Corners.BottomLeft) != 0) Cut(i, size.y - 1 - j);
                    if ((corners & Corners.BottomRight) != 0) Cut(size.x - 1 - i, size.y - 1 - j);
                }
            }

            return region;

            void Cut(int x, int y) {
                var pixel = CreateRectRgn(x, y, x + 1, y + 1);
                CombineRgn(region, region, pixel, CombineDiff);
                DeleteObject(pixel);
            }
        }

        private static HashSet<IntPtr> ThreadWindows() {
            var found = new HashSet<IntPtr>();
            EnumThreadWindows(GetCurrentThreadId(), (handle, _) => {
                found.Add(handle);
                return true;
            }, IntPtr.Zero);
            return found;
        }

        private const int CombineDiff = 4;

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect {
            public int Left, Top, Right, Bottom;
        }

        private delegate bool EnumWindowsProc(IntPtr handle, IntPtr parameter);

        [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint threadId, EnumWindowsProc callback, IntPtr parameter);
        [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
        [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr handle);
        [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr handle, out NativeRect rect);
        [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr handle);
        [DllImport("user32.dll")] private static extern int SetWindowRgn(IntPtr handle, IntPtr region, bool redraw);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateRectRgn(int left, int top, int right, int bottom);
        [DllImport("gdi32.dll")] private static extern int CombineRgn(IntPtr destination, IntPtr first, IntPtr second, int mode);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr handle);
    }
}