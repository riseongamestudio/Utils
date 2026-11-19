using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// The native side of a search window opened as a drop-down, a borderless window, on Windows: <see cref="Show"/> opens it rounded by
    /// <see cref="Section.Radius"/>, as the fields and boxes in it round, <see cref="Square"/> squares the corners along the edge where its
    /// controls and its list meet, and <see cref="KeepOnTop"/> keeps it over the other windows.<br/>
    /// The window is cut to the shape, and its root's background and border follow the curve. UI Toolkit draws an element's border under
    /// its children, so a window whose content reaches into a corner draws its edge in a last child over the content (SearchWindow does).<br/>
    /// Squared corners let two windows that meet round as one, where Windows' own rounding (DWM) takes all four or none; Windows 10 rounds the same.<br/>
    /// The cut keeps the pixels the curve covers three quarters of or more, so the corner looks as round as a smoothed one of the same radius.<br/>
    /// On another editor platform the window stays square (<see cref="IsSupported"/>).
    /// </summary>
    /// <example><code>
    /// BorderlessWindow.Show(window, window.ShowPopup);
    /// BorderlessWindow.Square(window, BorderlessWindow.Corners.Bottom);
    /// </code></example>
    internal static class BorderlessWindow {
        /// <summary>Which corners.</summary>
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
            public Corners    Square;
            public bool       Hidden;
            public Vector2Int Size;
        }

        private static readonly Dictionary<EditorWindow, Shape> shapes = new();
        private static bool tracking;

        /// <summary>Whether this editor's windows can be cut: Windows'. Elsewhere a window stays as it is, square and shown.</summary>
        public static bool IsSupported => Application.platform is RuntimePlatform.WindowsEditor;

        /// <summary>Runs <paramref name="show"/>, which opens <paramref name="window"/>, and rounds the native window it makes, all four corners.</summary>
        public static void Show(EditorWindow window, Action show) {
            if (!IsSupported) {
                show();
                return;
            }

            // A window can show another as it shows, as a drop-down's controls show its list: that one's native window, kept by its own
            // call, is new here too.
            var before = ThreadWindows();
            show();
            var made = ThreadWindows().FirstOrDefault(handle => !before.Contains(handle) && shapes.Values.All(shape => shape.Handle != handle));
            if (made == IntPtr.Zero) return;

            shapes[window] = new Shape { Handle = made };
            Square(window, Corners.None);
        }

        /// <summary>
        /// Makes a window opened through <see cref="Show"/> a topmost window of Windows, over every window that is not, another
        /// application's too, as a menu is: a click on another editor window raises that one at once, and Unity raises a popup back only a
        /// frame later, so a drop-down rolling up after the click would blink out. Where <see cref="IsSupported"/> is false it does nothing.
        /// </summary>
        public static void KeepOnTop(EditorWindow window) {
            if (!shapes.TryGetValue(window, out var shape)) return;

            SetWindowPos(shape.Handle, TopMost, 0, 0, 0, 0, NoSize | NoMove | NoActivate);
        }

        /// <summary>
        /// Squares <paramref name="corners"/> of a window opened through <see cref="Show"/>, those along an edge another window meets, the
        /// others round; <see cref="Corners.None"/> rounds all four again. <paramref name="hidden"/> cuts it away whole, for a window that has nothing to show.<br/>
        /// The cut follows the window as it changes size.
        /// </summary>
        public static void Square(EditorWindow window, Corners corners, bool hidden = false) {
            if (!shapes.TryGetValue(window, out var shape)) return;

            shape.Square = corners;
            shape.Hidden  = hidden;
            shape.Size    = default;

            // UI Toolkit draws a rounded border about half a point inside the curve of its radius, where GUI.DrawTexture, which draws the
            // page's boxes, puts it on the curve: half a point more lays the border on the boxes' curve, the one the cut follows.
            var root   = window.rootVisualElement;
            var radius = Section.Radius + .5f;
            root.style.borderTopLeftRadius     = (corners & Corners.TopLeft) != 0 ? 0 : radius;
            root.style.borderTopRightRadius    = (corners & Corners.TopRight) != 0 ? 0 : radius;
            root.style.borderBottomLeftRadius  = (corners & Corners.BottomLeft) != 0 ? 0 : radius;
            root.style.borderBottomRightRadius = (corners & Corners.BottomRight) != 0 ? 0 : radius;

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
            SetWindowRgn(shape.Handle, shape.Hidden ? CreateRectRgn(0, 0, 0, 0) : Region(shape.Handle, size, shape.Square), true);
        }

        // The window less the pixels the root's rounded corners cover less than three quarters of. A window keeps or drops a pixel
        // whole, where smoothing shows a pixel the curve half covers half lit; kept whole, such a pixel squares the corner off, so a
        // corner cut at the middle of its pixels looks smaller than a smoothed one of the same radius. In pixels at the window's own
        // scale, as its monitor sets it.
        private static IntPtr Region(IntPtr handle, Vector2Int size, Corners square) {
            const float Kept = .75f;
            var scale  = GetDpiForWindow(handle) / 96f;
            var radius = Section.Radius * (scale > 0 ? scale : 1);
            var reach  = Mathf.CeilToInt(radius);
            var region = CreateRectRgn(0, 0, size.x, size.y);
            for (var i = 0; i < reach; i++) {
                for (var j = 0; j < reach; j++) {
                    if (Covered(radius, i, j) >= Kept) continue;

                    if ((square & Corners.TopLeft) == 0) Cut(i, j);
                    if ((square & Corners.TopRight) == 0) Cut(size.x - 1 - i, j);
                    if ((square & Corners.BottomLeft) == 0) Cut(i, size.y - 1 - j);
                    if ((square & Corners.BottomRight) == 0) Cut(size.x - 1 - i, size.y - 1 - j);
                }
            }

            return region;

            void Cut(int x, int y) {
                var pixel = CreateRectRgn(x, y, x + 1, y + 1);
                CombineRgn(region, region, pixel, CombineDiff);
                DeleteObject(pixel);
            }
        }

        // How much of the corner pixel at (i, j) lies inside a curve of the radius around (radius, radius), from 8 by 8 samples.
        private static float Covered(float radius, int i, int j) {
            const int Samples = 8;
            var inside = 0;
            for (var a = 0; a < Samples; a++) {
                for (var b = 0; b < Samples; b++) {
                    var dx = radius - (i + (a + .5f) / Samples);
                    var dy = radius - (j + (b + .5f) / Samples);
                    if (dx <= 0 || dy <= 0 || dx * dx + dy * dy <= radius * radius) inside++;
                }
            }

            return inside / (float) (Samples * Samples);
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

        private static readonly IntPtr TopMost = new(-1);
        private const uint NoSize     = 0x1;
        private const uint NoMove     = 0x2;
        private const uint NoActivate = 0x10;

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
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr handle, IntPtr after, int x, int y, int width, int height, uint flags);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateRectRgn(int left, int top, int right, int bottom);
        [DllImport("gdi32.dll")] private static extern int CombineRgn(IntPtr destination, IntPtr first, IntPtr second, int mode);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr handle);
    }
}