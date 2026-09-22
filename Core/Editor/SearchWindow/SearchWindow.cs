using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Utils.Editor.SearchWindow {
    public abstract class SearchWindow : EditorWindow {
        // UI Layout Constants
        private const float HEADER_HEIGHT = 24f;
        private const float SEARCH_HEIGHT = 18f;
        private const float BREADCRUMB_HEIGHT = 22f;
        private const float ITEM_HEIGHT = 20f;
        private const float BORDER_WIDTH = 1f;
        private const float ICON_WIDTH = 16f;
        private const float SCROLLBAR_WIDTH = 13f;
        private const float ANIMATION_DURATION = 0.4f;
        private const float TAB_BAR_HEIGHT = 22f;

        // Custom Theme Colors
        private readonly Color darkSelectionColor = new Color(23f / 255f, 26f / 255f, 9f / 255f, 1f); // #171a09
        private readonly Color lightSelectionColor = new Color(0.8f, 0.8f, 0.8f, 1f);

        // Core State

        private int activeSectionIndex;
        private string searchText = string.Empty;
        private string lastSearchText = string.Empty;
        private bool isInSearchMode;

        private readonly List<SectionState> sections = new();

        private SectionState ActiveSection => sections.Count > 0 && activeSectionIndex < sections.Count ? sections[activeSectionIndex] : null;

        // Search Debounce Implementation
        private const double SEARCH_DEBOUNCE_DELAY = 0.15;

        private double lastSearchInputTime;
        private bool isSearchPending;

        // Interaction & Animation State
        private enum NavAnimDir {
            Forward
          , Backward
        }

        private bool isDragging;
        private Vector2 dragOffset;
        private bool isAnimating;
        private float animationProgress;
        private NavAnimDir animationDirection;
        private List<SearchNode> previousItems = new();
        private Vector2 previousScrollPosition;
        private int previousSelectedIndex = -1;
        private float animationStartTime;
        private string previousBreadcrumbText = string.Empty;
        private bool previousHasBreadcrumb;
        private string headerText = "Search";
        private Action<SearchNode> onItemSelected;

        // Styles Cache
        private GUIStyle headerStyle;
        private GUIStyle breadcrumbStyle;
        private GUIStyle itemStyle;
        private GUIStyle itemSelectedStyle;
        private GUIStyle searchFieldStyle;
        private Texture2D backIcon;
        private Texture2D breadcrumbBgTex;
        private Texture2D breadcrumbHoverTex;
        private static Texture2D cachedNoneIcon;

        protected abstract void RegisterSections();

        protected void Show(
            Rect btnRect
          , string headerText
          , Action<SearchNode> onItemSelected
          , string searchText = "") {
            Setup(headerText, onItemSelected);
            Vector2 screenPos = GUIUtility.GUIToScreenRect(btnRect).position;
            screenPos.y += btnRect.height;
            position    =  new Rect(screenPos, minSize);

            ShowPopup();
            Focus();

            if (!string.IsNullOrEmpty(searchText)) {
                this.searchText = searchText;
                ExecuteFilterNow();
            }
        }

        private void Setup(string headerText, Action<SearchNode> onItemSelected) {
            this.headerText     = headerText;
            this.onItemSelected = onItemSelected;

            sections.Clear();
            activeSectionIndex = 0;

            RegisterSections();
            LoadIcons();
            InitializeStyles();

            wantsMouseMove             =  true;
            wantsMouseEnterLeaveWindow =  true;
            EditorApplication.update   += EditorUpdate;
        }

        private void OnLostFocus() => Close();

        private void OnDestroy() {
            EditorApplication.update -= EditorUpdate;
            EditorApplication.update -= UpdateAnimation;

            if (breadcrumbBgTex != null) DestroyImmediate(breadcrumbBgTex);
            if (breadcrumbHoverTex != null) DestroyImmediate(breadcrumbHoverTex);
            if (cachedNoneIcon != null) DestroyImmediate(cachedNoneIcon);
        }

        protected void AddSection(string label, Action<SectionBuildContext> builder) {
            var state = new SectionState { Label = label };
            sections.Add(state);
            EditorApplication.delayCall += () => {
                if (this != null) builder(new SectionBuildContext(state, this));
            };
        }

        internal void OnSectionReady(SectionState state) {
            state.NavigationStack.Clear();
            if (state.Root != null) {
                state.NavigationStack.Push(state.Root);
            }

            RefreshSection(state);

            if (state == ActiveSection) ExecuteFilterNow();
            Repaint();
        }

        public static SearchNode ConstructNoneNode(string label = "None") {
            if (cachedNoneIcon == null) {
                cachedNoneIcon = new Texture2D(16, 16) { hideFlags = HideFlags.HideAndDontSave };
                var pixels                                        = new Color32[16 * 16];
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

        #region Search Logic & Navigation

        private void EditorUpdate() {
            if (isSearchPending && EditorApplication.timeSinceStartup - lastSearchInputTime > SEARCH_DEBOUNCE_DELAY) {
                isSearchPending = false;
                ExecuteFilterNow();
                Repaint();
            }
        }

        private void QueueFilter() {
            lastSearchInputTime = EditorApplication.timeSinceStartup;
            isSearchPending     = true;
        }

        private void ExecuteFilterNow() {
            if (ActiveSection == null || ActiveSection.IsBuilding) return;

            ActiveSection.FilteredItems.Clear();
            if (string.IsNullOrEmpty(searchText)) {
                isInSearchMode = false;
                ActiveSection.FilteredItems.AddRange(ActiveSection.CurrentItems);
            } else {
                isInSearchMode = true;
                foreach (var node in ActiveSection.AllFlattened) {
                    if (MatchesSearch(node, searchText)) {
                        ActiveSection.FilteredItems.Add(node);
                    }
                }

                if (ActiveSection.FilteredItems.Count > 0 && ActiveSection.SelectedIndex < 0) {
                    ActiveSection.SelectedIndex = 0;
                } else if (ActiveSection.SelectedIndex >= ActiveSection.FilteredItems.Count) {
                    ActiveSection.SelectedIndex = ActiveSection.FilteredItems.Count - 1;
                }
            }
        }

        private bool MatchesSearch(SearchNode item, string search) {
            if (item == null || item.HasChildren) return false;

            bool validLabel       = !string.IsNullOrEmpty(item.Label);
            bool validLabelSearch = !string.IsNullOrEmpty(item.LabelSearch);
            if (!validLabel && !validLabelSearch) return false;

            // Every term must appear in at least one label the node actually has. Folding the validity
            // flags into one && chain let an empty label short-circuit the whole test and pass everything.
            foreach (string term in search.Split(' ', StringSplitOptions.RemoveEmptyEntries)) {
                bool inLabel       = validLabel       && item.Label.Contains(term, StringComparison.OrdinalIgnoreCase);
                bool inLabelSearch = validLabelSearch && item.LabelSearch.Contains(term, StringComparison.OrdinalIgnoreCase);

                if (!inLabel && !inLabelSearch) return false;
            }

            return true;
        }

        private void RefreshSection(SectionState s) {
            s.CurrentItems.Clear();
            if (s.NavigationStack.Count > 0) {
                var current = s.NavigationStack.Peek();
                if (current.Children != null) s.CurrentItems.AddRange(current.Children);
            }

            s.AllFlattened.Clear();
            if (s.NavigationStack.Count > 0) FlattenTree(s.NavigationStack.Last(), s.AllFlattened);
        }

        private void FlattenTree(SearchNode node, List<SearchNode> result) {
            if (node?.Children == null || node.Children.Count == 0) return;
            foreach (var child in node.Children) {
                if (child == null) continue;
                result.Add(child);
                if (child.HasChildren) FlattenTree(child, result);
            }
        }

        private void SelectItem(SearchNode item) {
            onItemSelected?.Invoke(item);
            Close();
        }

        private void NavigateInto(SearchNode item) {
            if (!item.HasChildren || ActiveSection == null) return;

            SaveCurrentState();
            StartAnimation(NavAnimDir.Forward);

            ActiveSection.NavigationStack.Push(item);
            searchText = string.Empty;

            RefreshSection(ActiveSection);
            RestoreState();
            ExecuteFilterNow();
        }

        private void NavigateBack() {
            if (ActiveSection == null || ActiveSection.NavigationStack.Count <= 1) return;

            SaveCurrentState();
            StartAnimation(NavAnimDir.Backward);

            ActiveSection.NavigationStack.Pop();
            searchText = string.Empty;

            RefreshSection(ActiveSection);
            RestoreState();
            ExecuteFilterNow();
        }

        private void SaveCurrentState() {
            if (ActiveSection == null || ActiveSection.NavigationStack.Count == 0) return;
            var current = ActiveSection.NavigationStack.Peek();
            if (!ActiveSection.LevelStates.ContainsKey(current)) {
                ActiveSection.LevelStates[current] = new LevelState();
            }

            ActiveSection.LevelStates[current].ScrollPosition = ActiveSection.ScrollPosition;
            ActiveSection.LevelStates[current].SelectedIndex  = ActiveSection.SelectedIndex;
        }

        private void RestoreState() {
            if (ActiveSection == null || ActiveSection.NavigationStack.Count == 0) return;
            var current = ActiveSection.NavigationStack.Peek();
            if (ActiveSection.LevelStates.TryGetValue(current, out var state)) {
                ActiveSection.ScrollPosition = state.ScrollPosition;
                ActiveSection.SelectedIndex  = state.SelectedIndex;
            } else {
                ActiveSection.ScrollPosition = Vector2.zero;
                ActiveSection.SelectedIndex  = -1;
            }
        }

        #endregion

        #region GUI Rendering & Animation

        private void InitializeStyles() {
            if (headerStyle != null) return;

            headerStyle = new GUIStyle(EditorStyles.label) {
                alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, richText = true, fixedHeight = HEADER_HEIGHT
            };

            breadcrumbBgTex    = CreateColorTexture(new Color(0.3f, 0.3f, 0.3f, 0.3f));
            breadcrumbHoverTex = CreateColorTexture(new Color(0.4f, 0.4f, 0.4f, 0.4f));

            breadcrumbStyle = new GUIStyle(EditorStyles.label) {
                alignment = TextAnchor.MiddleCenter, richText = true, fontStyle = FontStyle.Bold, normal = { background = breadcrumbBgTex, textColor = EditorGUIUtility.isProSkin ? new Color(0.8f, 0.8f, 0.8f) : new Color(0.2f, 0.2f, 0.2f) }, hover = { background = breadcrumbHoverTex }, padding = new RectOffset(4, 4, 2, 2), margin = new RectOffset(0, 0, 0, 0)
            };

            itemStyle         = new GUIStyle(EditorStyles.label) { richText = true };
            itemSelectedStyle = new GUIStyle(itemStyle);
            searchFieldStyle  = new GUIStyle(EditorStyles.toolbarSearchField) { alignment = TextAnchor.MiddleLeft };
        }

        private void LoadIcons() {
            backIcon = EditorGUIUtility.IconContent("back").image as Texture2D
             ?? EditorGUIUtility.IconContent("TreeEditor.Trash").image as Texture2D;
        }

        private Texture2D CreateColorTexture(Color color) {
            var tex = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private void StartAnimation(NavAnimDir direction) {
            if (ActiveSection == null || ActiveSection.NavigationStack.Count == 0) return;

            isAnimating        = true;
            animationDirection = direction;
            animationProgress  = 0f;
            animationStartTime = (float)EditorApplication.timeSinceStartup;

            previousItems          = new List<SearchNode>(ActiveSection.FilteredItems);
            previousScrollPosition = ActiveSection.ScrollPosition;
            previousSelectedIndex  = ActiveSection.SelectedIndex;

            var currentNode = ActiveSection.NavigationStack.Peek();
            previousBreadcrumbText = currentNode.Label ?? string.Empty;
            previousHasBreadcrumb  = ActiveSection.NavigationStack.Count > 1;

            EditorApplication.update -= UpdateAnimation;
            EditorApplication.update += UpdateAnimation;
        }

        private void UpdateAnimation() {
            if (!isAnimating) {
                EditorApplication.update -= UpdateAnimation;
                return;
            }

            float elapsed = (float)EditorApplication.timeSinceStartup - animationStartTime;
            animationProgress = Mathf.Clamp01(elapsed / ANIMATION_DURATION);

            if (animationProgress >= 1f) {
                isAnimating              =  false;
                EditorApplication.update -= UpdateAnimation;
            }

            Repaint();
        }

        private float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        private void OnGUI() {
            if (headerStyle == null) InitializeStyles();

            if (!isAnimating) {
                HandleKeyboardInput();
                HandleMouseSelection();
            }

            DrawBorder();

            var contentRect = new Rect(BORDER_WIDTH, BORDER_WIDTH, position.width - BORDER_WIDTH * 2, position.height - BORDER_WIDTH * 2);
            GUILayout.BeginArea(contentRect);

            DrawHeader();
            DrawSearchBar();
            DrawTabBar();

            if (isAnimating) DrawBreadcrumbAndListAnimated();
            else {
                DrawBreadcrumb();
                DrawItemList();
            }

            GUILayout.EndArea();
            if (Event.current.type == EventType.MouseMove) Repaint();
        }

        private void DrawBorder() {
            EditorGUI.DrawRect(new Rect(0, 0, position.width, BORDER_WIDTH), Color.black);
            EditorGUI.DrawRect(new Rect(0, position.height - BORDER_WIDTH, position.width, BORDER_WIDTH), Color.black);
            EditorGUI.DrawRect(new Rect(0, 0, BORDER_WIDTH, position.height), Color.black);
            EditorGUI.DrawRect(new Rect(position.width - BORDER_WIDTH, 0, BORDER_WIDTH, position.height), Color.black);
        }

        private void DrawHeader() {
            var headerRect = GUILayoutUtility.GetRect(0, HEADER_HEIGHT, GUILayout.ExpandWidth(true));
            int controlID  = GUIUtility.GetControlID(FocusType.Passive);
            var evt        = Event.current;

            switch (evt.GetTypeForControl(controlID)) {
                case EventType.MouseDown:
                    if (headerRect.Contains(evt.mousePosition)) {
                        isDragging            = true;
                        dragOffset            = evt.mousePosition;
                        GUIUtility.hotControl = controlID;
                        evt.Use();
                    }

                    break;
                case EventType.MouseDrag:
                    if (isDragging && GUIUtility.hotControl == controlID) {
                        var np = GUIUtility.GUIToScreenPoint(evt.mousePosition) - dragOffset;
                        position = new Rect(np.x, np.y, position.width, position.height);
                        evt.Use();
                    }

                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID) {
                        isDragging            = false;
                        GUIUtility.hotControl = 0;
                        evt.Use();
                    }

                    break;
            }

            GUI.Box(headerRect, headerText, headerStyle);
        }

        private void DrawSearchBar() {
            EditorGUI.BeginChangeCheck();
            var searchRect = GUILayoutUtility.GetRect(0, SEARCH_HEIGHT, GUILayout.ExpandWidth(true));
            searchRect.x     += 3;
            searchRect.width -= 4;

            // Explicitly set the mouse cursor to the text I-beam when hovering over this rect
            EditorGUIUtility.AddCursorRect(searchRect, MouseCursor.Text);

            GUI.SetNextControlName("SearchField");
            searchText = GUI.TextField(searchRect, searchText, searchFieldStyle);

            if (EditorGUI.EndChangeCheck() && searchText != lastSearchText) {
                QueueFilter();
                lastSearchText = searchText;
            }

            if (Event.current.type == EventType.Repaint && string.IsNullOrEmpty(GUI.GetNameOfFocusedControl())) {
                EditorGUI.FocusTextInControl("SearchField");
            }
        }

        private void DrawTabBar() {
            if (sections.Count <= 1) return;
            GUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(TAB_BAR_HEIGHT));
            for (int i = 0; i < sections.Count; i++) {
                var  s          = sections[i];
                bool isBuilding = s.IsBuilding;

                using (new EditorGUI.DisabledScope(isBuilding)) {
                    // Count nodes that actually have data (ignoring group headers and the "None" node)
                    int candidateCount = s.AllFlattened.Count(n => n.Data != null);

                    // Append the count to the label if it's done building
                    string label    = isBuilding ? $"{s.Label}  {s.BuildProgress:P0}" : $"{s.Label} ({candidateCount})";
                    bool   isActive = (i == activeSectionIndex);

                    if (GUILayout.Toggle(isActive, label, EditorStyles.toolbarButton)) {
                        if (!isActive && !isBuilding) {
                            activeSectionIndex       =  i;
                            isAnimating              =  false;
                            EditorApplication.update -= UpdateAnimation;
                            ExecuteFilterNow();
                            Repaint();
                        }
                    }
                }

                // Draw the building progress bar under the tab if it's still loading
                if (isBuilding) {
                    var rect         = GUILayoutUtility.GetLastRect();
                    var progressRect = new Rect(rect.x, rect.yMax - 2, rect.width * s.BuildProgress, 2);
                    EditorGUI.DrawRect(progressRect, new Color(0.3f, 0.7f, 1f));
                }
            }

            GUILayout.EndHorizontal();
        }

        private void DrawBreadcrumb() {
            if (ActiveSection == null || ActiveSection.NavigationStack.Count <= 1) return;
            var bRect = GUILayoutUtility.GetRect(0, BREADCRUMB_HEIGHT, GUILayout.ExpandWidth(true));
            if (GUI.Button(bRect, new GUIContent(ActiveSection.NavigationStack.Peek().Label ?? "", backIcon), breadcrumbStyle)) {
                NavigateBack();
            }
        }

        private void DrawItemList() {
            if (ActiveSection == null) return;

            float tabBarHeightOffset     = sections.Count > 1 ? TAB_BAR_HEIGHT : 0;
            float breadcrumbHeightOffset = ActiveSection.NavigationStack.Count > 1 ? BREADCRUMB_HEIGHT : 0;

            var listRect = GUILayoutUtility.GetRect(
                0, position.height - HEADER_HEIGHT - SEARCH_HEIGHT - tabBarHeightOffset - breadcrumbHeightOffset - BORDER_WIDTH * 2 - 10,
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)
            );

            if (ActiveSection.IsBuilding) {
                var barRect = new Rect(listRect.x + 20, listRect.y + listRect.height / 2 - 10, listRect.width - 40, 20);
                EditorGUI.ProgressBar(barRect, ActiveSection.BuildProgress, "Building...");
                Repaint();
                return;
            }

            ActiveSection.ScrollPosition = GUI.BeginScrollView(listRect, ActiveSection.ScrollPosition, new Rect(0, 0, listRect.width - 20, ActiveSection.FilteredItems.Count * ITEM_HEIGHT));

            for (int i = 0; i < ActiveSection.FilteredItems.Count; i++) {
                DrawItem(i, ActiveSection.FilteredItems[i], 0f, ActiveSection.SelectedIndex, false);
            }

            GUI.EndScrollView();
        }

        private void DrawItem(int index, SearchNode item, float xOffset, int selectedIdx, bool isInAnimation) {
            if (item == null || ActiveSection == null) return;

            var  itemRect   = new Rect(xOffset, index * ITEM_HEIGHT, position.width - BORDER_WIDTH * 2 - SCROLLBAR_WIDTH, ITEM_HEIGHT);
            bool isSelected = (index == selectedIdx);

            if (isSelected) {
                EditorGUI.DrawRect(itemRect, EditorGUIUtility.isProSkin ? darkSelectionColor : lightSelectionColor);
            }

            if (!string.IsNullOrEmpty(item.Label)) {
                GUI.Label(itemRect, new GUIContent(string.Empty, item.Label));
            }

            var iconRect = new Rect(itemRect.x, itemRect.y + (ITEM_HEIGHT - ICON_WIDTH) / 2, ICON_WIDTH, ICON_WIDTH);
            if (item.Icon != null) GUI.DrawTexture(iconRect, item.Icon);

            var    labelRect    = new Rect(itemRect.x + iconRect.width + 2, itemRect.y, itemRect.width - iconRect.width - 2, itemRect.height);
            string displayLabel = isInSearchMode && !string.IsNullOrEmpty(item.LabelSearch) ? item.LabelSearch : item.Label ?? string.Empty;
            GUI.Label(labelRect, displayLabel, isSelected ? itemSelectedStyle : itemStyle);

            if (!isInSearchMode && item.HasChildren) {
                GUI.Label(new Rect(itemRect.xMax - 14, itemRect.y, 14, itemRect.height), "►");
            }

            if (!isInAnimation && Event.current.type == EventType.MouseDown && itemRect.Contains(Event.current.mousePosition)) {
                ActiveSection.SelectedIndex = index;
                if (Event.current.clickCount == 2 || !item.HasChildren || isInSearchMode) {
                    SelectItem(item);
                    Event.current.Use();
                } else {
                    NavigateInto(item);
                    Event.current.Use();
                }
            }
        }

        private void DrawBreadcrumbAndListAnimated() {
            if (ActiveSection == null || ActiveSection.NavigationStack.Count == 0) return;

            float progress   = EaseOutCubic(animationProgress);
            float totalWidth = position.width - BORDER_WIDTH * 2;
            float curOff, prevOff;

            if (animationDirection == NavAnimDir.Forward) {
                curOff  = totalWidth * (1f - progress);
                prevOff = -totalWidth * progress;
            } else {
                curOff  = -totalWidth * (1f - progress);
                prevOff = totalWidth * progress;
            }

            float tabBarHeightOffset = sections.Count > 1 ? TAB_BAR_HEIGHT : 0;
            float remainH            = position.height - BORDER_WIDTH * 2 - HEADER_HEIGHT - SEARCH_HEIGHT - tabBarHeightOffset;
            var   animRect           = GUILayoutUtility.GetRect(0, remainH, GUILayout.ExpandWidth(true));

            GUI.BeginGroup(animRect);
            GUI.BeginClip(new Rect(0, 0, animRect.width, animRect.height));

            DrawBreadcrumbAndListPanel(prevOff, 0, totalWidth, animRect.height, previousBreadcrumbText, previousHasBreadcrumb, previousItems, previousScrollPosition, previousSelectedIndex, true);

            var curNode = ActiveSection.NavigationStack.Peek();
            DrawBreadcrumbAndListPanel(curOff, 0, totalWidth, animRect.height, curNode.Label ?? "", ActiveSection.NavigationStack.Count > 1, ActiveSection.FilteredItems, ActiveSection.ScrollPosition, ActiveSection.SelectedIndex, true);

            GUI.EndClip();
            GUI.EndGroup();
        }

        private void DrawBreadcrumbAndListPanel(float x, float y, float width, float height, string breadcrumbText, bool hasBreadcrumb, List<SearchNode> items, Vector2 scroll, int selectedIdx, bool isInAnimation = false) {
            GUI.BeginGroup(new Rect(x, y, width, height));
            float curY = 0f;

            if (hasBreadcrumb) {
                var bRect = new Rect(0, curY, width, BREADCRUMB_HEIGHT);
                EditorGUI.DrawRect(bRect, new Color(0.3f, 0.3f, 0.3f, 0.3f));
                GUI.Label(bRect, new GUIContent(breadcrumbText ?? "", backIcon), breadcrumbStyle);
                curY += BREADCRUMB_HEIGHT;
            }

            float cw         = width - SCROLLBAR_WIDTH;
            float ch         = items.Count * ITEM_HEIGHT;
            float viewHeight = height - curY;

            GUI.BeginGroup(new Rect(0, curY, width, viewHeight));
            GUI.BeginGroup(new Rect(0, 0, cw, viewHeight));
            GUI.BeginGroup(new Rect(0, -scroll.y, cw, ch));

            for (int i = 0; i < items.Count; i++) DrawItem(i, items[i], 0f, selectedIdx, isInAnimation);

            GUI.EndGroup();
            GUI.EndGroup();

            if (ch > viewHeight) GUI.VerticalScrollbar(new Rect(cw, 0, SCROLLBAR_WIDTH, viewHeight), scroll.y, viewHeight, 0, ch);
            GUI.EndGroup();
            GUI.EndGroup();
        }

        private void HandleMouseSelection() {
            if (Event.current.type != EventType.MouseMove || ActiveSection == null || ActiveSection.IsBuilding) return;

            var   mousePos           = Event.current.mousePosition;
            float tabBarHeightOffset = sections.Count > 1 ? TAB_BAR_HEIGHT : 0;
            float breadcrumbOffset   = ActiveSection.NavigationStack.Count > 1 ? BREADCRUMB_HEIGHT + 2 : 0;
            float listStartY         = BORDER_WIDTH + HEADER_HEIGHT + SEARCH_HEIGHT + tabBarHeightOffset + 2 + breadcrumbOffset;
            float listStopX          = position.width - BORDER_WIDTH - SCROLLBAR_WIDTH;

            if (mousePos.y < listStartY || mousePos.x > listStopX) return;

            float relativeY = mousePos.y - listStartY + ActiveSection.ScrollPosition.y;
            int   newIndex  = Mathf.FloorToInt(relativeY / ITEM_HEIGHT);

            if (newIndex >= 0 && newIndex < ActiveSection.FilteredItems.Count && ActiveSection.SelectedIndex != newIndex) {
                ActiveSection.SelectedIndex = newIndex;
                Repaint();
            }
        }

        private void HandleKeyboardInput() {
            if (Event.current.type != EventType.KeyDown || ActiveSection == null || ActiveSection.IsBuilding) return;

            switch (Event.current.keyCode) {
                case KeyCode.Escape:
                    Close();
                    Event.current.Use();
                    break;
                case KeyCode.UpArrow:
                    if (ActiveSection.FilteredItems.Count > 0) {
                        ActiveSection.SelectedIndex = ActiveSection.SelectedIndex <= 0 ? ActiveSection.FilteredItems.Count - 1 : ActiveSection.SelectedIndex - 1;
                        ScrollToSelected();
                        GUI.FocusControl(null);
                    }

                    Event.current.Use();
                    Repaint();
                    break;
                case KeyCode.DownArrow:
                    if (ActiveSection.FilteredItems.Count > 0) {
                        ActiveSection.SelectedIndex = ActiveSection.SelectedIndex < 0 || ActiveSection.SelectedIndex >= ActiveSection.FilteredItems.Count - 1 ? 0 : ActiveSection.SelectedIndex + 1;
                        ScrollToSelected();
                        GUI.FocusControl(null);
                    }

                    Event.current.Use();
                    Repaint();
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (ActiveSection.SelectedIndex >= 0 && ActiveSection.SelectedIndex < ActiveSection.FilteredItems.Count) {
                        var item = ActiveSection.FilteredItems[ActiveSection.SelectedIndex];
                        if (item.HasChildren && !isInSearchMode) NavigateInto(item);
                        else SelectItem(item);
                    }

                    Event.current.Use();
                    break;
                case KeyCode.Backspace:
                case KeyCode.LeftArrow:
                    if (string.IsNullOrEmpty(searchText)) {
                        NavigateBack();
                        Event.current.Use();
                    }

                    break;
                case KeyCode.RightArrow:
                    if (ActiveSection.SelectedIndex >= 0 && ActiveSection.SelectedIndex < ActiveSection.FilteredItems.Count) {
                        var item = ActiveSection.FilteredItems[ActiveSection.SelectedIndex];
                        if (item.HasChildren && !isInSearchMode) {
                            NavigateInto(item);
                            Event.current.Use();
                        }
                    }

                    break;
            }
        }

        private void ScrollToSelected() {
            if (ActiveSection == null || ActiveSection.SelectedIndex < 0) return;

            float itemY              = ActiveSection.SelectedIndex * ITEM_HEIGHT;
            float tabBarHeightOffset = sections.Count > 1 ? TAB_BAR_HEIGHT : 0;
            float breadcrumbOffset   = ActiveSection.NavigationStack.Count > 1 ? BREADCRUMB_HEIGHT : 0;
            float viewHeight         = position.height - HEADER_HEIGHT - SEARCH_HEIGHT - tabBarHeightOffset - breadcrumbOffset - BORDER_WIDTH * 2 - 10;

            Vector2 currentScrollPos = ActiveSection.ScrollPosition;

            if (itemY < currentScrollPos.y) {
                currentScrollPos.y = itemY;
            } else if (itemY + ITEM_HEIGHT > currentScrollPos.y + viewHeight) {
                currentScrollPos.y = itemY + ITEM_HEIGHT - viewHeight;
            }

            ActiveSection.ScrollPosition = currentScrollPos;
        }

        #endregion
    }
}