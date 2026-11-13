using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// A section of a settings page, drawn as Odin draws its groups: a rounded box in Odin's colors under a header bar.<br/>
    /// The bar's tooltip carries the explanation, and its right end holds buttons from <see cref="Button"/>.<br/>
    /// <c>Foldout</c> makes one the bar folds, with Odin's fade: only for a section worth putting away, long or seldom used.<br/>
    /// Draw on the bar, its buttons and <see cref="BarValue"/>, first, then the content inside the scope while <see cref="Open"/>: a folding section's content begins, in its fade, where <see cref="Open"/> is first read.<br/>
    /// <see cref="Row"/> and <see cref="RowButton"/> line up with the bar.
    /// </summary>
    /// <example><code>
    /// using (var section = new SettingsSection("Items", "What the game sells.", buttons: 1, padded: false)) {
    ///     if (section.Button(0, SdfIconType.Plus, "Add an item")) Add();
    ///     DrawItems();
    /// }
    ///
    /// using (var section = SettingsSection.Foldout("MyPack.Testing", "Testing", open: false)) {
    ///     if (section.Open) DrawTesting();
    /// }
    /// </code></example>
    public sealed class SettingsSection : IDisposable {
        /// <summary>The bar's height, and a row's from <see cref="Row"/>.</summary>
        public const float Height = 22;

        /// <summary>The width of a button on the bar or on a row.</summary>
        public const float ButtonWidth = 22;

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
        private readonly Rect barValue;
        private readonly bool folds;
        private readonly bool padded;
        private readonly float labelWidth;
        private readonly string fadeKey;
        private readonly bool expanded;
        private bool begun;
        private bool open;

        /// <summary>A section that stays open.</summary>
        /// <param name="buttons">How many buttons <see cref="Button"/> puts at the bar's right end.</param>
        /// <param name="padded">False for content with a frame of its own, or rows from <see cref="Row"/>, which then meet the border.</param>
        /// <param name="barValue">As <see cref="Foldout(string,string,string,bool,int,bool,bool)"/>'s.</param>
        /// <param name="barOnly">The section is its bar, with no content under it, shaped as a folded section is: for one among foldouts of its kind.</param>
        public SettingsSection(string title, string tooltip = null, int buttons = 0, bool padded = true, bool barValue = false, bool barOnly = false) {
            Begin(out bar);
            EditorGUI.LabelField(Title(bar, buttons, barValue), new GUIContent(title, tooltip));
            if (!barOnly) BeginContent(padded);
            this.barValue = barValue ? BarValueRect(bar, buttons) : default;
            begun       = true;
            open        = !barOnly;
            folds       = false;
            this.padded = padded;
            labelWidth  = Narrow();
        }

        private SettingsSection(Rect bar, Rect barValue, string fadeKey, bool expanded, bool padded) {
            this.bar      = bar;
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
        public static SettingsSection Foldout(string key, string title, string tooltip = null, bool open = true, int buttons = 0, bool padded = true, bool barValue = false) {
            Begin(out var bar);
            open = EditorGUI.Foldout(Title(bar, buttons, barValue), SessionState.GetBool(key, open), new GUIContent(title, tooltip), true);
            SessionState.SetBool(key, open);
            return new SettingsSection(bar, barValue ? BarValueRect(bar, buttons) : default, key, open, padded);
        }

        // The value column of the bar, up to its buttons, a field's height and centered.
        private static Rect BarValueRect(Rect bar, int buttons) {
            var line = RowContent(bar, EditorGUIUtility.singleLineHeight);
            return Rect.MinMaxRect(bar.x + EditorGUIUtility.labelWidth + LabelGap, line.y, bar.xMax - 1 - buttons * ButtonWidth - 2, line.yMax);
        }

        /// <summary>Where a field on the bar goes, in the page's value column: set for a section made with <c>barValue</c>.</summary>
        public Rect BarValue => barValue;

        /// <summary>
        /// A section the bar folds, whose fold is the property's own state, so an Alt+click folds what is under it.<br/>
        /// Call <see cref="Restore"/> from the drawer's Initialize so the section opens as it was left.
        /// </summary>
        public static SettingsSection Foldout(InspectorProperty property, string title, string tooltip = null, int buttons = 0, bool padded = true, bool barValue = false) {
            Begin(out var bar);
            property.State.Expanded = EditorGUI.Foldout(Title(bar, buttons, barValue), property.State.Expanded, new GUIContent(title, tooltip), true);
            // Every draw: an Alt+click on an outer section sets this one's state without its bar.
            SessionState.SetBool(Key(property), property.State.Expanded);
            return new SettingsSection(bar, barValue ? BarValueRect(bar, buttons) : default, $"{RuntimeHelpers.GetHashCode(property.Tree)}/{property.Path}", property.State.Expanded, padded);
        }

        /// <summary>Gives the property the fold it had this session, else <paramref name="open"/>.</summary>
        public static void Restore(InspectorProperty property, bool open = true) => property.State.Expanded = SessionState.GetBool(Key(property), open);

        /// <summary>
        /// Opens or closes the property and everything under it, as an Alt+click on a foldout does, sections from <see cref="Foldout(InspectorProperty,string,string,int,bool,bool)"/> included: their kept fold changes too, so one whose drawer has not started yet opens as asked.
        /// </summary>
        public static void SetExpandedRecursively(InspectorProperty property, bool expanded) {
            property.State.Expanded = expanded;
            SessionState.SetBool(Key(property), expanded);
            foreach (var child in property.Children) SetExpandedRecursively(child, expanded);
        }

        private static string Key(InspectorProperty property) => $"RiseOn.SettingsSection.{property.Tree.TargetType.FullName}.{property.Path}";

        /// <summary>The bar's button in <paramref name="slot"/>, counted from the right; true when clicked.</summary>
        public bool Button(int slot, SdfIconType icon, string tooltip) => IconButton(ButtonRect(slot), icon, tooltip);

        /// <summary>Where the bar's button in <paramref name="slot"/> sits, counted from the right.</summary>
        public Rect ButtonRect(int slot) => RowButton(bar, slot);

        /// <summary>
        /// A row of a list inside an unpadded section, striped and lit under the mouse.<br/>
        /// Buttons from <see cref="RowButton"/> line up with the bar's.
        /// </summary>
        public static Rect Row(int index) => Row(index, EditorGUIUtility.singleLineHeight);

        /// <summary>A row as <see cref="Row(int)"/>, for content of <paramref name="content"/> points; <see cref="RowContent"/> places it.</summary>
        public static Rect Row(int index, float content) {
            var row = GUILayoutUtility.GetRect(0, RowHeight(content), GUILayout.ExpandWidth(true));
            RowBackground(row, index);
            return row;
        }

        /// <summary>The space above and below a row's content, and inside a padded section's border: the space between two fields.</summary>
        public static float RowPadding => EditorGUIUtility.standardVerticalSpacing;

        /// <summary>
        /// The height of a row for content of <paramref name="content"/> points, as much above the content as below: whole points, as Unity's controls are, since GUILayout puts each entry at a whole point and a fraction would leave uneven space between what follows.<br/>
        /// Drawing puts the row on the pixel grid.
        /// </summary>
        public static float RowHeight(float content) => Mathf.Round(content) + 2 * RowPadding;

        /// <summary>Where content of <paramref name="content"/> points sits in a row of <see cref="RowHeight"/>: on the pixel grid, centered.</summary>
        public static Rect RowContent(Rect row, float content) {
            var snapped = PixelUtils.Snap(row);
            return new Rect(snapped.x, snapped.y + PixelUtils.Snap(RowPadding), snapped.width, PixelUtils.Snap(content));
        }

        /// <summary>
        /// The stripe of the row at <paramref name="index"/>, lit under the mouse, for a list that lays out its own rows.<br/>
        /// Shades over whatever is under the row rather than painting a color of its own: an even row leaves it as it is and an odd one darkens it, so a list takes on the box or the page it lies on, in either skin; the row under the mouse takes <see cref="Hover"/>.<br/>
        /// Call it on every event, before the row's content.
        /// </summary>
        public static void RowBackground(Rect row, int index) {
            var lit = HoverUtils.Track(row);
            if (Event.current.type is not EventType.Repaint) return;

            // On the pixel grid, so rows of a height between pixels neither leave a sliver between them nor overlap.
            var snapped = PixelUtils.Snap(row);
            if (index % 2 is 1) EditorGUI.DrawRect(snapped, Shade);
            if (lit) EditorGUI.DrawRect(snapped, Hover);
        }

        /// <summary>The darkening of an odd row; from the even and odd colors of Odin's lists, 60 over 51 in the dark skin.</summary>
        public static readonly Color Shade = new(0, 0, 0, 0.15f);

        /// <summary>
        /// A row under the mouse, as Unity's own lists paint one (.unity-collection-view__item:hover): a solid color over the stripe, so a lit row looks the same on any stripe and on any box.
        /// </summary>
        public static Color Hover => EditorGUIUtility.isProSkin ? new Color32(0x30, 0x30, 0x30, 0xFF) : new Color32(0xB2, 0xB2, 0xB2, 0xFF);

        /// <summary>
        /// A ReorderableList's rows, flush with the section as <see cref="Row"/>'s are: without the space the list keeps above and below them, which its striped rows would show as two bands.<br/>
        /// For a list with no header, footer or background of its own, inside an unpadded section.
        /// </summary>
        public static void List(ReorderableList list) {
            // The header, which ReorderableList keeps at least its internal Defaults.minHeaderHeight tall, then its internal listElementTopPadding: 4 under a header, else 1.
            var top    = Mathf.Max(list.headerHeight, MinHeaderHeight) + (list.headerHeight > 5 ? 4 : 1);
            var height = list.GetHeight() - top - BottomPadding;
            var rect   = GUILayoutUtility.GetRect(0, height, GUILayout.ExpandWidth(true));
            list.DoList(new Rect(rect.x, rect.y - top, rect.width, height + top + BottomPadding));
        }

        /// <summary>The height ReorderableList adds to elementHeight for each row, its Defaults.elementPadding: an elementHeight of a row's height less this gives rows that height.</summary>
        public const float ListRowPadding = 2;

        // ReorderableList's space under its last row, its kListElementBottomPadding, and its least header, Defaults.minHeaderHeight.
        private const float BottomPadding   = 4;
        private const float MinHeaderHeight = 2;

        /// <summary>A row's button in <paramref name="slot"/>, counted from the right, under the bar's.</summary>
        public static Rect RowButton(Rect row, int slot) {
            return new Rect(row.xMax - 1 - ButtonWidth * (slot + 1), row.y + 1, ButtonWidth, row.height - 2);
        }

        /// <summary>
        /// A button of one vector icon at <see cref="IconSize"/>, centered on the rect, in the label's color and lit under the mouse; true when clicked.<br/>
        /// Every button of a settings page draws with it, so icons keep one size whatever the button's.
        /// </summary>
        public static bool IconButton(Rect rect, SdfIconType icon, string tooltip) {
            var lit     = HoverUtils.Track(rect) && GUI.enabled;
            var clicked = GUI.Button(rect, new GUIContent(string.Empty, tooltip), GUIStyle.none);
            if (Event.current.type is EventType.Repaint) DrawIcon(rect, icon, lit);
            return clicked;
        }

        /// <summary>
        /// A text field with a button of one icon inside its right end, as a search field holds its clear button: for an action on the text,<br/>
        /// such as taking it back to a default, so the field still reaches the row's buttons.<br/>
        /// The button shows while <paramref name="shown"/>, the text then keeping clear of it; it is a control every event even while hidden,<br/>
        /// so the field keeps its focus as the button comes and goes. A click on it ends the editing of the field.
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
            if (shown && Event.current.type is EventType.Repaint) DrawIcon(button, icon, lit);
            return edited;
        }

        private static GUIStyle inlineButtonField;

        // The page's one icon size, in the label's color, lighter under the mouse, its middle on the rect's.
        // Odin draws an icon at whole points, shifted by a table made for whole pixels, off the middle by up to a pixel on a screen
        // not at 100%; made at the screen's pixels and put on the pixel grid, it keeps to the middle.
        private static void DrawIcon(Rect rect, SdfIconType icon, bool lit) {
            var color = EditorStyles.label.normal.textColor;
            if (lit) color = Color.Lerp(color, Color.white, 0.5f);
            if (!GUI.enabled) color.a *= 0.4f;

            var ppp     = EditorGUIUtility.pixelsPerPoint;
            var texture = IconTexture(icon, Mathf.RoundToInt(Mathf.Min(IconSize, rect.width, rect.height) * ppp));
            // A half pixel rounded up: an icon a pixel short of the middle leaves the spare pixel on its left and above, so a button at
            // the end of a field or row is as far from the frame on its right as above and below.
            var x     = Mathf.Floor(rect.center.x * ppp - texture.width / 2f + 0.51f) / ppp;
            var y     = Mathf.Floor(rect.center.y * ppp - texture.height / 2f + 0.51f) / ppp;
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
        /// Odin asks only its own windows to repaint while it fades, so this asks the window it draws in, a settings page included.<br/>
        /// Call <see cref="EndFade"/> after it whatever it returns.
        /// </summary>
        public static bool BeginFade(object key, bool open) {
            var shows = SirenixEditorGUI.BeginFadeGroup(key, open, out var faded);
            if (faded is > 0 and < 1) HandleUtility.Repaint();
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
            // The border goes over the content, so rows meet it rather than cover it. On the pixel grid, as the bar and the
            // background are, so the space to the next section is as many whole pixels as the space between two fields.
            if (Event.current.type is EventType.Repaint) Round(PixelUtils.Snap(GUILayoutUtility.GetLastRect()), SirenixGUIStyles.BorderColor, 1, Radius, Radius);
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
            Round(PixelUtils.Snap(box), SirenixGUIStyles.BoxBackgroundColor, 0, Radius, Radius);
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

    /// <summary>Draws <see cref="SectionGroupAttribute"/> as a <see cref="SettingsSection"/>.</summary>
    public sealed class SectionGroupAttributeDrawer : OdinGroupDrawer<SectionGroupAttribute> {
        protected override void Initialize() {
            if (Attribute.Foldable) SettingsSection.Restore(Property, !Attribute.Closed);
        }

        protected override void DrawPropertyLayout(GUIContent label) {
            using var section = Attribute.Foldable
                ? SettingsSection.Foldout(Property, Attribute.GroupName, Attribute.Tooltip, padded: !Attribute.Paddingless)
                : new SettingsSection(Attribute.GroupName, Attribute.Tooltip, padded: !Attribute.Paddingless);
            if (!section.Open) return;

            foreach (var child in Property.Children) child.Draw();
        }
    }
}