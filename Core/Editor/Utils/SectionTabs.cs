using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Tabs across an unpadded <see cref="Section"/>, drawn as Unity draws a settings page's platform tabs: the selected tab in the page's color, opening into it, the others darker and closed off by a line.<br/>
    /// A tab not selected lights under the mouse; the selected one stays as it is, as Unity's do.<br/>
    /// A switch slides the pages sideways and eases the height between them, as Odin's tab groups do, repainting every frame of it on any window.<br/>
    /// The pages meet the tabs and the section's border: no margin above, below or inside them.
    /// </summary>
    /// <example><code>
    /// tabs ??= new SectionTabs(new[] { "Android", "iOS" }, 0);
    /// tabs.Draw(page => DrawPlatform(page));
    /// </code></example>
    public sealed class SectionTabs {
        // Material's medium duration: a settings window repaints at about 20 frames a second, too few for Odin's 0.13 s slide to show.
        private const float Duration = 0.25f;

        private static GUIStyle label;

        private readonly string[] labels;
        private readonly float[] heights;
        private readonly bool[]  lit;

        // The tabs' width at the last repaint: a slide lays its pages out at it, the same in every event of a frame.
        private float width = 1;
        private int previous  = -1;
        private int requested = -1;
        private double started = -1;
        private float progress = 1;

        /// <summary>The selected tab, the one a slide ends on.</summary>
        public int Current { get; private set; }

        public SectionTabs(IReadOnlyList<string> labels, int current) {
            this.labels = labels.ToArray();
            heights     = new float[this.labels.Length];
            lit         = new bool[this.labels.Length];
            Current     = current;
        }

        /// <summary>
        /// Draws the tabs and the selected page, or both pages while one slides out for the other.<br/>
        /// <paramref name="drawPage"/> draws, with layout, the page at the index it gets.
        /// </summary>
        public void Draw(Action<int> drawPage) {
            // The state changes only at layout, so every event of a frame lays out the same controls.
            if (Event.current.type is EventType.Layout) Advance();

            var row = LayoutTabs();
            if (previous < 0) DrawPage(Current, drawPage);
            else DrawSlide(drawPage);

            if (Event.current.type is EventType.Repaint) PaintTabs(row);
        }

        private void DrawSlide(Action<int> drawPage) {
            HandleUtility.Repaint();
            if (Event.current.type is EventType.Repaint && started < 0) started = EditorApplication.timeSinceStartup;
            // Ease-out quad: at the window's few frames a cubic covers most of the way in the first one and reads as a jump.
            var eased  = 1 - (1 - progress) * (1 - progress);
            var target = heights[Current] > 0 ? heights[Current] : heights[previous];
            var height = Mathf.Lerp(heights[previous], target, eased);

            // Both pages side by side in a scroll view without bars, which clips them and lays them out as any other
            // controls; the scroll moves from one page to the other. The page on the left is the lower tab's.
            var forward = Current > previous;
            var scroll  = new Vector2((forward ? eased : 1 - eased) * width, 0);
            GUILayout.BeginScrollView(scroll, false, false, GUIStyle.none, GUIStyle.none, GUIStyle.none, GUILayout.Height(height));
            GUILayout.BeginHorizontal(GUILayout.Width(width * 2));
            DrawSliding(forward ? previous : Current, drawPage);
            DrawSliding(forward ? Current : previous, drawPage);
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
        }

        private void Advance() {
            if (requested >= 0) {
                previous  = Current;
                Current   = requested;
                requested = -1;
                started   = -1;
                progress  = 0;
            }

            // The clock starts with the first frame shown: a window may repaint well after the click, and the slide would be over unseen.
            if (previous < 0 || started < 0) return;

            progress = Mathf.Clamp01((float) ((EditorApplication.timeSinceStartup - started) / Duration));
            if (progress >= 1) previous = -1;
        }

        // The others' darkening, from the tabs of Unity's Player page: 53 beside a page of 65; half of it under the mouse.
        private static readonly Color shade = new(0, 0, 0, 0.27f);

        private int Selected => requested >= 0 ? requested : Current;

        // The tabs' row and their clicks; what is lit is kept for the paint.
        private Rect LayoutTabs() {
            // As tall as the section's bar shows less the line under it, in whole points as the rest of the layout.
            var row = GUILayoutUtility.GetRect(0, Section.Height - 1, GUILayout.ExpandWidth(true));
            if (Event.current.type is EventType.Repaint) width = row.width;

            for (var i = 0; i < labels.Length; i++) {
                var cell = Cell(row, i);
                lit[i] = HoverUtils.Track(cell);
                if (Event.current.type is not EventType.MouseDown || Event.current.button is not 0 || !cell.Contains(Event.current.mousePosition)) continue;

                if (i != Selected) requested = i;
                Event.current.Use();
                HandleUtility.Repaint();
            }

            return row;
        }

        // Edges on the pixel grid, so neighboring tabs neither overlap nor leave a gap.
        private Rect Cell(Rect row, int index) {
            var x0 = PixelUtils.SnapX(row.x + row.width * index / labels.Length);
            var x1 = PixelUtils.SnapX(row.x + row.width * (index + 1) / labels.Length);
            return PixelUtils.Snap(new Rect(x0, row.y, x1 - x0, row.height));
        }

        private void PaintTabs(Rect row) {
            label ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
            for (var i = 0; i < labels.Length; i++) DrawTab(Cell(row, i), labels[i], i == Selected, lit[i], i < labels.Length - 1);
        }

        // The selected tab paints nothing, so it is the color of the box under it, as its page is: it opens onto the page whatever skin or content.
        // The others shade it.
        private static void DrawTab(Rect cell, string text, bool on, bool lit, bool divided) {
            if (!on) EditorGUI.DrawRect(cell, lit ? new Color(0, 0, 0, shade.a / 2) : shade);

            // The others close off from the page; a line parts each tab from the next.
            var line = SirenixGUIStyles.BorderColor;
            var pixel = PixelUtils.Pixel;
            if (!on) EditorGUI.DrawRect(new Rect(cell.x, cell.yMax - pixel, cell.width, pixel), line);
            if (divided) EditorGUI.DrawRect(new Rect(cell.xMax - pixel, cell.y, pixel, cell.height), line);

            label.Draw(cell, new GUIContent(text), false, false, false, false);
        }

        private void DrawPage(int index, Action<int> drawPage) {
            var repaint = Event.current.type is EventType.Repaint;
            var page    = EditorGUILayout.BeginVertical();
            drawPage(index);
            EditorGUILayout.EndVertical();
            if (repaint) heights[index] = page.height;
        }

        // A page as wide as the tabs, at the top of the scroll view, as tall as it needs.
        private void DrawSliding(int index, Action<int> drawPage) {
            GUILayout.BeginVertical(GUILayout.Width(width));
            DrawPage(index, drawPage);
            GUILayout.EndVertical();
        }
    }
}