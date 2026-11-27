using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// A section of a settings page, drawn as Odin draws its groups: a rounded box in Odin's colors under a header bar.<br/>
    /// The bar's tooltip carries the explanation, and its right end holds buttons from <see cref="Button"/>.<br/>
    /// <c>Foldout</c> makes one the bar folds, with Odin's fade: only for a section worth putting away, long or seldom used.<br/>
    /// Draw on the bar, its buttons and <see cref="BarValue"/>, first, then the content inside the scope while <see cref="Open"/>: a folding section's content begins, in its fade, where <see cref="Open"/> is first read.<br/>
    /// <see cref="RowButton"/> and <see cref="RowHandle"/> line up with the bar, for the rows of <see cref="SectionRows"/>.
    /// </summary>
    /// <example><code>
    /// using (var section = new Section("Items", "What the game sells.", buttons: 1, padded: false)) {
    ///     if (section.Button(0, SdfIconType.Plus, "Add an item")) Add();
    ///     DrawItems();
    /// }
    ///
    /// using (var section = Section.Foldout("MyPack.Testing", "Testing", open: false)) {
    ///     if (section.Open) DrawTesting();
    /// }
    /// </code></example>
    public sealed class Section : IDisposable {
        /// <summary>The bar's height.</summary>
        public const float Height = 22;

        /// <summary>The side of a button on the bar or on a row, and of a row's drag handle: a square, a pixel short of the bar above and below.</summary>
        public const float ButtonWidth = Height - 2;

        /// <summary>The space between the border and padded content, left and right; above and below it is <see cref="RowPadding"/>.</summary>
        public const int Inset = 4;

        /// <summary>
        /// Unity's indent for one level: content folded under a row's foldout starts this far in, under the foldout's text.<br/>
        /// Narrow the label column by as much, so the values stay in line with the page's.
        /// </summary>
        public const float Indent = 15;

        /// <summary>
        /// The space Unity leaves between a label column and its field (EditorGUI.PrefixLabel's): a field a row lays out by hand starts this far past the label column, in line with the page's.
        /// </summary>
        public const float LabelGap = 2;

        /// <summary>The size an icon from <see cref="IconButton"/> draws at, wherever it sits.</summary>
        public const float IconSize = 12;

        private static GUIStyle cutLabel;

        /// <summary>
        /// A label on one line, cut short with an ellipsis where its rect is narrower than its text, as Unity's own labels are: for text a row
        /// lays out by hand, which GUI.Label's default style wraps onto a second line, past the row.<br/>
        /// Its CalcSize gives its padding only; measure the text with EditorStyles.label.
        /// </summary>
        public static GUIStyle CutLabel => cutLabel ??= new GUIStyle(EditorStyles.label) { clipping = TextClipping.Ellipsis };

        /// <summary>The corners' radius: Unity's controls' own (--unity-metrics-default-border_radius), so a box rounds as the fields in it do.</summary>
        public const float Radius = 3;

        private static readonly GUIStyle padding = new() { padding = new RectOffset(Inset, Inset, 0, 0) };

        // A control's margin above and below, so a section stands as far from what is next to it as two fields stand apart.
        private static readonly GUIStyle outer = new() { margin = new RectOffset(0, 0, 2, 2) };

        /// <summary>
        /// Whether the content draws now: always for a section that does not fold, else while open or folding shut.<br/>
        /// The first read begins a folding section's content: what is drawn after it fades and clips with the content, so the bar's buttons and value go before.
        /// </summary>
        public bool Open {
            get {
                if (begun) return open;

                begun = true;
                open  = Body(fadeKey, expanded, padded);
                return open;
            }
        }

        private readonly Rect bar;

        // The bar on the screen, which the rows' buttons line up with whatever group draws them.
        private readonly Rect barOnScreen;

        // The sections being drawn, the innermost on top.
        private static readonly Stack<Section> drawing = new();

        // How many sections are open, for a list to tell whether one frames it inside another list's item.
        internal static int Depth => drawing.Count;

        // Frames for what something dragged will take, on the screen, drawn over the border once it is drawn.
        private readonly List<(Rect Rect, bool Fill)> dragFrames = new();
        private readonly Rect barValue;
        private readonly bool folds;
        private readonly bool padded;
        private readonly float labelWidth;
        private readonly string fadeKey;
        private readonly bool expanded;
        private bool begun;
        private bool open;

        // An Inspector's hierarchy mode, which moves every foldout's arrow out to the left of its rect, past the box's edge; off within the section,
        // as a settings page has it, so the foldouts of the fields inside keep their arrows in the box.
        private readonly bool hierarchyMode = EditorGUIUtility.hierarchyMode;

        /// <summary>A section that stays open.</summary>
        /// <param name="buttons">How many buttons <see cref="Button"/> puts at the bar's right end.</param>
        /// <param name="padded">False for content with a frame of its own, or rows from <see cref="SectionRows"/>, which then meet the border.</param>
        /// <param name="barValue">As <see cref="Foldout(string,string,string,bool,int,bool,bool)"/>'s.</param>
        /// <param name="barOnly">The section is its bar, with no content under it, shaped as a folded section is: for one among foldouts of its kind.</param>
        public Section(string title, string tooltip = null, int buttons = 0, bool padded = true, bool barValue = false, bool barOnly = false) {
            EditorGUIUtility.hierarchyMode = false;
            Begin(out bar);
            EditorGUI.LabelField(Title(bar, buttons, barValue), new GUIContent(title, tooltip));
            if (!barOnly) BeginContent(padded);
            this.barValue = barValue ? BarValueRect(bar, buttons) : default;
            barOnScreen = GUIUtility.GUIToScreenRect(bar);
            drawing.Push(this);
            begun       = true;
            open        = !barOnly;
            folds       = false;
            this.padded = padded;
            labelWidth  = Narrow();
        }

        private Section(Rect bar, Rect barValue, string fadeKey, bool expanded, bool padded) {
            EditorGUIUtility.hierarchyMode = false;
            this.bar      = bar;
            barOnScreen   = GUIUtility.GUIToScreenRect(bar);
            drawing.Push(this);
            this.barValue = barValue;
            this.fadeKey  = fadeKey;
            this.expanded = expanded;
            folds         = true;
            this.padded   = padded;
            labelWidth    = Narrow();
        }

        /// <summary>A section the bar folds.</summary>
        /// <param name="key">Unique among the project's sections: the fold state is kept under it for the session.</param>
        /// <param name="open">The fold the first time the section shows this session.</param>
        /// <param name="barValue">
        /// The title keeps to the label column and leaves the value column to a field drawn in <see cref="BarValue"/>: for the one value<br/>
        /// a section always has, such as the default the rows under it vary.
        /// </param>
        public static Section Foldout(string key, string title, string tooltip = null, bool open = true, int buttons = 0, bool padded = true, bool barValue = false) {
            Begin(out var bar);
            open = Fold(Title(bar, buttons, barValue), SessionState.GetBool(key, open), new GUIContent(title, tooltip));
            SessionState.SetBool(key, open);
            return new Section(bar, barValue ? BarValueRect(bar, buttons) : default, key, open, padded);
        }

        // The value column of the bar, up to its buttons as a row's value is to its x, a field's height and centered.
        private static Rect BarValueRect(Rect bar, int buttons) {
            var line = RowContent(bar, EditorGUIUtility.singleLineHeight);
            return Rect.MinMaxRect(bar.x + EditorGUIUtility.labelWidth + LabelGap, line.y, buttons > 0 ? bar.xMax - 1 - buttons * ButtonWidth : bar.xMax - Inset, line.yMax);
        }

        /// <summary>Where a field on the bar goes, in the page's value column: set for a section made with <c>barValue</c>.</summary>
        public Rect BarValue => barValue;

        /// <summary>
        /// A section the bar folds, whose fold is the property's own state, so an Alt+click folds what is under it.<br/>
        /// Call <see cref="Restore"/> from the drawer's Initialize so the section opens as it was left.
        /// </summary>
        /// <param name="toggle">
        /// A bool drawn as a checkbox on the bar, before the title: the section opens as it turns on, and folds and edits as any other whether
        /// it is on or off, so its settings can be filled before it is.<br/>
        /// One marked <c>[ReadOnly]</c> shows off and locked, for a feature the project cannot use now, with its <c>[Tooltip]</c> saying why.
        /// </param>
        /// <param name="titleField">
        /// A string member's property, drawn as a text field in the title's place, after the arrow: for a section the page names, such as one item of a list. The arrow and the rest of the title's place fold; a click on the field edits.<br/>
        /// Its <c>[Tooltip]</c> goes with it; <paramref name="title"/> is not drawn.
        /// </param>
        public static Section Foldout(InspectorProperty property, string title, string tooltip = null, int buttons = 0, bool padded = true, bool barValue = false, InspectorProperty toggle = null,
                                      InspectorProperty titleField = null) {
            Begin(out var bar);
            var content = new GUIContent(title, tooltip);
            var open = toggle != null ? ToggleTitle(bar, buttons, property, toggle, content)
                : titleField != null  ? FieldTitle(Title(bar, buttons, barValue), property, titleField)
                : property.State.Expanded = Fold(Title(bar, buttons, barValue), property.State.Expanded, content);
            // Every draw: an Alt+click on an outer section sets this one's state without its bar.
            SessionState.SetBool(Key(property), property.State.Expanded);
            return new Section(bar, barValue ? BarValueRect(bar, buttons) : default, $"{RuntimeHelpers.GetHashCode(property.Tree)}/{property.Path}", open, padded);
        }

        // The arrow, the box, then the title, as Unity draws a toggled header such as a Volume override's: the arrow and the title fold, the box
        // alone turns the toggle, and turning it on opens the section. Written through the value entry, so it reaches every object edited, with undo.
        private static bool ToggleTitle(Rect bar, int buttons, InspectorProperty property, InspectorProperty toggle, GUIContent title) {
            const float arrow = 14;
            const float box   = 16;
            var line   = RowContent(Title(bar, buttons), EditorGUIUtility.singleLineHeight);
            var entry  = toggle.ValueEntry;
            var locked = toggle.GetAttribute<ReadOnlyAttribute>() != null;
            var on     = !locked && entry.WeakSmartValue is true;
            if (locked && toggle.GetAttribute<TooltipAttribute>() is { } why) title = new GUIContent(title.text, why.tooltip);

            var check = new Rect(line.x + arrow, line.y, box, line.height);
            var text  = Rect.MinMaxRect(check.xMax + LabelGap, line.y, line.xMax, line.yMax);
            var open  = Fold(new Rect(line.x, line.y, arrow, line.height), property.State.Expanded, GUIContent.none);
            var e = Event.current;
            if (e.type is EventType.MouseDown && e.button == 0 && text.Contains(e.mousePosition)) {
                open = !open;
                e.Use();
            }

            property.State.Expanded = open;

            using (new EditorGUI.DisabledScope(locked)) GUI.Label(text, title, EditorStyles.label);
            EditorGUI.showMixedValue = !locked && entry.ValueState is PropertyValueState.PrimitiveValueConflict;
            EditorGUI.BeginChangeCheck();
            bool next;
            using (new EditorGUI.DisabledScope(locked)) next = EditorGUI.Toggle(check, on);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck()) {
                entry.WeakSmartValue = next;
                if (next) property.State.Expanded = true;
            }

            return property.State.Expanded;
        }

        // The arrow, then the field, where a click edits rather than folds; the title's place past the field folds, as a title does.
        // Written through the value entry, so it reaches every object edited, with undo.
        private static bool FieldTitle(Rect title, InspectorProperty property, InspectorProperty field) {
            const float arrow = 14;
            var line = RowContent(title, EditorGUIUtility.singleLineHeight);
            property.State.Expanded = Fold(new Rect(line.x, line.y, arrow, line.height), property.State.Expanded, GUIContent.none);

            // As wide as its text, at least as wide as most names need so that a list's names line up, and never closer than the inset to what follows on the bar,
            // even where the column is narrower than that least.
            const float least = 120;
            var entry  = field.ValueEntry;
            var value  = entry.WeakSmartValue as string ?? "";
            var width  = Mathf.Max(0, Mathf.Min(Mathf.Max(EditorStyles.textField.CalcSize(new GUIContent(value)).x + Inset, least), line.xMax - Inset - line.x - arrow));
            var rect   = new Rect(line.x + arrow, line.y, width, line.height);
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel    = 0;
            EditorGUI.showMixedValue = entry.ValueState is PropertyValueState.PrimitiveValueConflict;
            EditorGUI.BeginChangeCheck();
            var text = EditorGUI.TextField(rect, value);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck()) entry.WeakSmartValue = text;
            EditorGUI.indentLevel = indent;
            if (field.GetAttribute<TooltipAttribute>() is { } tip) GUI.Label(rect, new GUIContent("", tip.tooltip));

            var rest = Rect.MinMaxRect(rect.xMax, title.y, title.xMax, title.yMax);
            var e    = Event.current;
            if (e.type is EventType.MouseDown && e.button == 0 && rest.Contains(e.mousePosition)) {
                property.State.Expanded = !property.State.Expanded;
                e.Use();
            }

            return property.State.Expanded;
        }

        /// <summary>Gives the property the fold it had this session, else <paramref name="open"/>.</summary>
        public static void Restore(InspectorProperty property, bool open = true) {
            property.State.Expanded = SessionState.GetBool(Key(property), open);
        }

        /// <summary>
        /// Opens or closes the property and everything under it, as an Alt+click on a foldout does, sections from <see cref="Foldout(InspectorProperty,string,string,int,bool,bool)"/> included: their kept fold changes too, so one whose drawer has not started yet opens as asked.
        /// </summary>
        public static void SetExpandedRecursively(InspectorProperty property, bool expanded) {
            property.State.Expanded = expanded;
            SessionState.SetBool(Key(property), expanded);
            foreach (var child in property.Children) SetExpandedRecursively(child, expanded);
        }

        private static string Key(InspectorProperty property) {
            return $"RiseOn.Section.{property.Tree.TargetType.FullName}.{property.Path}";
        }

        // Opens the property's fold and the one its section keeps, for SettingsFocus.
        internal static void Unfold(InspectorProperty property) {
            property.State.Expanded = true;
            SessionState.SetBool(Key(property), true);
        }

        /// <summary>The bar's button in <paramref name="slot"/>, counted from the right; true when clicked.</summary>
        public bool Button(int slot, SdfIconType icon, string tooltip) => IconButton(ButtonRect(slot), icon, tooltip);

        /// <summary>Where the bar's button in <paramref name="slot"/> sits, counted from the right.</summary>
        public Rect ButtonRect(int slot) => RowButton(bar, slot);

        /// <summary>The bar, where the layout put it.</summary>
        public Rect Bar => bar;

        /// <summary>
        /// Lights a section, as a list lights for objects let go on its bar: <paramref name="box"/> is the section, which <see cref="GUILayoutUtility.GetLastRect"/> gives right after the section is disposed.<br/>
        /// Draws on a repaint only.
        /// </summary>
        public static void DrawDropLight(Rect box) {
            if (Event.current.type is not EventType.Repaint) return;

            var rect = BoxRect(box);
            Round(rect, DropFill, 0, Radius, Radius);
            Round(rect, DropOutline, 1, Radius, Radius);
        }

        /// <summary>
        /// Outlines a part of a section's content that something dragged will take, such as a list's gap or its dragged row, and fills it when <paramref name="fill"/>.<br/>
        /// In a section, it is drawn once the innermost section being drawn has drawn its border, from border to border and, when it reaches the content's end, down to the box's bottom: its outline then lies on the border, and its corners round with the box's.<br/>
        /// Outside a section, it is drawn at once.<br/>
        /// Draws on a repaint only.
        /// </summary>
        public static void DrawDragFrame(Rect rect, bool fill) {
            if (Event.current.type is not EventType.Repaint) return;

            if (drawing.Count is 0) {
                DrawFrame(PixelUtils.Snap(rect), fill, 0);
                return;
            }

            drawing.Peek().dragFrames.Add((new Rect(rect.position + PixelUtils.Origin(), rect.size), fill));
        }

        private static void DrawFrame(Rect rect, bool fill, float bottom) {
            if (fill) Round(rect, DropFill, 0, 0, bottom);
            Round(rect, DropOutline, 1, 0, bottom);
        }

        // The drag frames over the border just drawn around box, across it and down to its bottom where they reach the content's end.
        private void DrawDragFrames(Rect box) {
            foreach (var (onScreen, fill) in dragFrames) {
                var rect = PixelUtils.Snap(new Rect(onScreen.position - PixelUtils.Origin(), onScreen.size));
                rect.xMin = box.xMin;
                rect.xMax = box.xMax;
                var isAtBottom = rect.yMax >= box.yMax - 2 * PixelUtils.Pixel;
                if (isAtBottom) rect.yMax = box.yMax;
                DrawFrame(rect, fill, isAtBottom ? Radius : 0);
            }

            dragFrames.Clear();
        }

        /// <summary>The space above and below a row's content, and inside a padded section's border: the space between two fields.</summary>
        public static float RowPadding => EditorGUIUtility.standardVerticalSpacing;

        /// <summary>Where content of <paramref name="content"/> points sits in a row such as the bar, <see cref="RowPadding"/> under its top: on the pixel grid.</summary>
        public static Rect RowContent(Rect row, float content) {
            var snapped = PixelUtils.Snap(row);
            return new Rect(snapped.x, snapped.y + PixelUtils.Snap(RowPadding), snapped.width, PixelUtils.Snap(content));
        }

        /// <summary>The darkening of an odd row; from the even and odd colors of Odin's lists, 60 over 51 in the dark skin.</summary>
        public static readonly Color Shade = new(0, 0, 0, 0.15f);

        /// <summary>The outline and the fill of what takes objects dragged from elsewhere: the gap a list opens for them, or a section lit for a drop on its bar.</summary>
        public static readonly Color DropOutline = new(.3f, .6f, 1, 1);

        /// <inheritdoc cref="DropOutline"/>
        public static readonly Color DropFill = new(.3f, .6f, 1, .1f);

        /// <summary>
        /// A row's button in <paramref name="slot"/>, counted from the right, under the bar's: inside a section being drawn, in the bar's own column on the screen, whatever group draws the row, so its icon lands on the same pixels as the bar's.
        /// </summary>
        public static Rect RowButton(Rect row, int slot) {
            // A group such as a fade's clip starts on a whole pixel, which can move what it holds by part of a pixel from the bar.
            var right = drawing.Count > 0 ? drawing.Peek().barOnScreen.xMax - PixelUtils.Origin().x : row.xMax;
            return new Rect(right - 1 - ButtonWidth * (slot + 1), row.y + 1, ButtonWidth, row.height - 2);
        }

        /// <summary>A row's drag handle, the mirror of <see cref="RowButton"/>'s first slot at the bar's left end, so the two sides keep the same square.</summary>
        public static Rect RowHandle(Rect row) {
            var left = drawing.Count > 0 ? drawing.Peek().barOnScreen.xMin - PixelUtils.Origin().x : row.x;
            return new Rect(left + 1, row.y + 1, ButtonWidth, row.height - 2);
        }

        /// <summary>
        /// A button of one vector icon at <see cref="IconSize"/>, centered on the rect, in the label's color and lit under the mouse; true when clicked.<br/>
        /// Every button of a settings page draws with it, so icons keep one size whatever the button's.
        /// </summary>
        public static bool IconButton(Rect rect, SdfIconType icon, string tooltip) {
            var lit     = HoverUtils.Track(rect) && GUI.enabled;
            var clicked = GUI.Button(rect, new GUIContent(string.Empty, tooltip), GUIStyle.none);
            if (Event.current.type is EventType.Repaint) Icon(rect, icon, lit);
            return clicked;
        }

        /// <summary>
        /// A text field with a button of one icon inside its right end, as a search field holds its clear button: for an action on the text, such as taking it back to a default, so the field still reaches the row's buttons.<br/>
        /// The button shows while <paramref name="shown"/>, the text then keeping clear of it; it is a control every event even while hidden, so the field keeps its focus as the button comes and goes.<br/>
        /// A click on it ends the editing of the field.
        /// </summary>
        public static string TextField(Rect rect, string text, SdfIconType icon, string tooltip, bool shown, out bool clicked) {
            // On the pixel grid, as the field's frame draws, so the button's middle is the frame's.
            rect = PixelUtils.Snap(rect);
            var button = new Rect(rect.xMax - rect.height, rect.y, rect.height, rect.height);
            var lit    = HoverUtils.Track(button) && shown && GUI.enabled;
            // Before the field, so the click is the button's rather than the field's.
            clicked = GUI.Button(shown ? button : default, new GUIContent(string.Empty, shown ? tooltip : null), GUIStyle.none) && shown;
            if (clicked) GUIUtility.keyboardControl = 0;
            // Before the field's text cursor, which would otherwise win over the button.
            if (shown) EditorGUIUtility.AddCursorRect(button, MouseCursor.Arrow);

            inlineButtonField ??= new GUIStyle(EditorStyles.textField) {
                padding = new RectOffset(EditorStyles.textField.padding.left, (int) EditorGUIUtility.singleLineHeight, EditorStyles.textField.padding.top, EditorStyles.textField.padding.bottom)
            };
            var edited = EditorGUI.TextField(rect, text, shown ? inlineButtonField : EditorStyles.textField);
            if (shown && Event.current.type is EventType.Repaint) Icon(button, icon, lit);
            return edited;
        }

        private static GUIStyle inlineButtonField;

        /// <summary>
        /// An icon as <see cref="IconButton"/> draws it, for one that is not a button, such as a drag handle: the page's one icon size, in the label's color, lighter while <paramref name="lit"/>, its middle on the rect's.<br/>
        /// Draw it on Repaint.
        /// </summary>
        // Odin draws an icon at whole points, shifted by a table made for whole pixels, off the middle by up to a pixel on a screen
        // not at 100%; made at the screen's pixels and put on the pixel grid, it keeps to the middle.
        public static void Icon(Rect rect, SdfIconType icon, bool lit = false) {
            var color = EditorStyles.label.normal.textColor;
            if (lit) color = Color.Lerp(color, Color.white, 0.5f);
            if (!GUI.enabled) color.a *= 0.4f;

            var ppp     = EditorGUIUtility.pixelsPerPoint;
            var texture = IconTexture(icon, Mathf.RoundToInt(Mathf.Min(IconSize, rect.width, rect.height) * ppp));
            // A half pixel rounded up: an icon a pixel short of the middle leaves the spare pixel on its left and above, so a button at
            // the end of a field or row is as far from the frame on its right as above and below.
            // On the window's grid, wherever the group drawing it starts, so the same rect shows the icon on the same pixels in a row as on the bar.
            var origin = PixelUtils.Origin();
            var x      = Mathf.Floor((rect.center.x + origin.x) * ppp - texture.width / 2f + 0.51f) / ppp - origin.x;
            var y      = Mathf.Floor((rect.center.y + origin.y) * ppp - texture.height / 2f + 0.51f) / ppp - origin.y;
            var image = new Rect(x, y, texture.width / ppp, texture.height / ppp);
            var tint  = GUI.color;
            GUI.color = tint * color;
            GUI.DrawTexture(image, texture);
            GUI.color = tint;
        }

        private static readonly Dictionary<(SdfIconType, int), Texture2D> iconTextures = new();

        // An icon in white, the size of a square of so many pixels; unloaded with the unused assets once a reload drops the cache.
        // Made four times as large and averaged down: at the size itself Odin thins every stroke to one sharp pixel. Each stroke
        // grows a quarter pixel on either side first, as Odin's own icons on a screen at 125% or 150% do, so they weigh the same.
        private static Texture2D IconTexture(SdfIconType icon, int pixels) {
            if (iconTextures.TryGetValue((icon, pixels), out var texture) && texture != null) return texture;

            const int Scale = 4;
            var large  = SdfIcons.CreateTransparentIconTexture(icon, Color.white, pixels * Scale, pixels * Scale, 0);
            var source = large.GetPixels32();
            var width  = (large.width + Scale - 1) / Scale;
            var height = (large.height + Scale - 1) / Scale;
            var sums   = new int[width * height];
            for (var y = 0; y < large.height; y++) {
                for (var x = 0; x < large.width; x++) sums[y / Scale * width + x / Scale] += Grown(source, large.width, large.height, x, y);
            }

            UnityEngine.Object.DestroyImmediate(large);
            var shrunk = sums.Select(sum => new Color32(255, 255, 255, (byte) (sum / (Scale * Scale)))).ToArray();

            texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(shrunk);
            texture.Apply();
            iconTextures[(icon, pixels)] = texture;
            return texture;

            // The most of the texel and the eight around it.
            static int Grown(Color32[] source, int width, int height, int x, int y) {
                var most = 0;
                for (var j = Mathf.Max(y - 1, 0); j <= Mathf.Min(y + 1, height - 1); j++) {
                    for (var i = Mathf.Max(x - 1, 0); i <= Mathf.Min(x + 1, width - 1); i++) most = Mathf.Max(most, source[j * width + i].a);
                }

                return most;
            }
        }

        /// <summary>
        /// Odin's fade, the one its foldouts use, for content that folds under a row; true while any of it shows.<br/>
        /// Odin asks only its own windows to repaint while it fades, so this asks the window it draws in, a settings page included (<see cref="RepaintUtils"/>).<br/>
        /// Call <see cref="EndFade"/> after it whatever it returns.
        /// </summary>
        public static bool BeginFade(object key, bool open) {
            var shows = SirenixEditorGUI.BeginFadeGroup(key, open, out var faded);
            if (faded is > 0 and < 1) RepaintUtils.Request();
            return shows;
        }

        /// <summary>Ends what <see cref="BeginFade"/> began.</summary>
        public static void EndFade() {
            SirenixEditorGUI.EndFadeGroup();
        }

        public void Dispose() {
            EditorGUIUtility.labelWidth = labelWidth;
            // Begins the content of a section nothing drew in, so its fade runs as any other's.
            if (Open) {
                // The box's border is drawn over the end of the content, where the bar's line above lies inside the bar: a point,
                // whole as the rest of the layout. Padded content needs nothing more: its last control's own margin is the space between two fields.
                GUILayout.Space(1);
                GUILayout.EndVertical();
            }

            if (folds) EndFade();

            GUILayout.EndVertical();
            EditorGUIUtility.hierarchyMode = hierarchyMode;
            if (drawing.Count > 0 && drawing.Peek() == this) drawing.Pop();
            // The border goes over the content, so rows meet it rather than cover it. On the pixel grid, as the bar and the
            // background are, so the space to the next section is as many whole pixels as the space between two fields.
            if (Event.current.type is not EventType.Repaint) return;

            var box = BoxRect(GUILayoutUtility.GetLastRect());
            Round(box, SirenixGUIStyles.BorderColor, 1, Radius, Radius);
            DrawDragFrames(box);
        }

        // The box on the pixel grid, its bottom a pixel under the content when there is some: the point the layout keeps
        // for the border is more than a pixel on a screen above 100%, and the background would show between the last row
        // and the border wherever that point spans two pixels.
        private static Rect BoxRect(Rect box) {
            var snapped = PixelUtils.Snap(box);
            if (box.height - Height >= 1) snapped.yMax = PixelUtils.SnapY(box.yMax - 1) + PixelUtils.Pixel;
            return snapped;
        }

        // The box's background and the bar go first, under the title and the content.
        // The bar's lower corners round as the content under it shrinks below the radius, so a fold closes on the same shape it ends in.
        private static void Begin(out Rect bar) {
            var box = EditorGUILayout.BeginVertical(outer);
            bar = GUILayoutUtility.GetRect(0, Height, GUILayout.ExpandWidth(true));
            if (Event.current.type is not EventType.Repaint) return;

            // On the pixel grid, so the line under the bar meets what comes under it without a sliver of the bar between.
            var shown = PixelUtils.Snap(bar);
            var below = box.height - Height;
            SectionRows.NoteSectionFrame(BoxRect(box));
            Round(BoxRect(box), SirenixGUIStyles.BoxBackgroundColor, 0, Radius, Radius);
            Round(shown, SirenixGUIStyles.HeaderBoxBackgroundColor, 0, Radius, Mathf.Clamp(Radius - below, 0, Radius));
            if (below >= 1) EditorGUI.DrawRect(new Rect(shown.x, shown.yMax - PixelUtils.Pixel, shown.width, PixelUtils.Pixel), SirenixGUIStyles.BorderColor);
        }

        // Labels inside start an inset further in, so the label column narrows by as much: the fields then line up with the page's plain rows.
        // Returns the width to restore.
        private static float Narrow() {
            var width = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = width - Inset;
            return width;
        }

        // The bar's foldout, its arrow inside the rect: an Inspector turns hierarchy mode on, which moves a foldout's arrow out to the left of
        // its rect by the style's padding, past the bar's edge; a settings page has it off.
        private static bool Fold(Rect rect, bool open, GUIContent title) {
            var hierarchy = EditorGUIUtility.hierarchyMode;
            EditorGUIUtility.hierarchyMode = false;
            open = EditorGUI.Foldout(rect, open, title, true);
            EditorGUIUtility.hierarchyMode = hierarchy;
            return open;
        }

        // With a value on the bar, the title keeps to the label column.
        private static Rect Title(Rect bar, int buttons, bool barValue = false) {
            var width = barValue ? EditorGUIUtility.labelWidth - Inset : bar.width - Inset - buttons * ButtonWidth;
            return new Rect(bar.x + Inset, bar.y, width, bar.height);
        }

        // Odin's fade, the one its foldout groups use: the padding shrinks with the content.
        private static bool Body(string key, bool open, bool padded) {
            if (!BeginFade(key, open)) return false;

            BeginContent(padded);
            return true;
        }

        private static void BeginContent(bool padded) {
            GUILayout.BeginVertical(padded ? padding : GUIStyle.none);
            if (!padded) return;

            GUILayout.Space(RowPadding);
        }

        // A filled rect, or its outline when width is above 0, with the top and bottom corners rounded as asked.
        private static void Round(Rect rect, Color color, float width, float top, float bottom) {
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, color, new Vector4(width, width, width, width), new Vector4(top, top, bottom, bottom));
        }
    }

    /// <summary>Draws <see cref="SectionGroupAttribute"/> as a <see cref="Section"/>.</summary>
    public sealed class SectionGroupAttributeDrawer : OdinGroupDrawer<SectionGroupAttribute> {
        // The group's member drawn on the bar rather than in it.
        private InspectorProperty toggle;

        protected override void Initialize() {
            toggle = Attribute.Toggle == null ? null : Property.Children[Attribute.Toggle];
            if (Attribute.Foldable || toggle != null) Section.Restore(Property, !Attribute.Closed);
        }

        protected override void DrawPropertyLayout(GUIContent label) {
            using var section = Attribute.Foldable || toggle != null
                ? Section.Foldout(Property, Attribute.GroupName, Attribute.Tooltip, padded: !Attribute.Paddingless, toggle: toggle)
                : new Section(Attribute.GroupName, Attribute.Tooltip, padded: !Attribute.Paddingless);
            if (!section.Open) return;

            foreach (var child in Property.Children) {
                if (child != toggle) child.Draw();
            }
        }
    }
}