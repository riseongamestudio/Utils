using System;
using System.Collections.Generic;
using System.Linq;
using RiseOn.Utils.Editor.SearchWindow;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// The items of a list as the rows of an unpadded <see cref="Section"/>, which draws the list's title bar and frame: every list of a RiseOn page or inspector draws with it.<br/>
    /// Items are striped, as Odin's lists are, and not lit under the pointer; an empty list shows a hint.<br/>
    /// A section inside an item keeps its own background: the item's stripe goes around it.<br/>
    /// An item that can move has a drag handle on its left, and one that can be removed an x on its right, in squares of one size mirrored at the bar's ends; both are level with the item's first row.<br/>
    /// A drag moves: the dragged item follows the pointer above the others, which glide aside to open the gap it will land in.<br/>
    /// A list that takes drops opens such a gap for objects dragged from elsewhere, such as assets from the Project window, and they land there:
    /// over the handle's column, the x's, the edge between two items, past the last item or in the gap itself. Over an item's content, the drag is left to its fields,
    /// so an object field under the pointer takes the objects itself, as in Unity's lists.<br/>
    /// An item's own drawer can lay out its first row with <see cref="MarkFirstRow(Rect)"/>, which keeps only that row clear of the handle and the x, the rows below starting under the handle's middle;
    /// otherwise the whole item keeps clear of them. The values line up with the page's fields either way.<br/>
    /// Moves and removals run after the list is drawn, so its items never change midway. One per list drawn, as it keeps the drag between events.
    /// </summary>
    /// <example><code>
    /// using (var section = Section.Foldout("MyPack.Items", "Items", buttons: 1, padded: false)) {
    ///     SectionRows.AddButton(section, 0, "item", null, _ => Add());
    ///     if (section.Open) rows.Draw(items.Count, i => DrawItem(i), "No item yet.", Move, Remove);
    /// }
    /// </code></example>
    public sealed class SectionRows {
        // Where the rows below a first row marked by the item start: under the handle's middle; in a nested list, where the first row starts,
        // so a foldout below lines up with the item's own instead of standing out to its left.
        private const int BodyInset       = 1 + (int) Section.ButtonWidth / 2;
        private const int NestedBodyInset = 20;

        // How the other items glide to their place, as Unity's SmoothDamp: about the time to cover the way, easing in and out.
        private const float GlideTime = .03f;

        // The most of the glide a frame takes, in seconds. While something is dragged from another view, the editor draws only as the drag
        // moves, every 64 ms or so while the pointer rests: such a frame takes this much of the glide, a small step, rather than a leap to catch up.
        private const float GlideStepMax = .025f;

        // How long objects dragged from elsewhere stay over the list before it opens its gap: a pointer passing over the items on its way
        // crosses the edges between them, and would open and close the gap at each.
        private const float DropOpenDelay = .1f;

        // A row of one field, the height of the gap a drop opens, however many objects it holds.
        private static float RowHeight => EditorGUIUtility.singleLineHeight + 2 * Section.RowPadding;

        // An item, edge to edge, so what Odin draws along an item's edge, such as the bar of a right click, sits where it does on Odin's own items.
        private static readonly GUIStyle itemStyle = new(GUIStyle.none);

        // The items being drawn, the innermost list's on top; an item's drawer marks its first row on the top one.
        private static readonly Stack<Slot> slots = new();

        // The outermost list being drawn, which keeps the view being drawn for every list inside it.
        private static SectionRows root;

        private readonly List<Rect>  itemRects = new();
        private readonly List<float> offsets   = new();
        private readonly List<float> speeds    = new();

        // The whole drag belongs to one control of the list, so it holds while the dragged item is drawn out of its place.
        private int    listId;
        private int    dragIndex = -1;
        private int    landIndex;
        private float  pressY;
        private float  dragOffset;
        private double glideTime;
        private bool   isGliding;
        private float  gapSpeed;

        // When objects that the list takes came over it, while its gap is not open yet; negative while none are.
        private double dropComing = -1;

        // The objects dragged from elsewhere that the list takes, while they are over it, the gap the items open for them, and the list's rect, gap included.
        private readonly List<Object> dropping = new();
        private          float        gap;
        private          Rect         listRect;

        // Set for each draw: the columns the handle and the x keep, and how the content is padded.
        private float    handleColumn;
        private float    removeColumn;
        private int      bodyInset;
        private GUIStyle content;

        private Action<int>      drawItem;
        private Action<int, int> move;
        private Action<int>      remove;
        private string           removeTooltip;

        private Func<Object, bool>                  takes;
        private Action<int, IReadOnlyList<Object>> drop;

        private Func<int, bool>              isSelected;
        private Action<int, EventModifiers> select;

        // Unity's own for a selected row of a list, such as the Package Manager window's.
        private static Color Selected => EditorGUIUtility.isProSkin ? new Color32(0x2C, 0x5D, 0x87, 0xFF) : new Color32(0x3A, 0x72, 0xB0, 0xFF);

        // What a press on a handle or an x asked for, run once the list is drawn.
        private Action pending;

        // Kept on the outermost list: the view being drawn, as a list drawn in two views, such as two Project Settings windows, keeps the
        // frames of each.
        private int view;

        // For each view and each item, the frames of the sections in it on the last repaint: its stripe goes around them, as a section's
        // background is a faint white over what is under it, and would take the item's stripe. The item is striped before what is in it is
        // drawn, so the frames are the last repaint's, kept by their distance from the item's four edges: a section that grows as the view
        // narrows grows the item with it.
        private readonly Dictionary<(int View, int Index), List<FrameInset>> sectionFrames = new();

        /// <param name="drawItem">Lays out one item with GUILayout, with the label column set so its values line up with the page's.</param>
        /// <param name="emptyHint">Shown grayed out while the list has no item, such as what + adds.</param>
        /// <param name="move">Moves the item at the first index to the second, as the list reads after it left its place; null for a list whose order is not the user's.</param>
        /// <param name="remove">Removes the item at the index; null for a list nothing leaves.</param>
        /// <param name="removeTooltip">The x's tooltip, such as "Remove the device".</param>
        /// <param name="marksFirstRow">Every item's drawer calls <see cref="MarkFirstRow(Rect)"/>, so only that row keeps clear of the handle and the x.</param>
        /// <param name="takes">Whether the list takes an object dragged from elsewhere; only with <paramref name="drop"/>.</param>
        /// <param name="drop">Inserts the objects let go over the list that it takes, in the order they were dragged, from the index where they landed; null for a list that takes no drop.</param>
        /// <param name="isSelected">Whether the item at the index is selected, which colors it as a selected row; null for a list with no selection.</param>
        /// <param name="select">Called for a press on an item that no control in it took, with the press's modifiers, such as Ctrl to add to the selection.</param>
        public void Draw(int count, Action<int> drawItem, string emptyHint = "None", Action<int, int> move = null, Action<int> remove = null, string removeTooltip = "Remove", bool marksFirstRow = false,
                         Func<Object, bool> takes = null, Action<int, IReadOnlyList<Object>> drop = null, Func<int, bool> isSelected = null, Action<int, EventModifiers> select = null) {
            this.isSelected    = isSelected;
            this.select        = select;
            this.drawItem      = drawItem;
            this.move          = move;
            this.remove        = remove;
            this.removeTooltip = removeTooltip;
            this.takes         = drop == null ? null : takes;
            this.drop          = drop;
            var type = Event.current.type;
            if (dropping.Count > 0 && HasDragEnded(type)) {
                dropping.Clear();
                isGliding = true;
                glideTime = EditorApplication.timeSinceStartup;
                RepaintView();
            }

            var isNested = slots.Count > 0;
            if (!isNested) {
                root = this;
                view = CurrentView();
            }

            handleColumn = move == null ? (isNested ? 0 : Section.Inset) : Section.ButtonWidth + 1;
            removeColumn = remove == null ? Section.Inset : Section.ButtonWidth + 1;
            bodyInset    = marksFirstRow && move != null ? (isNested ? NestedBodyInset : BodyInset) : (int) handleColumn;
            var right = marksFirstRow ? Section.Inset : (int) removeColumn;
            // The padding is the content's, where a field's own margin merges into it instead of adding to it, so the rows keep the page's height.
            content = new GUIStyle { padding = new RectOffset(bodyInset, right, (int) Section.RowPadding, (int) Section.RowPadding) };

            listId = GUIUtility.GetControlID(FocusType.Passive);
            if (move == null || dragIndex >= count) dragIndex = -1;
            HandleDrag(count);

            while (itemRects.Count < count) itemRects.Add(default);
            while (offsets.Count < count) offsets.Add(0);
            while (speeds.Count < count) speeds.Add(0);
            Glide(count);

            // Cleared while the items are drawn, so a field or a list inside an item that takes the drag shows by setting it again.
            var isDragOver = IsDragOver(type, count);
            var modeBefore = DragAndDrop.visualMode;
            if (isDragOver) DragAndDrop.visualMode = DragAndDropVisualMode.None;

            SirenixEditorGUI.BeginVerticalList(false, false);

            if (count == 0) {
                // Where an item's first row would start.
                var empty = BeginItem(0);
                GUILayout.BeginVertical(content);
                var hint = EditorGUILayout.GetControlRect();
                hint.xMin = empty.x + handleColumn;
                if (dropping.Count == 0) GUI.Label(hint, emptyHint, SirenixGUIStyles.LeftAlignedGreyMiniLabel);
                GUILayout.EndVertical();
                EndItem();
                EndFrame();
                HandleDrop(type, count, isDragOver, modeBefore);
                DrawGap(count);
                RunPending();
                return;
            }

            for (var i = 0; i < count; i++) {
                if (i == dragIndex) {
                    // The dragged item's place stays laid out but empty; the item is drawn over the list below.
                    SirenixEditorGUI.BeginListItem(false, itemStyle, Color.clear, Color.clear, Color.clear, Color.clear);
                    GUILayoutUtility.GetRect(0, itemRects[i].height);
                    SirenixEditorGUI.EndListItem();
                    continue;
                }

                var matrix = GUI.matrix;
                if (offsets[i] != 0) GUI.matrix = Matrix4x4.Translate(new Vector3(0, offsets[i], 0)) * matrix;

                var itemRect = BeginItem(i);
                DrawItem(new Slot(this, itemRect, i), isNested);
                EndItem();
                Select(i, itemRect);

                GUI.matrix = matrix;
                if (Event.current.type is EventType.Repaint && dragIndex < 0) itemRects[i] = itemRect;
            }

            EndFrame();
            HandleDrop(type, count, isDragOver, modeBefore);
            DrawGap(count);
            DrawDragged(isNested);
            RunPending();
        }

        // The stripe, or the selection's color, drawn on whole pixels so items meet edge to edge.
        private Rect BeginItem(int index) {
            var rect = SirenixEditorGUI.BeginListItem(false, itemStyle, Color.clear, Color.clear, Color.clear, Color.clear);
            if (Event.current.type is not EventType.Repaint) return rect;

            var snapped = PixelUtils.Snap(rect);
            sectionFrames.TryGetValue((root.view, index), out var frames);
            if (isSelected?.Invoke(index) is true) FillAround(snapped, frames, Selected);
            else if (index % 2 is 1) FillAround(snapped, frames, Section.Shade);

            return rect;
        }

        // A press on the item that its controls left: the controls drew first, and one that took the press used it.
        private void Select(int index, Rect itemRect) {
            var current = Event.current;
            if (select == null || current.type is not EventType.MouseDown || current.button is not 0 || !itemRect.Contains(current.mousePosition)) return;

            select(index, current.modifiers);
            current.Use();
            RepaintView();
        }

        // The rect but for the frames: in bands between the frames' tops and bottoms, each filled beside the frames across it.
        private static void FillAround(Rect rect, List<FrameInset> frames, Color color) {
            if (frames is not { Count: > 0 }) {
                EditorGUI.DrawRect(rect, color);
                return;
            }

            var holes = frames.Select(frame => PixelUtils.Snap(frame.In(rect))).Where(hole => hole.Overlaps(rect)).ToList();
            var edges = holes.SelectMany(hole => new[] { hole.yMin, hole.yMax }).Where(y => y > rect.yMin && y < rect.yMax).Append(rect.yMin).Append(rect.yMax).Distinct().OrderBy(y => y).ToList();
            for (var i = 0; i + 1 < edges.Count; i++) {
                var top    = edges[i];
                var bottom = edges[i + 1];
                var x      = rect.xMin;
                foreach (var hole in holes.Where(hole => hole.yMin < bottom && hole.yMax > top).OrderBy(hole => hole.xMin)) {
                    if (hole.xMin > x) EditorGUI.DrawRect(Rect.MinMaxRect(x, top, Mathf.Min(hole.xMin, rect.xMax), bottom), color);
                    x = Mathf.Max(x, hole.xMax);
                }

                if (x < rect.xMax) EditorGUI.DrawRect(Rect.MinMaxRect(x, top, rect.xMax, bottom), color);
            }
        }

        /// <summary>Called by a section as it draws its frame on a repaint, so the item it is in leaves the frame out of its stripe.</summary>
        internal static void NoteSectionFrame(Rect frame) {
            if (slots.Count is 0 || Event.current.type is not EventType.Repaint) return;

            // Only the outermost section in the item: one inside it is inside its frame already.
            var slot   = slots.Peek();
            var inset  = new FrameInset(frame, slot.itemRect);
            if (slot.frames.Any(outer => outer.Contains(frame, slot.itemRect))) return;

            slot.frames.Add(inset);
        }

        // A section's frame by its distance from the item's four edges.
        private readonly struct FrameInset {
            private readonly float left;
            private readonly float top;
            private readonly float right;
            private readonly float bottom;

            public FrameInset(Rect frame, Rect item) {
                left   = frame.xMin - item.xMin;
                top    = frame.yMin - item.yMin;
                right  = item.xMax - frame.xMax;
                bottom = item.yMax - frame.yMax;
            }

            public Rect In(Rect item) => Rect.MinMaxRect(item.xMin + left, item.yMin + top, item.xMax - right, item.yMax - bottom);

            public bool Contains(Rect frame, Rect item) {
                var own = In(item);
                return own.xMin <= frame.xMin && own.yMin <= frame.yMin && own.xMax >= frame.xMax && own.yMax >= frame.yMax;
            }
        }

        private void EndItem() {
            SirenixEditorGUI.EndListItem();
        }

        // The gap a drop opens grows the list at its end, while the items from where the objects land glide down over it.
        // It is laid out on every event, if only as nothing, so the layout and the events after it always agree on the list's rows.
        private void EndFrame() {
            GUILayoutUtility.GetRect(0, gap);
            SirenixEditorGUI.EndVerticalList();
            if (Event.current.type is not EventType.Layout) listRect = GUILayoutUtility.GetLastRect();
        }

        /// <summary>
        /// Puts the drag handle and the x of the item being drawn level with <paramref name="row"/>, its first row, and returns the row clear of them;<br/>
        /// an item's drawer calls it with its first row. Outside a list, or for any later row, it returns the row as it is.<br/>
        /// The row is placed across by the item, not by the layout: Unity's groups with no style, such as Odin's around each property, take the smallest margin<br/>
        /// of what they hold, so a field row would sit a margin further in once its item opens on a list, which has none.
        /// </summary>
        public static Rect MarkFirstRow(Rect row) {
            if (slots.Count is 0 || slots.Peek().isMarked) return row;

            var slot = slots.Peek();
            row = slot.Span(row);
            slot.Mark(row);
            return slot.Clear(row);
        }

        /// <summary>
        /// <see cref="MarkFirstRow(Rect)"/> for a row laid out as a field: split into the label column, returned as <paramref name="prefix"/>, and the value column, returned;<br/>
        /// split where the rows below split, so the value columns line up, then each kept clear of the handle and the x.
        /// </summary>
        public static Rect MarkFirstRow(Rect row, out Rect prefix) {
            if (slots.Count is 0 || slots.Peek().isMarked) return SirenixEditorGUI.PrefixRect(row, out prefix);

            var slot = slots.Peek();
            row = slot.Span(row);
            var value = SirenixEditorGUI.PrefixRect(row, out prefix);
            slot.Mark(row);
            prefix = slot.Clear(prefix);
            return slot.Clear(value);
        }

        // As Odin draws its items: out of hierarchy mode, so a foldout's arrow stays inside the item instead of in the margin.
        // The label column loses the content's inset, less the section's own that it already lost, so the values line up with the page's.
        private void DrawItem(Slot slot, bool isNested) {
            var isRepaint = Event.current.type is EventType.Repaint;
            slots.Push(slot);
            GUIHelper.PushHierarchyMode(false);
            GUIHelper.PushLabelWidth(EditorGUIUtility.labelWidth - (isNested ? bodyInset : bodyInset - Section.Inset));
            // A control in the item may end the event with ExitGUI, as an object field opening its picker does: the stacks still come back,
            // or the next list would take itself for one nested in this item. Unity drops the layout groups itself.
            try {
                GUILayout.BeginVertical(content);
                drawItem(slot.index);
                GUILayout.EndVertical();
            } finally {
                GUIHelper.PopLabelWidth();
                GUIHelper.PopHierarchyMode();
                slots.Pop();
                if (isRepaint) sectionFrames[(root.view, slot.index)] = slot.frames;
            }

            if (!slot.isMarked) slot.Mark(new Rect(slot.itemRect.x, slot.itemRect.y + Section.RowPadding, slot.itemRect.width, EditorGUIUtility.singleLineHeight));
        }

        // The dragged item over the list, where the pointer has taken it, in an area of its own drawn after every other item.
        private void DrawDragged(bool isNested) {
            if (dragIndex < 0) return;

            var place = itemRects[dragIndex];
            var rect  = new Rect(place.x, place.y + dragOffset, place.width, place.height);
            GUILayout.BeginArea(rect);
            SirenixEditorGUI.BeginVerticalList(false, true);
            var itemRect = SirenixEditorGUI.BeginListItem(false, itemStyle);
            DrawItem(new Slot(this, itemRect, dragIndex), isNested);
            SirenixEditorGUI.EndListItem();
            SirenixEditorGUI.EndVerticalList();
            GUILayout.EndArea();

            Section.DrawDragFrame(rect, false);
        }

        // Pressing a handle starts the drag; dragging moves the item with the pointer and picks where it lands; letting go moves it there.
        private void HandleDrag(int count) {
            var current = Event.current;
            switch (current.GetTypeForControl(listId)) {
                case EventType.MouseDrag when GUIUtility.hotControl == listId && dragIndex >= 0:
                    // Kept within the list: past its ends, the item would be drawn over the fields around it, or cut off by them.
                    var place = itemRects[dragIndex];
                    dragOffset = Mathf.Clamp(current.mousePosition.y - pressY, itemRects[0].yMin - place.yMin, itemRects[count - 1].yMax - place.yMax);
                    landIndex  = FindLanding(count);
                    current.Use();
                    break;
                case EventType.MouseUp when GUIUtility.hotControl == listId:
                    GUIUtility.hotControl = 0;
                    var from = dragIndex;
                    var to   = landIndex;
                    dragIndex = -1;
                    Settle();
                    if (from >= 0 && to != from) Later(() => move(from, to));
                    current.Use();
                    break;
            }
        }

        private void StartDrag(int index) {
            GUIUtility.hotControl = listId;
            dragIndex  = landIndex = index;
            pressY     = Event.current.mousePosition.y;
            dragOffset = 0;
            Settle();
            glideTime  = EditorApplication.timeSinceStartup;
            Event.current.Use();
        }

        // Where the dragged item lands: after every other item whose middle its own middle has passed.
        private int FindLanding(int count) {
            var center = itemRects[dragIndex].center.y + dragOffset;
            var index  = 0;
            for (var i = 0; i < count; i++) {
                if (i != dragIndex && itemRects[i].center.y < center) index++;
            }

            return index;
        }

        // Each other item eases toward where it sits once what is dragged lands: for a move, a step up or down by the dragged item's height, or back home;
        // for a drop, down by the gap from where the objects land. The gap eases open with them, and both ease back once a drop leaves the list.
        // A step is taken as the frame is laid out, so the gap laid out and the items drawn over it in that frame are at the same point of the glide.
        private void Glide(int count) {
            if (Event.current.type is not EventType.Layout || (dragIndex < 0 && dropping.Count == 0 && !isGliding)) return;

            var now   = EditorApplication.timeSinceStartup;
            var delta = Mathf.Clamp((float) (now - glideTime), 0, GlideStepMax);
            glideTime = now;

            var height    = dragIndex >= 0 ? itemRects[dragIndex].height : 0;
            var opened    = OpenedGap(count);
            var isSettled = true;
            gap       =  Damp(gap, opened, ref gapSpeed, delta);
            isSettled &= gap == opened;
            for (var i = 0; i < count; i++) {
                if (i == dragIndex) continue;

                var target = dragIndex >= 0
                    ? i > dragIndex && i <= landIndex ? -height : i < dragIndex && i >= landIndex ? height : 0
                    : i >= landIndex ? opened : 0;
                var speed = speeds[i];
                offsets[i] =  Damp(offsets[i], target, ref speed, delta);
                speeds[i]  =  speed;
                isSettled  &= offsets[i] == target;
            }

            isGliding = !isSettled;
            if (isGliding) RepaintView();
        }

        private static int CurrentView() => GUIHelper.CurrentWindow is { } window ? window.GetInstanceID() : 0;

        // The view the list is in, alone: every view of the editor at each frame of a glide would slow the glide itself.
        private static void RepaintView() {
            var window = GUIHelper.CurrentWindow;
            if (window) window.Repaint();
            else InternalEditorUtility.RepaintAllViews();
        }

        // The gap a drop opens once settled: a row, or none on an empty list, whose hint row is the gap.
        private float OpenedGap(int count) => dropping.Count == 0 || count == 0 ? 0 : RowHeight;

        // Where an item settles for the drop as it is now, down by the gap from where the objects land.
        // What the pointer is over is read against it, not against the items gliding there: read midway, an item gliding away
        // would hand the pointer to the gap and back, opening and closing it at every step along their edge.
        private float DropOffset(int index, int count) => index >= landIndex ? OpenedGap(count) : 0;

        private static float Damp(float from, float to, ref float speed, float delta) {
            var value = Mathf.SmoothDamp(from, to, ref speed, GlideTime, Mathf.Infinity, delta);
            if (Mathf.Abs(to - value) >= .5f || Mathf.Abs(speed) >= 10) return value;

            speed = 0;
            return to;
        }

        // Every item home at once, as when a move or a drop has just landed, so the items take the new order where they stand.
        private void Settle() {
            gap       = 0;
            gapSpeed  = 0;
            isGliding = false;
            for (var i = 0; i < offsets.Count; i++) offsets[i] = 0;
            for (var i = 0; i < speeds.Count; i++) speeds[i] = 0;
        }

        /// <summary>
        /// Whether a drag from elsewhere is over, as far as an event of this view tells: a drag that ends in another view, or is given up there,
        /// sends this one nothing more, so a list or section lit for it learns from the pointer's next plain event, or a repaint once the drag holds nothing.
        /// </summary>
        public static bool HasDragEnded(EventType type) {
            return type is EventType.MouseMove or EventType.MouseDown or EventType.MouseUp or EventType.MouseLeaveWindow
                || type is EventType.Repaint && DragAndDrop.objectReferences.Length == 0;
        }

        // A drag from elsewhere over a list that takes drops, and not over an item's content, which is its fields'.
        // The rects are the last event's, as the list is not laid out yet; an item is where it settles for the drop.
        private bool IsDragOver(EventType type, int count) {
            var pointer = Event.current.mousePosition;
            if (takes == null || type is not (EventType.DragUpdated or EventType.DragPerform) || !listRect.Contains(pointer)) return false;

            for (var i = 0; i < count && i < itemRects.Count; i++) {
                var item    = itemRects[i];
                var offset  = DropOffset(i, count);
                var content = Rect.MinMaxRect(item.xMin + handleColumn, item.yMin + offset + Section.RowPadding, item.xMax - removeColumn, item.yMax + offset - Section.RowPadding);
                if (content.Contains(pointer)) return false;
            }

            return true;
        }

        // Once the items are drawn, so a field or a list inside an item takes the drag first: one that did set the visual mode or used the event,
        // and this list closes its gap. Otherwise, the objects land after every item whose middle is above the pointer;
        // let go, they are inserted there and the gap is gone at once, the items taking its place.
        private void HandleDrop(EventType type, int count, bool isDragOver, DragAndDropVisualMode modeBefore) {
            if (takes == null) return;
            if (type is not (EventType.DragUpdated or EventType.DragPerform or EventType.DragExited)) return;

            var current   = Event.current;
            var isClaimed = current.type is EventType.Used || DragAndDrop.visualMode is not DragAndDropVisualMode.None;
            var taken     = !isDragOver || isClaimed ? null : DragAndDrop.objectReferences.Where(takes).ToList();
            if (isDragOver && !isClaimed && taken.Count == 0) DragAndDrop.visualMode = modeBefore;
            if (!isGliding) glideTime = EditorApplication.timeSinceStartup;
            if (taken is not { Count: > 0 }) {
                dropComing = -1;
                if (dropping.Count == 0) return;

                dropping.Clear();
                isGliding = true;
                RepaintView();
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            // Read against where the items settle for the gap as it is, so the pointer in the gap keeps it where it is.
            var landing = 0;
            for (var i = 0; i < count; i++) {
                if (itemRects[i].center.y + DropOffset(i, count) < current.mousePosition.y) landing++;
            }

            landIndex = landing;

            var isOpen = dropping.Count > 0;
            var now    = EditorApplication.timeSinceStartup;
            if (!isOpen && dropComing < 0) dropComing = now;
            dropping.Clear();
            if (type is EventType.DragPerform) {
                DragAndDrop.AcceptDrag();
                dropComing = -1;
                Settle();
                drop(landIndex, taken);
            } else if (isOpen || now - dropComing >= DropOpenDelay) {
                dropComing = -1;
                dropping.AddRange(taken);
            }

            current.Use();
            RepaintView();
        }

        // The gap, outlined as a dragged item is, naming the first object that lands in it and how many more follow; a drop on an empty list takes its one row.
        private void DrawGap(int count) {
            if (dropping.Count == 0 || Event.current.type is not EventType.Repaint) return;

            var rect = count == 0 ? listRect : new Rect(listRect.x, landIndex < count ? itemRects[landIndex].yMin : listRect.yMax - gap, listRect.width, gap);
            if (rect.height < 1) return;

            Section.DrawDragFrame(rect, true);
            var row = new Rect(rect.x + handleColumn, rect.y + Section.RowPadding, rect.width - handleColumn - removeColumn, EditorGUIUtility.singleLineHeight);
            if (row.yMax <= rect.yMax + .5f) {
                var first = new GUIContent(EditorGUIUtility.ObjectContent(dropping[0], dropping[0].GetType()));
                if (dropping.Count > 1) first.text += $"  +{dropping.Count - 1}";
                using (new EditorGUIUtility.IconSizeScope(new Vector2(EditorGUIUtility.singleLineHeight - 2, EditorGUIUtility.singleLineHeight - 2))) {
                    GUI.Label(row, first, SirenixGUIStyles.LeftAlignedGreyMiniLabel);
                }
            }
        }

        private void Later(Action action) {
            pending += action;
            InternalEditorUtility.RepaintAllViews();
        }

        // On the repaint, once every item is drawn.
        private void RunPending() {
            if (pending == null || Event.current.type is not EventType.Repaint) return;

            var run = pending;
            pending = null;
            run();
            GUIHelper.RequestRepaint();
            InternalEditorUtility.RepaintAllViews();
        }

        /// <summary>
        /// The section's + in <paramref name="slot"/>: with <paramref name="choices"/>, a drop-down of them, which the caller keeps to what the list lacks, the button off once none is left; without, <paramref name="add"/> runs with default.<br/>
        /// The pick arrives after the window drew, so the window repaints for it to show; <paramref name="add"/> opens a folded section if it should.
        /// </summary>
        /// <param name="what">The item as the tooltips name it, such as "format".</param>
        public static void AddButton<T>(Section section, int slot, string what, IReadOnlyCollection<SearchChoice<T>> choices, Action<T> add) {
            var window = GUIHelper.CurrentWindow;
            var none   = choices is { Count: 0 };
            using (new EditorGUI.DisabledScope(none)) {
                if (!section.Button(slot, SdfIconType.Plus, none ? $"Add is off.\nEvery {what} is in the list already." : $"Add {Article(what)} {what}")) return;
            }

            if (choices == null) {
                add(default);
                return;
            }

            ChoiceSearchWindow.Open(section.ButtonRect(slot), "", choices, value => {
                add(value);
                if (window) window.Repaint();
            }, search: choices.Count > 12);
        }

        private static string Article(string word) => word.Length > 0 && "aeiouAEIOU".IndexOf(word[0]) >= 0 ? "an" : "a";

        private sealed class Slot {
            public readonly SectionRows list;
            public readonly Rect        itemRect;
            public readonly int         index;
            public          bool        isMarked;

            // The frames of the sections drawn in the item on this repaint.
            public readonly List<FrameInset> frames = new();

            public Slot(SectionRows list, Rect itemRect, int index) {
                this.list     = list;
                this.itemRect = itemRect;
                this.index    = index;
            }

            // The first row across the item, from where its body starts to its inset on the right, as a field row sits in it with nothing else beside.
            public Rect Span(Rect row) {
                row.xMin = itemRect.x + list.bodyInset;
                row.xMax = itemRect.xMax - Section.Inset;
                return row;
            }

            // A part of the first row, kept clear of the handle's column and the x's.
            public Rect Clear(Rect part) {
                part.xMin = Mathf.Max(part.xMin, itemRect.x + list.handleColumn);
                part.xMax = Mathf.Min(part.xMax, itemRect.xMax - list.removeColumn);
                return part;
            }

            // The handle and the x sit in squares of one size at the item's two ends, centered on the row, in line with the bar's buttons.
            public void Mark(Rect row) {
                isMarked = true;

                var line = new Rect(itemRect.x, row.center.y - Section.Height / 2, itemRect.width, Section.Height);
                if (list.move != null) {
                    var handle  = Section.RowHandle(line);
                    var lit     = HoverUtils.Track(handle) && GUI.enabled;
                    var current = Event.current;
                    if (current.type is EventType.MouseDown && current.button is 0 && handle.Contains(current.mousePosition)) list.StartDrag(index);
                    if (current.type is EventType.Repaint) {
                        EditorGUIUtility.AddCursorRect(handle, MouseCursor.Pan);
                        Section.Icon(handle, SdfIconType.List, lit || list.dragIndex == index);
                    }
                }

                if (list.remove == null) return;

                var button = Section.RowButton(line, 0);
                var owner  = list;
                var at     = index;
                if (Section.IconButton(button, SdfIconType.X, list.removeTooltip)) owner.Later(() => owner.remove(at));
            }
        }
    }
}