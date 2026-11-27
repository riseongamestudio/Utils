using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RiseOn.Utils.Editor.SearchWindow {
    public abstract partial class SearchWindow {
        private const double MoveDuration = 0.15;

        // How far a press moves before it drags rather than picks.
        private const float DragThreshold = 4f;

        private Func<SearchNode, bool>   canMove;
        private Action<List<SearchNode>> onReordered;

        // The row pressed, on a row that moves: a release without a drag picks it.
        private SearchNode pressed;
        private float      pressedAt;
        private bool       isDragging;
        private float      dragShift;

        // Each row sliding, or held aside while a dragged row passes it, by its node.
        private readonly Dictionary<SearchNode, Slide> slides = new();
        private IVisualElementScheduledItem             slider;

        private sealed class Slide {
            public float  From;
            public float  To;
            public float  Now;
            public double Start;
        }

        /// <summary>
        /// Lets the rows <paramref name="movable"/> says may move be dragged up and down among each other: the rows between make way as the dragged row passes them, and
        /// once it is let go <paramref name="reordered"/> gets those nodes in their new order, first as the tree lists them, to put its folder's children in; each row then
        /// slides into its place.<br/>
        /// A click on such a row with no drag picks it as before, as the button comes up rather than as it goes down.<br/>
        /// For a list shown whole, with no search field. Call before <see cref="Show"/>.
        /// </summary>
        protected void AllowReorder(Func<SearchNode, bool> movable, Action<List<SearchNode>> reordered) {
            canMove     = movable;
            onReordered = reordered;
        }

        /// <summary>
        /// Shows the open folder's children in their order now, after a change to it made in place, such as a pick that moves its row: each row slides from where it shows to its new place.
        /// </summary>
        protected void MoveRows() {
            var section = ActiveSection;
            if (page == null || section is not { IsBuilding: false }) return;

            // Where each row shows now, from the top of the list.
            var shown = new Dictionary<SearchNode, float>();
            for (var i = 0; i < page.Items.Count; i++) shown[page.Items[i]] = page.VisualOf(i) * ItemHeight + ShiftOf(page.Items[i]);

            var selected = TryGetSelected(out var item) ? item : null;
            RefreshSection(section);
            ApplyFilter();
            page.Items.Clear();
            page.Items.AddRange(section.FilteredItems);
            page.SelectedIndex    = selected == null ? -1 : page.Items.IndexOf(selected);
            section.SelectedIndex = page.SelectedIndex;

            slides.Clear();
            var now = EditorApplication.timeSinceStartup;
            for (var i = 0; i < page.Items.Count; i++) {
                if (!shown.TryGetValue(page.Items[i], out var y)) continue;

                var offset = y - page.VisualOf(i) * ItemHeight;
                if (Mathf.Abs(offset) > .01f) slides[page.Items[i]] = new Slide { From = offset, To = 0, Now = offset, Start = now };
            }

            page.List.RefreshItems();
            StartSliding();
        }

        private bool IsMovable(SearchNode node) {
            return canMove != null && node != null && node.DisabledReason == null && canMove(node);
        }

        // How far a row shows from its place: the dragged one where the pointer has it, the others where they slide.
        private float ShiftOf(SearchNode node) {
            if (isDragging && node == pressed) return dragShift;

            return slides.TryGetValue(node, out var slide) ? slide.Now : 0f;
        }

        // A press on a row that moves: it is held until the pointer moves far enough to drag it, or comes up to pick it.
        private void Press(VisualElement row, PointerDownEvent evt, int index) {
            pressed    = page.Items[index];
            pressedAt  = evt.position.y;
            isDragging = false;
            dragShift  = 0f;
            row.CapturePointer(evt.pointerId);
            evt.StopPropagation();
            Select(index, scrollTo: false);
        }

        private void Drag(PointerMoveEvent evt) {
            if (pressed == null) return;

            var shift = evt.position.y - pressedAt;
            if (!isDragging && Mathf.Abs(shift) < DragThreshold) return;

            isDragging = true;
            var from  = page.VisualOf(page.Items.IndexOf(pressed));
            var slots = MovableSlots();
            dragShift = Mathf.Clamp(shift, (slots.Min() - from) * ItemHeight, (slots.Max() - from) * ItemHeight);
            var to = Mathf.Clamp(Mathf.RoundToInt(from + dragShift / ItemHeight), slots.Min(), slots.Max());

            // The rows between the dragged row's place and the slot under it step aside by a row, toward the place it left.
            var now = EditorApplication.timeSinceStartup;
            for (var i = 0; i < page.Items.Count; i++) {
                var node = page.Items[i];
                if (node == pressed || !IsMovable(node)) continue;

                var slot   = page.VisualOf(i);
                var aside  = slot > from && slot <= to ? -ItemHeight : slot < from && slot >= to ? ItemHeight : 0f;
                var exists = slides.TryGetValue(node, out var slide);
                if (exists && Mathf.Approximately(slide.To, aside)) continue;
                if (!exists && aside == 0f) continue;

                var at = exists ? slide.Now : 0f;
                slides[node] = new Slide { From = at, To = aside, Now = at, Start = now };
            }

            evt.StopPropagation();
            ApplyShifts();
            StartSliding();
        }

        // Up from a press: a pick with no drag; after one, the order the rows show in.
        private void Release(VisualElement row, int pointerId) {
            if (pressed == null) return;

            var node = pressed;
            if (row.HasPointerCapture(pointerId)) row.ReleasePointer(pointerId);
            if (!isDragging) {
                pressed = null;
                SelectItem(node);
                return;
            }

            // The dragged row stays where it shows, for MoveRows to slide it into its slot from there.
            slides[node] = new Slide { From = dragShift, To = dragShift, Now = dragShift, Start = EditorApplication.timeSinceStartup };
            isDragging = false;
            pressed    = null;

            var order = page.Items.Where(IsMovable).OrderBy(item => page.VisualOf(page.Items.IndexOf(item)) * ItemHeight + ShiftOf(item)).ToList();
            if (page.IsReversed) order.Reverse();
            onReordered?.Invoke(order);
            MoveRows();
        }

        private List<int> MovableSlots() {
            var slots = new List<int>();
            for (var i = 0; i < page.Items.Count; i++) {
                if (IsMovable(page.Items[i])) slots.Add(page.VisualOf(i));
            }

            return slots;
        }

        private void StartSliding() {
            if (slider != null) {
                slider.Resume();
                return;
            }

            slider = rootVisualElement.schedule.Execute(StepSlides).Every(0);
        }

        // Every slide a step on, eased out; a row back in its place is done.
        private void StepSlides() {
            var now = EditorApplication.timeSinceStartup;
            foreach (var (node, slide) in slides.ToList()) {
                var t = Mathf.Clamp01((float) ((now - slide.Start) / MoveDuration));
                slide.Now = Mathf.Lerp(slide.From, slide.To, 1f - Mathf.Pow(1f - t, 3f));
                if (t >= 1f && slide.To == 0f && !(isDragging && node == pressed)) slides.Remove(node);
            }

            ApplyShifts();
            var moving = slides.Values.Any(slide => slide.Now != slide.To);
            if (!moving && !isDragging) slider?.Pause();
        }

        private void ApplyShifts() {
            if (page?.List == null) return;

            page.List.Query(className: Uss.Row).ForEach(row => {
                if (row.userData is int index && index < page.Items.Count) row.style.translate = new Translate(0, ShiftOf(page.Items[index]));
            });
        }
    }
}