using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Unity's own marks for the two mobile platforms, as its build profiles show them: where a page shows an Android value beside an iOS one,
    /// the mark stands for the word; a place naming one platform keeps the word.<br/>
    /// Each carries its platform's name as its tooltip.<br/>
    /// A mark takes the width of its glyph (<see cref="Width"/>), not of its icon, which leaves more room either side of one glyph than of
    /// the other: laid out by it, each mark sits as near its field as the other.
    /// </summary>
    /// <example><code>
    /// PlatformMarks.Draw(new Rect(rect.x, rect.y, PlatformMarks.Width(PlatformMarks.Android), rect.height), PlatformMarks.Android);
    /// </code></example>
    public static class PlatformMarks {
        /// <summary>A mark's icon's side, in points.</summary>
        public const float Size = 16;

        // Each icon's glyph, as the shares of its width before its first column of ink and before the end of its last.
        private static readonly Dictionary<Texture, (float From, float To)> glyphs = new();

        private static GUIContent android;
        private static GUIContent ios;

        public static GUIContent Android => android ??= Mark("BuildSettings.Android.Small", "Android");
        public static GUIContent IOS => ios ??= Mark("BuildSettings.iPhone.Small", "iOS");

        /// <summary>How wide <paramref name="mark"/> shows, in points: its glyph's width at <see cref="Size"/>.</summary>
        public static float Width(GUIContent mark) {
            var (from, to) = Glyph(mark.image);
            return (to - from) * Size;
        }

        /// <summary>The mark's glyph from the left of <paramref name="rect"/>, centered down it, its tooltip over the whole rect.</summary>
        public static void Draw(Rect rect, GUIContent mark) {
            if (Event.current.type is EventType.Repaint) {
                var icon = PixelUtils.Snap(new Rect(rect.x - Glyph(mark.image).From * Size, rect.center.y - Size / 2, Size, Size));
                GUI.DrawTexture(icon, mark.image, ScaleMode.ScaleToFit);
            }

            GUI.Label(rect, new GUIContent(string.Empty, mark.tooltip));
        }

        // Read back from the GPU, as Unity's icons are not readable, and without a render target, so it may run in the middle of a draw.
        private static (float From, float To) Glyph(Texture texture) {
            if (texture == null) return (0, 1);
            if (glyphs.TryGetValue(texture, out var glyph)) return glyph;

            glyph = (0, 1);
            var read = AsyncGPUReadback.Request(texture, 0, TextureFormat.RGBA32);
            read.WaitForCompletion();
            if (!read.hasError) {
                var pixels = read.GetData<Color32>();
                int width = texture.width, first = width, last = -1;
                for (var i = 0; i < pixels.Length; i++) {
                    if (pixels[i].a < 32) continue;

                    first = Mathf.Min(first, i % width);
                    last  = Mathf.Max(last, i % width);
                }

                if (last >= first) glyph = (first / (float) width, (last + 1) / (float) width);
            }

            glyphs[texture] = glyph;
            return glyph;
        }

        private static GUIContent Mark(string icon, string name) => new(EditorGUIUtility.IconContent(icon).image, name);
    }
}