using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace RiseOn.Utils.Editor.SearchWindow {
    /// <summary>
    /// Window that browses and searches trees of <see cref="SearchNode"/>, one tab per section.<br/>
    /// Built with UI Toolkit: the list only creates rows for what is on screen, so long lists stay fast.<br/>
    /// Opens as an aux window, like Unity's own object picker: it moves by its title bar, resizes by its edges, and remembers its size per window type.<br/>
    /// Subclasses add sections in <see cref="RegisterSections"/> and open the window with <see cref="Show"/>.
    /// </summary>
    public abstract class SearchWindow : EditorWindow {
        private const float  ItemHeight        = 20f;
        private const double AnimationDuration = 0.4;
        private const string StyleSheetPath    = "SearchWindow/SearchWindow.uss"; // relative to this assembly's asmdef

        private static readonly Vector2 minWindowSize = new(250f, 200f);

        private static Texture2D  cachedNoneIcon;
        private static StyleSheet cachedStyleSheet;

        private readonly List<SectionState>  sections        = new();
        private readonly List<ToolbarToggle> tabs            = new();
        private readonly List<VisualElement> tabProgressBars = new();
        private readonly List<ScoredNode>    scoredNodes     = new();

        private int                activeSectionIndex;
        private string             searchText = string.Empty;
        private Action<SearchNode> onItemSelected;
        private bool               isInSearchMode;

        // What to highlight when the window opens: the value the field holds now (null highlights None).
        private object initialData;
        private bool   isInitialDataPending;

        // Set once the user types, switches tab or moves through the tree; a section that finishes building later no
        // longer pulls the window over to show the initial value.
        private bool userTookOver;

        // Where the pointer last was, in panel space, so a scroll can move the highlight to the row now under it.
        private Vector2 lastPointerPosition;
        private bool    isPointerInside;

        private ToolbarSearchField searchField;
        private VisualElement      content;
        private ProgressBar        buildProgressBar;
        private Page               page;

        private bool                        isAnimating;
        private Page                        outgoingPage;
        private IVisualElementScheduledItem animation;

        private SectionState ActiveSection => activeSectionIndex < sections.Count ? sections[activeSectionIndex] : null;

        private string SizePrefKey => $"RiseOn.SearchWindow.{GetType().Name}";
        private string TabPrefKey  => $"RiseOn.SearchWindow.{StateKey}.Tab";

        /// <summary>Key the last open tab is remembered under. Override to remember it per filter, for example.</summary>
        protected virtual string StateKey => GetType().Name;

        protected abstract void RegisterSections();

        /// <summary>
        /// Highlights the node whose <see cref="SearchNode.Data"/> equals <paramref name="data"/> once its section is built, opening the folders above it and switching to its tab.<br/>
        /// Null highlights the None row.<br/>
        /// Call before <see cref="Show"/>.
        /// </summary>
        protected void PreselectOnOpen(object data) {
            initialData          = data;
            isInitialDataPending = true;
        }

        /// <param name="defaultSize">Size of the first open. Later opens reuse whatever size the window was left at.</param>
        protected void Show(
            Rect btnRect
          , string headerText
          , Action<SearchNode> onItemSelected
          , Vector2 defaultSize
          , string searchText = "") {
            this.onItemSelected = onItemSelected;
            this.searchText     = searchText ?? string.Empty;

            sections.Clear();
            RegisterSections();

            var lastTab = EditorPrefs.GetString(TabPrefKey, null);
            activeSectionIndex = Math.Max(0, sections.FindIndex(section => section.Label == lastTab));

            titleContent = new GUIContent(headerText);
            minSize      = minWindowSize;
            position     = PlaceUnderButton(GUIUtility.GUIToScreenRect(btnRect), LoadSize(defaultSize));
            ShowAuxWindow();
        }

        protected void AddSection(string label, Action<SectionBuildContext> builder) {
            var state = new SectionState { Label = label };
            sections.Add(state);
            EditorApplication.delayCall += () => {
                if (this != null) builder(new SectionBuildContext(state, this));
            };
        }

        public static SearchNode ConstructNoneNode(string label = "None") {
            if (cachedNoneIcon == null) {
                cachedNoneIcon = new Texture2D(16, 16) { hideFlags = HideFlags.HideAndDontSave };
                var pixels = new Color32[16 * 16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 0, 0, 0);

                for (int i = 3; i < 13; i++) {
                    int i17 = i * 17;
                    pixels[i17]     = new Color32(180, 180, 180, 255);
                    pixels[i17 + 1] = new Color32(180, 180, 180, 200);
                    pixels[i17 - 1] = new Color32(180, 180, 180, 200);

                    int i15p15 = i * 15 + 15;
                    pixels[i15p15]     = new Color32(180, 180, 180, 255);
                    pixels[i15p15 + 1] = new Color32(180, 180, 180, 200);
                    pixels[i15p15 - 1] = new Color32(180, 180, 180, 200);
                }

                cachedNoneIcon.SetPixels32(pixels);
                cachedNoneIcon.Apply();
            }

            return new SearchNode { Label = label, Icon = cachedNoneIcon };
        }

        // Clicking anywhere else closes it, so a pick never lands on an object the Inspector has stopped showing.
        // Moving and resizing through the title bar and edges keep the focus.
        private void OnLostFocus() {
            Close();
        }

        private void OnDestroy() {
            SaveSize();
            if (sections.Count > 1 && ActiveSection != null) EditorPrefs.SetString(TabPrefKey, ActiveSection.Label);

            animation?.Pause();
            if (cachedNoneIcon != null) DestroyImmediate(cachedNoneIcon);
        }

        #region Placement

        /// <summary>
        /// Under the button, or above it when there is no room below.<br/>
        /// Kept inside the main editor window only when the button is in it: Unity exposes no bounds for a floating window on another display.
        /// </summary>
        private static Rect PlaceUnderButton(Rect button, Vector2 size) {
            var rect   = new Rect(button.x, button.yMax, size.x, size.y);
            var bounds = EditorGUIUtility.GetMainWindowPosition();
            if (!bounds.Contains(button.center)) return rect;

            if (rect.yMax > bounds.yMax && button.y - size.y >= bounds.y) rect.y = button.y - size.y;
            rect.x = Mathf.Clamp(rect.x, bounds.x, Mathf.Max(bounds.x, bounds.xMax - size.x));
            return rect;
        }

        private Vector2 LoadSize(Vector2 defaultSize) {
            var size = new Vector2(
                EditorPrefs.GetFloat($"{SizePrefKey}.Width", defaultSize.x),
                EditorPrefs.GetFloat($"{SizePrefKey}.Height", defaultSize.y));

            return Vector2.Max(size, minWindowSize);
        }

        private void SaveSize() {
            if (position.width <= 0f || position.height <= 0f) return;

            EditorPrefs.SetFloat($"{SizePrefKey}.Width", position.width);
            EditorPrefs.SetFloat($"{SizePrefKey}.Height", position.height);
        }

        #endregion

        #region Sections

        internal void OnSectionProgress(SectionState state) {
            if (content == null) return;

            UpdateTabs();
            if (state == ActiveSection) buildProgressBar.value = state.BuildProgress;
        }

        internal void OnSectionReady(SectionState state) {
            state.NavigationStack.Clear();
            if (state.Root != null) state.NavigationStack.Push(state.Root);

            RefreshSection(state);

            state.PickableCount = 0;
            foreach (var node in state.AllFlattened) {
                if (node.Data != null) state.PickableCount++;
            }

            // A real object pulls the window over to its tab. None sits in every tab, so it is highlighted in each and
            // moves nothing.
            if (isInitialDataPending && RevealInitialData(state) && initialData != null) {
                isInitialDataPending = false;
                if (!userTookOver) activeSectionIndex = sections.IndexOf(state);
            }

            // The window may not have built its UI yet; CreateGUI shows the section then.
            if (content == null) return;

            if (state == ActiveSection) {
                ApplyFilter();
                RefreshView(restoreScroll: true);
            } else {
                UpdateTabs();
            }
        }

        /// <summary>Opens the folders above the initial value in this section and selects it; false when it is not here.</summary>
        private bool RevealInitialData(SectionState state) {
            if (state.Root == null || !string.IsNullOrWhiteSpace(searchText)) return false;

            var ancestors = new List<SearchNode>();
            if (!FindLeaf(state.Root, initialData, ancestors, out var index)) return false;

            foreach (var ancestor in ancestors) state.NavigationStack.Push(ancestor);
            RefreshSection(state);
            state.SelectedIndex = index;
            state.ScrollOffset  = 0f;
            state.RevealPending = true;
            return true;
        }

        private static bool FindLeaf(SearchNode parent, object data, List<SearchNode> ancestors, out int index) {
            index = -1;
            var children = parent.Children;
            if (children == null) return false;

            for (var i = 0; i < children.Count; i++) {
                var child = children[i];
                if (child == null) continue;

                if (!child.HasChildren) {
                    if (data == null ? child.Data == null : Equals(child.Data, data)) {
                        index = i;
                        return true;
                    }

                    continue;
                }

                // None sits at the top level, so there is nothing to look for inside folders.
                if (data == null) continue;

                ancestors.Add(child);
                if (FindLeaf(child, data, ancestors, out index)) return true;

                ancestors.RemoveAt(ancestors.Count - 1);
            }

            return false;
        }

        private static void RefreshSection(SectionState state) {
            state.CurrentItems.Clear();
            if (state.NavigationStack.Count > 0) {
                var current = state.NavigationStack.Peek();
                if (current.Children != null) state.CurrentItems.AddRange(current.Children);
            }

            // A search always covers the whole tree, whatever level is open.
            state.AllFlattened.Clear();
            if (state.Root != null) FlattenTree(state.Root, state.AllFlattened);
        }

        private static void FlattenTree(SearchNode node, List<SearchNode> result) {
            if (node?.Children == null) return;

            foreach (var child in node.Children) {
                if (child == null) continue;

                result.Add(child);
                if (child.HasChildren) FlattenTree(child, result);
            }
        }

        #endregion

        #region Search and navigation

        private void OnSearchChanged(string text) {
            searchText   = text ?? string.Empty;
            userTookOver = true;

            // Start from the top so the best match is the one highlighted.
            if (ActiveSection != null) ActiveSection.SelectedIndex = -1;

            FinishAnimation();
            ApplyFilter();
            RefreshView();
        }

        private void ApplyFilter() {
            var section = ActiveSection;
            if (section == null || section.IsBuilding) return;

            section.FilteredItems.Clear();
            if (string.IsNullOrWhiteSpace(searchText)) {
                isInSearchMode = false;
                section.FilteredItems.AddRange(section.CurrentItems);
                return;
            }

            isInSearchMode = true;
            var terms = searchText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            scoredNodes.Clear();
            foreach (var node in section.AllFlattened) {
                if (node.TryScore(terms, out var score)) scoredNodes.Add(new ScoredNode(node, score, scoredNodes.Count));
            }

            scoredNodes.Sort(ScoredNode.Comparison);
            foreach (var scored in scoredNodes) section.FilteredItems.Add(scored.Node);

            if (section.FilteredItems.Count > 0 && section.SelectedIndex < 0) {
                section.SelectedIndex = 0;
            } else if (section.SelectedIndex >= section.FilteredItems.Count) {
                section.SelectedIndex = section.FilteredItems.Count - 1;
            }
        }

        private void SelectItem(SearchNode item) {
            onItemSelected?.Invoke(item);
            Close();
        }

        private void NavigateInto(SearchNode item) {
            var section = ActiveSection;
            if (section == null || !item.HasChildren) return;

            userTookOver = true;
            SaveLevelState(section);
            var previousPage = page;
            section.NavigationStack.Push(item);
            OpenLevel(section, previousPage, forward: true);
        }

        private void NavigateBack() {
            var section = ActiveSection;
            if (section == null || section.NavigationStack.Count <= 1) return;

            userTookOver = true;
            SaveLevelState(section);
            var previousPage = page;
            section.NavigationStack.Pop();
            OpenLevel(section, previousPage, forward: false);
        }

        private void OpenLevel(SectionState section, Page previousPage, bool forward) {
            searchText = string.Empty;
            searchField.SetValueWithoutNotify(string.Empty);

            RefreshSection(section);
            RestoreLevelState(section);
            ApplyFilter();

            FinishAnimation();
            page = CreatePage();
            content.Add(page.Root);
            RefreshView(restoreScroll: true);
            StartAnimation(previousPage, forward);
        }

        private void SaveLevelState(SectionState section) {
            if (section.NavigationStack.Count == 0) return;

            var current = section.NavigationStack.Peek();
            if (!section.LevelStates.TryGetValue(current, out var state)) {
                state                        = new LevelState();
                section.LevelStates[current] = state;
            }

            state.ScrollOffset  = page.List.Q<ScrollView>().scrollOffset.y;
            state.SelectedIndex = section.SelectedIndex;
        }

        private static void RestoreLevelState(SectionState section) {
            var current = section.NavigationStack.Peek();
            if (section.LevelStates.TryGetValue(current, out var state)) {
                section.ScrollOffset  = state.ScrollOffset;
                section.SelectedIndex = state.SelectedIndex;
            } else {
                section.ScrollOffset  = 0f;
                section.SelectedIndex = -1;
            }
        }

        private void SwitchSection(int index) {
            if (index == activeSectionIndex || sections[index].IsBuilding) return;

            userTookOver = true;
            FinishAnimation();
            if (ActiveSection != null) ActiveSection.ScrollOffset = page.List.Q<ScrollView>().scrollOffset.y;

            activeSectionIndex = index;
            ApplyFilter();
            RefreshView(restoreScroll: true);
        }

        private bool TryGetSelected(out SearchNode item) {
            var index = page.SelectedIndex;
            item = index >= 0 && index < page.Items.Count ? page.Items[index] : null;
            return item != null;
        }

        private void Select(int index, bool scrollTo) {
            var previous = page.SelectedIndex;
            page.SelectedIndex = index;
            if (ActiveSection != null) ActiveSection.SelectedIndex = index;

            if (previous != index && previous >= 0 && previous < page.Items.Count) page.List.RefreshItem(previous);
            if (index < 0 || index >= page.Items.Count) return;

            page.List.RefreshItem(index);
            if (scrollTo) page.List.ScrollToItem(index);
        }

        #endregion

        #region UI

        private void CreateGUI() {
            // A domain reload recreates the window without its callback, so there is nothing to pick for.
            if (onItemSelected == null) {
                Close();
                return;
            }

            var root = rootVisualElement;
            root.AddToClassList(Uss.Root);

            var styleSheet = LoadStyleSheet();
            if (styleSheet != null) root.styleSheets.Add(styleSheet);

            searchField = new ToolbarSearchField();
            searchField.AddToClassList(Uss.Search);
            searchField.SetValueWithoutNotify(searchText);
            searchField.RegisterValueChangedCallback(evt => OnSearchChanged(evt.newValue));
            root.Add(searchField);

            if (sections.Count > 1) root.Add(CreateTabBar());

            content = new VisualElement();
            content.AddToClassList(Uss.Content);
            root.Add(content);

            buildProgressBar = new ProgressBar { title = "Building...", lowValue = 0f, highValue = 1f };
            buildProgressBar.AddToClassList(Uss.BuildProgress);
            content.Add(buildProgressBar);

            page = CreatePage();
            content.Add(page.Root);

            root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationMoveEvent>(IgnoreNavigation, TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationSubmitEvent>(IgnoreNavigation, TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationCancelEvent>(IgnoreNavigation, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerUpEvent>(_ => FocusSearchField());
            root.RegisterCallback<PointerMoveEvent>(evt => {
                lastPointerPosition = evt.position;
                isPointerInside     = true;
            }, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerLeaveEvent>(_ => isPointerInside = false);

            ApplyFilter();
            RefreshView(restoreScroll: true);
            FocusSearchField();
        }

        /// <summary>
        /// The stylesheet sits next to this script.<br/>
        /// It is found through this assembly's asmdef rather than a fixed "Packages/..." path, so it still loads when the package is renamed or embedded under Assets.
        /// </summary>
        private static StyleSheet LoadStyleSheet() {
            if (cachedStyleSheet != null) return cachedStyleSheet;

            var assemblyName = typeof(SearchWindow).Assembly.GetName().Name;
            var asmdefPath   = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assemblyName);
            if (string.IsNullOrEmpty(asmdefPath)) return null;

            var path = $"{Path.GetDirectoryName(asmdefPath)?.Replace('\\', '/')}/{StyleSheetPath}";
            cachedStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            if (cachedStyleSheet == null) Debug.LogWarning($"[{nameof(SearchWindow)}] No stylesheet at {path}");

            return cachedStyleSheet;
        }

        private VisualElement CreateTabBar() {
            var bar = new Toolbar();
            bar.AddToClassList(Uss.TabBar);

            for (var i = 0; i < sections.Count; i++) {
                var index = i;
                var tab   = new ToolbarToggle();
                tab.AddToClassList(Uss.Tab);
                tab.RegisterValueChangedCallback(evt => {
                    if (evt.newValue) SwitchSection(index);
                    tab.SetValueWithoutNotify(index == activeSectionIndex);
                });

                var progress = new VisualElement { pickingMode = PickingMode.Ignore };
                progress.AddToClassList(Uss.TabProgress);
                tab.Add(progress);

                tabs.Add(tab);
                tabProgressBars.Add(progress);
                bar.Add(tab);
            }

            return bar;
        }

        private void UpdateTabs() {
            for (var i = 0; i < tabs.Count; i++) {
                var section = sections[i];
                tabs[i].text = section.IsBuilding
                    ? $"{section.Label}  {section.BuildProgress:P0}"
                    : $"{section.Label} ({section.PickableCount})";
                tabs[i].SetValueWithoutNotify(i == activeSectionIndex);
                tabs[i].SetEnabled(!section.IsBuilding);

                tabProgressBars[i].style.display = section.IsBuilding ? DisplayStyle.Flex : DisplayStyle.None;
                tabProgressBars[i].style.width   = Length.Percent(section.BuildProgress * 100f);
            }
        }

        /// <param name="restoreScroll">
        /// True when the list shows another level or tab, so it goes back to where that one was scrolled.<br/>
        /// Typing keeps the current scroll and only brings the selected row into view.
        /// </param>
        private void RefreshView(bool restoreScroll = false) {
            if (content == null) return;

            UpdateTabs();

            var section    = ActiveSection;
            var isBuilding = section == null || section.IsBuilding;
            buildProgressBar.style.display = isBuilding ? DisplayStyle.Flex : DisplayStyle.None;
            page.Root.style.display        = isBuilding ? DisplayStyle.None : DisplayStyle.Flex;
            if (isBuilding) {
                buildProgressBar.value = section?.BuildProgress ?? 0f;
                return;
            }

            page.IsInSearchMode = isInSearchMode;
            page.SelectedIndex  = section.SelectedIndex;
            page.Items.Clear();
            page.Items.AddRange(section.FilteredItems);

            var hasBreadcrumb = section.NavigationStack.Count > 1;
            page.Breadcrumb.style.display = hasBreadcrumb ? DisplayStyle.Flex : DisplayStyle.None;
            page.BreadcrumbLabel.text     = hasBreadcrumb ? section.NavigationStack.Peek().Label ?? string.Empty : string.Empty;

            page.List.RefreshItems();

            // Search results and a value revealed on open both bring the selected row into view.
            var scrollToSelection = isInSearchMode || section.RevealPending;
            section.RevealPending = false;

            // The scroll view clamps offsets until it has a size, so both run after the next layout.
            var scrollOffset = section.ScrollOffset;
            var boundPage    = page;
            page.List.schedule.Execute(() => {
                if (restoreScroll) boundPage.List.Q<ScrollView>().scrollOffset = new Vector2(0f, scrollOffset);
                if (scrollToSelection && boundPage.SelectedIndex >= 0) boundPage.List.ScrollToItem(boundPage.SelectedIndex);
            });
        }

        private Page CreatePage() {
            var newPage = new Page { Root = new VisualElement() };
            newPage.Root.AddToClassList(Uss.Page);

            newPage.Breadcrumb = new VisualElement();
            newPage.Breadcrumb.AddToClassList(Uss.Breadcrumb);
            newPage.Breadcrumb.RegisterCallback<PointerDownEvent>(evt => {
                if (evt.button != 0 || isAnimating || newPage != page) return;

                evt.StopPropagation();
                NavigateBack();
            });

            var backIcon = new Image {
                image       = EditorGUIUtility.IconContent("back").image,
                scaleMode   = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore
            };
            backIcon.AddToClassList(Uss.BreadcrumbIcon);
            newPage.Breadcrumb.Add(backIcon);

            newPage.BreadcrumbLabel = new Label { pickingMode = PickingMode.Ignore };
            newPage.BreadcrumbLabel.AddToClassList(Uss.BreadcrumbLabel);
            newPage.Breadcrumb.Add(newPage.BreadcrumbLabel);
            newPage.Root.Add(newPage.Breadcrumb);

            newPage.List = new ListView {
                itemsSource                = newPage.Items,
                fixedItemHeight            = ItemHeight,
                virtualizationMethod       = CollectionVirtualizationMethod.FixedHeight,
                selectionType              = SelectionType.None,
                horizontalScrollingEnabled = false,
                focusable                  = false,
                makeItem                   = () => MakeRow(newPage),
                bindItem                   = (row, index) => BindRow(newPage, row, index)
            };
            newPage.List.AddToClassList(Uss.List);

            // The wheel moves the rows but not the pointer, so no move event follows; pick the new row once it is laid out.
            // Trickle down: the inner ScrollView stops the wheel event after scrolling, so it never bubbles up to here.
            newPage.List.RegisterCallback<WheelEvent>(_ => newPage.List.schedule.Execute(SelectRowUnderPointer), TrickleDown.TrickleDown);
            newPage.Root.Add(newPage.List);

            return newPage;
        }

        private VisualElement MakeRow(Page owner) {
            var row = new VisualElement();
            row.AddToClassList(Uss.Row);

            var icon = new Image { scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            icon.AddToClassList(Uss.RowIcon);
            row.Add(icon);

            var label = new Label { enableRichText = true, pickingMode = PickingMode.Ignore };
            label.AddToClassList(Uss.RowLabel);
            row.Add(label);

            var arrow = new Label("►") { pickingMode = PickingMode.Ignore };
            arrow.AddToClassList(Uss.RowArrow);
            row.Add(arrow);

            // The highlight follows the mouse, like Unity's Add Component popup.
            row.RegisterCallback<PointerMoveEvent>(_ => {
                if (isAnimating || owner != page || row.userData is not int index || index == page.SelectedIndex) return;

                Select(index, scrollTo: false);
            });

            row.RegisterCallback<PointerDownEvent>(evt => {
                if (evt.button != 0 || isAnimating || owner != page) return;
                if (row.userData is not int index || index >= page.Items.Count) return;

                evt.StopPropagation();
                Select(index, scrollTo: false);

                var item = page.Items[index];
                if (evt.clickCount == 2 || !item.HasChildren || isInSearchMode) SelectItem(item);
                else NavigateInto(item);
            });

            return row;
        }

        /// <summary>Moves the highlight to the row under the pointer, so it keeps covering the list's own hover shade.</summary>
        private void SelectRowUnderPointer() {
            if (!isPointerInside || isAnimating || rootVisualElement.panel == null) return;

            for (var element = rootVisualElement.panel.Pick(lastPointerPosition); element != null; element = element.parent) {
                if (!element.ClassListContains(Uss.Row)) continue;

                if (page.List.Contains(element) && element.userData is int index && index != page.SelectedIndex) {
                    Select(index, scrollTo: false);
                }

                return;
            }
        }

        private static void BindRow(Page owner, VisualElement row, int index) {
            var item = owner.Items[index];
            row.userData = index;
            row.tooltip  = item.PlainLabel;
            row.EnableInClassList(Uss.SelectedRow, index == owner.SelectedIndex);

            ((Image)row[0]).image = item.Icon;
            ((Label)row[1]).text = owner.IsInSearchMode && !string.IsNullOrEmpty(item.LabelSearch)
                ? item.LabelSearch
                : item.Label ?? string.Empty;
            row[2].style.display = !owner.IsInSearchMode && item.HasChildren ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnKeyDown(KeyDownEvent evt) {
            if (evt.keyCode is KeyCode.Escape) {
                Consume(evt);
                Close();
                return;
            }

            var section = ActiveSection;
            if (isAnimating || section == null || section.IsBuilding) {
                if (evt.keyCode is KeyCode.UpArrow or KeyCode.DownArrow or KeyCode.Return or KeyCode.KeypadEnter) Consume(evt);
                return;
            }

            switch (evt.keyCode) {
                case KeyCode.UpArrow:
                    userTookOver = true;
                    if (page.Items.Count > 0) Select(page.SelectedIndex <= 0 ? page.Items.Count - 1 : page.SelectedIndex - 1, scrollTo: true);
                    Consume(evt);
                    break;
                case KeyCode.DownArrow:
                    userTookOver = true;
                    if (page.Items.Count > 0) Select(page.SelectedIndex < 0 || page.SelectedIndex >= page.Items.Count - 1 ? 0 : page.SelectedIndex + 1, scrollTo: true);
                    Consume(evt);
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    Consume(evt);
                    if (!TryGetSelected(out var chosen)) break;

                    if (chosen.HasChildren && !isInSearchMode) NavigateInto(chosen);
                    else SelectItem(chosen);
                    break;
                case KeyCode.Backspace:
                case KeyCode.LeftArrow:
                    if (!string.IsNullOrEmpty(searchText)) break;

                    Consume(evt);
                    NavigateBack();
                    break;
                case KeyCode.RightArrow:
                    if (!string.IsNullOrEmpty(searchText) || !TryGetSelected(out var folder) || !folder.HasChildren) break;

                    Consume(evt);
                    NavigateInto(folder);
                    break;
            }
        }

        private void Consume(EventBase evt) {
            evt.StopImmediatePropagation();
            rootVisualElement.focusController?.IgnoreEvent(evt);
        }

        /// <summary>Keeps arrow keys and Enter from moving focus out of the search field.</summary>
        private void IgnoreNavigation(EventBase evt) {
            Consume(evt);
        }

        private void FocusSearchField() {
            rootVisualElement.schedule.Execute(() => searchField?.Q<TextField>()?.Focus());
        }

        private void StartAnimation(Page previousPage, bool forward) {
            isAnimating  = true;
            outgoingPage = previousPage;

            var start     = EditorApplication.timeSinceStartup;
            var direction = forward ? 1f : -1f;
            SetHorizontalOffset(page.Root, direction * 100f);

            animation = rootVisualElement.schedule.Execute(() => {
                if (!isAnimating) return;

                var progress = Mathf.Clamp01((float)((EditorApplication.timeSinceStartup - start) / AnimationDuration));
                var eased    = 1f - Mathf.Pow(1f - progress, 3f);

                SetHorizontalOffset(page.Root, direction * 100f * (1f - eased));
                SetHorizontalOffset(outgoingPage.Root, -direction * 100f * eased);

                if (progress >= 1f) FinishAnimation();
            }).Every(10);
        }

        private void FinishAnimation() {
            if (!isAnimating) return;

            animation?.Pause();
            animation = null;
            outgoingPage?.Root.RemoveFromHierarchy();
            outgoingPage = null;
            SetHorizontalOffset(page.Root, 0f);
            isAnimating = false;
        }

        private static void SetHorizontalOffset(VisualElement element, float percent) {
            element.style.translate = new Translate(Length.Percent(percent), 0f);
        }

        /// <summary>One level of the tree on screen. Navigating creates a new page and slides the old one out.</summary>
        private sealed class Page {
            public readonly List<SearchNode> Items = new();

            public VisualElement Root;
            public VisualElement Breadcrumb;
            public Label         BreadcrumbLabel;
            public ListView      List;
            public int           SelectedIndex = -1;
            public bool          IsInSearchMode;
        }

        /// <summary>A search hit. Best score first; a shorter name wins a tie, then the order of the tree.</summary>
        private readonly struct ScoredNode {
            public static readonly Comparison<ScoredNode> Comparison = Compare;

            public readonly SearchNode Node;
            public readonly long       Score;
            public readonly int        Order;

            public ScoredNode(SearchNode node, long score, int order) {
                Node  = node;
                Score = score;
                Order = order;
            }

            private static int Compare(ScoredNode a, ScoredNode b) {
                var byScore = b.Score.CompareTo(a.Score);
                if (byScore != 0) return byScore;

                var byLength = a.Node.RankLength.CompareTo(b.Node.RankLength);
                return byLength != 0 ? byLength : a.Order.CompareTo(b.Order);
            }
        }

        /// <summary>Class names used in SearchWindow.uss.</summary>
        private static class Uss {
            public const string Root = "search-window";

            public const string Search        = "search-window__search";
            public const string TabBar        = "search-window__tab-bar";
            public const string Tab           = "search-window__tab";
            public const string TabProgress   = "search-window__tab-progress";
            public const string Content       = "search-window__content";
            public const string BuildProgress = "search-window__build-progress";

            public const string Page            = "search-window__page";
            public const string Breadcrumb      = "search-window__breadcrumb";
            public const string BreadcrumbIcon  = "search-window__breadcrumb-icon";
            public const string BreadcrumbLabel = "search-window__breadcrumb-label";
            public const string List            = "search-window__list";

            public const string Row         = "search-window__row";
            public const string SelectedRow = "search-window__row--selected";
            public const string RowIcon     = "search-window__row-icon";
            public const string RowLabel    = "search-window__row-label";
            public const string RowArrow    = "search-window__row-arrow";
        }

        #endregion
    }
}
