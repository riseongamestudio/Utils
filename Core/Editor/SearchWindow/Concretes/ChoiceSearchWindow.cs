using System;
using System.Collections.Generic;
using UnityEngine;

namespace RiseOn.Utils.Editor.SearchWindow {
    /// <summary>
    /// Picks one of a list of <see cref="SearchChoice{T}"/> made in code, each at a path whose <c>/</c> make folders, such as <c>Transform/localScale</c>.<br/>
    /// Folders and choices keep the order they are given in. A choice with a disabled reason is listed grayed out and cannot be picked,<br/>
    /// and a choice whose value is null is the None row. It opens as a drop-down by default, as wide as the button and as tall as its rows.<br/>
    /// A pick made in steps shows each next list in the same window with <see cref="Present{T}"/>, from the callback of the step before.<br/>
    /// <see cref="OpenToggles{T}"/> picks several instead: each pick toggles its choice, ticked while on, and the window stays open for more.
    /// </summary>
    public class ChoiceSearchWindow : SearchWindow {
        private static readonly Vector2 defaultSize = new(250f, 360f);

        private static Texture2D checkIcon;

        private string           label;
        private Func<SearchNode> build;

        // In a window of toggles, whether a node is on; null in a window that picks one.
        private Func<SearchNode, bool> isOn;
        private SearchNode             tree;

        /// <param name="btnRect">The button or field the window opens under, in the current GUI space; call it while the GUI draws.</param>
        /// <param name="choices">Enumerated once the window is on screen, so a long list does not hold up the click.</param>
        /// <param name="onSelected">Gets the value of the choice picked.</param>
        /// <param name="onCanceled">Runs when the window closes with nothing picked.</param>
        /// <param name="selected">The value the field holds now, highlighted and scrolled to when the window opens, its folders opened; null highlights nothing.</param>
        public static ChoiceSearchWindow Open<T>(
            Rect btnRect
          , string title
          , IEnumerable<SearchChoice<T>> choices
          , Action<T> onSelected
          , Action onCanceled = null
          , SearchWindowMode mode = SearchWindowMode.DropDown
          , object selected = null) {
            var window = CreateInstance<ChoiceSearchWindow>();
            window.Prepare(title, choices);
            if (selected != null) window.PreselectOnOpen(selected);
            window.Show(btnRect, title, ToCallback(onSelected), defaultSize, mode: mode, onCanceled: onCanceled);
            return window;
        }

        /// <summary>
        /// Opens a window whose picks toggle: picking a choice calls <paramref name="onToggled"/> with it, then the window stays open,<br/>
        /// each choice ticked while <paramref name="isOn"/> says it is on, asked again after every toggle. It closes as any drop-down does.
        /// </summary>
        public static ChoiceSearchWindow OpenToggles<T>(
            Rect btnRect
          , string title
          , IEnumerable<SearchChoice<T>> choices
          , Func<T, bool> isOn
          , Action<T> onToggled
          , SearchWindowMode mode = SearchWindowMode.DropDown) {
            var window = CreateInstance<ChoiceSearchWindow>();
            window.isOn = node => node.Data is T value && isOn(value);
            window.Prepare(title, choices);
            window.Show(btnRect, title, node => {
                if (node.Data is T value) onToggled?.Invoke(value);
                window.KeepOpen();
                window.Tick(window.tree);
                window.RefreshRows();
            }, defaultSize, mode: mode);
            return window;
        }

        /// <summary>
        /// Lists other choices in this window, keeping it open: call it from the callback of the step before.
        /// </summary>
        public void Present<T>(string title, IEnumerable<SearchChoice<T>> choices, Action<T> onSelected) {
            Prepare(title, choices);
            ShowNext(title, ToCallback(onSelected));
        }

        protected override void RegisterSections() {
            var builder = build;
            AddSection(label, ctx => ctx.Complete(builder()));
        }

        private void Prepare<T>(string title, IEnumerable<SearchChoice<T>> choices) {
            label = title;
            build = () => {
                tree = BuildTree(title, choices);
                Tick(tree);
                return tree;
            };
        }

        // Every choice ticked or not, as it is now on or off; nothing in a window that picks one.
        private void Tick(SearchNode node) {
            if (isOn == null || node?.Children == null) return;

            foreach (var child in node.Children) {
                if (child.HasChildren) {
                    Tick(child);
                    continue;
                }

                // Off, a choice keeps the room of its tick, so ticking one does not move every label.
                child.Icon        = isOn(child) ? CheckIcon() : null;
                child.IconLoader ??= NoIcon;
            }
        }

        private static Texture2D NoIcon() {
            return null;
        }

        // A tick drawn once, in the same gray as the None row's cross.
        private static Texture2D CheckIcon() {
            if (checkIcon != null) return checkIcon;

            checkIcon = new Texture2D(16, 16) { hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Color32[16 * 16];
            var ink = new Color32(200, 200, 200, 255);
            foreach (var (x, y) in Stroke(3, 8, 6, 5)) pixels[y * 16 + x] = ink;
            foreach (var (x, y) in Stroke(6, 5, 12, 11)) pixels[y * 16 + x] = ink;
            checkIcon.SetPixels32(pixels);
            checkIcon.Apply();
            return checkIcon;

            // A diagonal two pixels thick from one point to the other, in texture rows counted from the bottom.
            static IEnumerable<(int, int)> Stroke(int x0, int y0, int x1, int y1) {
                var steps = Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0));
                for (var i = 0; i <= steps; i++) {
                    var x = x0 + (x1 - x0) * i / steps;
                    var y = y0 + (y1 - y0) * i / steps;
                    yield return (x, y);
                    yield return (x, y + 1);
                }
            }
        }

        // The None row carries no data, which unboxes to nothing for a value type.
        private static Action<SearchNode> ToCallback<T>(Action<T> onSelected) {
            return node => onSelected?.Invoke(node.Data is T value ? value : default);
        }

        private static SearchNode BuildTree<T>(string title, IEnumerable<SearchChoice<T>> choices) {
            var root    = new SearchNode { Label = title };
            var iconned = new HashSet<SearchNode>();
            foreach (var choice in choices) {
                var slash = choice.Path.LastIndexOf('/');
                var name  = choice.Path[(slash + 1)..];
                var node  = choice.IsNone || choice.Value == null
                    ? ConstructNoneNode(name)
                    : new SearchNode { Label = name, LabelSearch = choice.LabelSearch, Data = choice.Value, IconLoader = choice.Icon };
                node.DisabledReason = choice.DisabledReason;

                var folder = root.AddInFolder(slash < 0 ? string.Empty : choice.Path[..slash], node);
                if (choice.FolderIcon == null || folder == root || !iconned.Add(folder)) continue;

                // Loaded when the folder's row first shows, as the choices' own icons are.
                folder.Icon       = null;
                folder.IconLoader = choice.FolderIcon;
            }

            return root;
        }
    }
}