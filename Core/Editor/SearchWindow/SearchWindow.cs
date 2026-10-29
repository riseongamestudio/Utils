using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace RiseOn.Utils.Editor.SearchWindow {
    /// <summary>
    /// Window that browses and searches trees of <see cref="SearchNode"/>, one tab per section.<br/>
    /// Built with UI Toolkit: the list only creates rows for what is on screen, so long lists stay fast.<br/>
    /// Opens as a <see cref="SearchWindowMode"/>: a window like Unity's own object picker, or a drop-down under the field like Odin's selectors.<br/>
    /// Subclasses add sections in <see cref="RegisterSections"/> and open the window with <see cref="Show"/>.
    /// </summary>
    public abstract class SearchWindow : EditorWindow {
        private const float  ItemHeight        = 20f;
        private const double AnimationDuration = 0.4;
        private const string StyleSheetPath    = "SearchWindow/SearchWindow.uss"; // relative to this assembly's asmdef

        // The height of a drop-down's body while its first section builds, and the height a drop-down stops growing at.
        private const float DropDownLoadingHeight = 80f;

        // The edge a drop-down draws, as SearchWindow.uss sets it, and the height of a page's breadcrumb.
        private const float DropDownBorder   = 1f;
        private const float BreadcrumbHeight = 22f;

        // How long a drop-down takes to reach a new size, to fold back to its button as it closes, and a list to fade in once built.
        private const double ResizeDuration = 0.15;
        private const double CloseDuration  = 0.1;
        private const double FadeDuration   = 0.15;

        // A list that takes longer than this to build shows the loading view, and then for at least the minimum, so it never flashes.
        // A faster one shows at once, with no loading view, as anything under a tenth of a second or so reads as instant.
        private const long LoadingDelayMs   = 250;
        private const long LoadingMinimumMs = 400;

        private static readonly Vector2 minWindowSize = new(250f, 200f);

        private static Texture2D  cachedNoneIcon;
        private static StyleSheet cachedStyleSheet;

        // Unity's own, internal, reached as Odin reaches it; should a version of Unity drop one, the public stand-in does, a little less well.
        // Draws a window at once, in the same tick as it changes size, where it would be drawn a frame later, its new edge showing nothing for that frame.
        private static readonly Action<EditorWindow> repaintNow = FindInternal("RepaintImmediately", window => window.Repaint());

        // Shows a popup without taking the focus from the window that has it: a drop-down's list beside its search field.
        private static readonly Action<EditorWindow> showWithoutFocus = FindShowWithoutFocus();

        // Lowers the floor Unity holds a new window's frame to, 80 high whatever the window asks for, so a drop-down shows as the line it opens from.
        private static readonly Action<EditorWindow, Vector2> setFrameFloor = FindSetFrameFloor();

        // How tall the controls of the last drop-down were, for the next one to know where it opens before its layout has run.
        private static float lastControlsHeight;

        // A drop-down a click outside closed, whose cancel, if any, waits for that click to end: a click on the very button that opened it
        // closes it as it lands, then has the button open it again, on press or on release. That opening finds the click here and is dropped with the cancel,
        // so the button works as a toggle; a click anywhere else lets the cancel run once it ends. The wait is at least this long.
        private const double CancelWait = 0.05;
        private static bool   isClickPending;

        // The drop-down open now, if any. Unity tells a popup it lost the focus only after the click that took it is handled,
        // so a click on the button that opened it would open a second one: opening it again from its button closes it instead, as a toggle,
        // and opening another closes it first.
        private static SearchWindow openDropDown;
        private static Action pendingCancel;
        private static Rect   pendingButton;
        private static double pendingSince;

        private readonly List<SectionState>  sections        = new();
        private readonly List<ToolbarToggle> tabs            = new();
        private readonly List<VisualElement> tabProgressBars = new();
        private readonly List<ScoredNode>    scoredNodes     = new();

        private SearchWindowMode   mode;
        private Rect               button;
        private string             headerText;
        private bool               isShowingNext;
        private bool               isKeptOpen;
        private bool               isClosing;
        private bool               isTakingFocusBack;
        private bool               isLoadingShown;
        private double             loadingShownAt;

        private readonly List<Action> pendingBuilds = new();
        private Action             onCanceled;
        private bool               isPicked;
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

        // A drop-down is two windows against each other: the controls, the title, the search field and the tabs, against the edge it opens from,
        // its anchor, and the list beside them, on the far side. The controls never change size; the list grows and shrinks by its far edge,
        // what it shows hanging from its top: the screen moves a window by its top left corner a frame before the editor draws it there,
        // so what hangs from that corner moves with it in step, and nothing has to stay put on screen while that corner moves.
        // The anchor stays where it opened as the list changes size, and moves only when the list needs more room than its side has, staying where it moved to.
        private SearchListWindow listWindow;
        private VisualElement    controls;
        private VisualElement    outgoingControls;
        private VisualElement    outgoingContent;
        private float            controlsHeight;
        private float            controlsShown;
        private float            listShown;
        private float            contentHeight;
        private float            listHeight;

        // Which side of its button a drop-down opens to, decided when the first window of a chain opens and kept by the others:
        // under it, the controls on top, or above it, turned over, the controls at the bottom next to the button.
        // With room on neither side, it opens over the field, as under it but centered on the field.
        private DropDownSide side;
        private float        anchor;
        private float?       openingAnchor;
        private float        frameX;
        private float        frameWidth;

        private Label              header;
        private ToolbarSearchField searchField;
        private VisualElement      content;
        private ProgressBar        buildProgressBar;
        private Page               page;

        private bool                        isAnimating;
        private Page                        outgoingPage;
        private IVisualElementScheduledItem animation;
        // The move of a drop-down's list under way, run on each editor update: out of the panel's own scheduler, whose loop drawing the window at once would run again inside itself.
        private EditorApplication.CallbackFunction moving;

        // The fit of a drop-down under way, waiting for the layout, and what runs once it has.
        private EditorApplication.CallbackFunction fitting;
        private readonly List<Action>              afterFitting = new();
        private bool                               isFramed;
        private IVisualElementScheduledItem loadingTimer;

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

        /// <param name="btnRect">The button or field the window opens under, in the current GUI space; call it while the GUI draws.</param>
        /// <param name="onCanceled">
        /// Runs when the window closes with nothing picked: Escape, a click elsewhere, or its close button; for a click, once it ends.<br/>
        /// A click on the button that opened a drop-down only closes it, as a toggle, and runs nothing.
        /// </param>
        /// <param name="defaultSize">
        /// Size of the first open of a <see cref="SearchWindowMode.Window"/>; later opens reuse whatever size it was left at.<br/>
        /// A <see cref="SearchWindowMode.DropDown"/> takes its width from <paramref name="btnRect"/>, at least this one's, and fits its height to its rows.
        /// </param>
        protected void Show(
            Rect btnRect
          , string headerText
          , Action<SearchNode> onItemSelected
          , Vector2 defaultSize
          , string searchText = ""
          , SearchWindowMode mode = SearchWindowMode.Window
          , Action onCanceled = null) {
            if (ClaimClosingClick(GUIUtility.GUIToScreenRect(btnRect)) || ToggleOpenDropDown(GUIUtility.GUIToScreenRect(btnRect))) {
                DestroyImmediate(this);
                return;
            }

            this.onItemSelected = onItemSelected;
            this.onCanceled     = onCanceled;
            this.searchText     = searchText ?? string.Empty;
            this.mode           = mode;
            this.headerText     = headerText;
            button              = GUIUtility.GUIToScreenRect(btnRect);

            sections.Clear();
            RegisterSections();

            var lastTab = EditorPrefs.GetString(TabPrefKey, null);
            activeSectionIndex = Math.Max(0, sections.FindIndex(section => section.Label == lastTab));
            Build();

            titleContent = new GUIContent(headerText);
            if (mode is SearchWindowMode.DropDown) {
                var bounds = GetBounds(button);
                frameWidth = Mathf.Max(button.width, defaultSize.x);
                frameX     = Mathf.Clamp(button.x, bounds.x, Mathf.Max(bounds.x, bounds.xMax - frameWidth));
                openDropDown = this;
                Place();

                // Folded to a line at its anchor until the layout knows its controls and its list, then it unfolds from there.
                // A popup, not a drop-down: Unity closes a drop-down as soon as it loses the focus, as it does to its list on a click.
                // Sized again once shown, as showing it gives it a size of its own.
                var folded = GetFrames(anchor, 0f, 0f).Controls;
                Frame(this, folded);
                ShowPopup();
                setFrameFloor(this, folded.size);
                Frame(this, folded);
                return;
            }

            minSize  = minWindowSize;
            position = PlaceUnderButton(button, LoadSize(defaultSize));
            ShowAuxWindow();
        }

        /// <summary>
        /// Lists new sections in place of the current ones, in the same window, for a pick made in steps:<br/>
        /// called from the callback of one step, the window stays open on the next instead of closing.<br/>
        /// <see cref="RegisterSections"/> runs again, so set up what it adds before calling this.
        /// </summary>
        protected void ShowNext(string headerText, Action<SearchNode> onItemSelected) {
            this.onItemSelected = onItemSelected;
            this.headerText     = headerText;
            titleContent        = new GUIContent(headerText);
            isShowingNext       = true;

            FinishAnimation();
            searchText           = string.Empty;
            isInSearchMode       = false;
            userTookOver         = false;
            isInitialDataPending = false;
            activeSectionIndex   = 0;

            // The old content goes first, so a section that builds at once lands in the new one. A drop-down keeps the old controls and list where they are,
            // fading them out as the new ones fade in over them; the list then eases to the new one's size.
            if (mode is SearchWindowMode.DropDown && controls != null) {
                outgoingControls?.RemoveFromHierarchy();
                outgoingContent?.RemoveFromHierarchy();
                outgoingControls = controls;
                outgoingContent  = content;
                foreach (var old in new[] { controls, content }) Fade(old, 1f, 0f, old.RemoveFromHierarchy);
            } else {
                rootVisualElement.Clear();
            }

            tabs.Clear();
            tabProgressBars.Clear();
            content = null;

            sections.Clear();
            RegisterSections();
            Build();
            BuildUI();
            if (ActiveSection is not { IsBuilding: false }) return;

            if (mode is SearchWindowMode.DropDown) {
                Fade(controls, 0f, 1f);
                Fade(content, 0f, 1f);
            } else {
                Fade(page.Root, 0f, 1f);
            }
        }

        // Builds every section now, once the tab to open on is known; a section that builds in steps reports when it is done.
        private void Build() {
            var builds = new List<Action>(pendingBuilds);
            pendingBuilds.Clear();
            foreach (var build in builds) build();
        }

        /// <summary>
        /// Adds a tab, built right after <see cref="RegisterSections"/>: the builder completes the section at once, or reports progress and completes later.
        /// </summary>
        protected void AddSection(string label, Action<SectionBuildContext> builder) {
            var state = new SectionState { Label = label };
            sections.Add(state);
            pendingBuilds.Add(() => builder(new SectionBuildContext(state, this)));
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

        // Clicking anywhere else closes it, so a pick never lands on an object the Inspector has stopped showing; a click on a drop-down's own list
        // takes the focus for a moment, and the list hands it back. Moving and resizing through the title bar and edges keep the focus.
        private void OnLostFocus() {
            // Over the list, the focus is going to it, unless it is going elsewhere, to another application say: told once it has gone.
            if (listWindow != null && mouseOverWindow == listWindow) {
                EditorApplication.delayCall += DismissUnlessFocused;
                return;
            }

            Dismiss(isByClick: true);
        }

        private void DismissUnlessFocused() {
            if (this == null || isClosing) return;
            if (InternalEditorUtility.isApplicationActive && (focusedWindow == this || focusedWindow == listWindow && listWindow != null)) return;

            Dismiss(isByClick: true);
        }

        // The list holds the focus after a click on it, until it hands it back: the focus leaving it for anywhere else closes the drop-down,
        // as it leaving the search field would.
        internal void OnListLostFocus() {
            if (isClosing || isTakingFocusBack) return;

            // Over the search field, the focus is going to it, unless it is going elsewhere: told once it has gone.
            if (mouseOverWindow == this) {
                EditorApplication.delayCall += DismissUnlessFocused;
                return;
            }

            Dismiss(isByClick: true);
        }

        private void OnFocus() {
            isTakingFocusBack = false;
        }

        // The list keeps the focus while the click on it lasts, and gives it back once it ends: taken back at once, the list would lose the press itself.
        private void TakeFocusBack() {
            if (isClosing || listWindow == null || focusedWindow != listWindow) return;

            isTakingFocusBack = true;
            Focus();
            FocusSearchField();
        }

        internal void OnListClosed() {
            listWindow = null;
            if (!isClosing) Dismiss();
        }

        // The pick or the cancel takes effect at once, but for the cancel of a click outside a drop-down, which waits for the click to end.
        // A drop-down then rolls back up under its button before it closes; a window closes at once.
        private void Dismiss(bool isByClick = false) {
            if (isClosing) return;

            isClosing = true;
            Cancel(isByClick);

            loadingTimer?.Pause();
            if (mode is SearchWindowMode.DropDown) MoveTo(anchor, 0f, 0f, CloseDuration, Close);
            else Close();
        }

        private void OnDestroy() {
            // Closed by Unity with no word to it first, as a drop-down can be on a click elsewhere.
            Cancel(isByClick: !isClosing);

            if (mode is SearchWindowMode.Window) SaveSize();
            if (sections.Count > 1 && ActiveSection != null) EditorPrefs.SetString(TabPrefKey, ActiveSection.Label);

            if (openDropDown == this) openDropDown = null;

            animation?.Pause();
            StopMoving();
            StopFitting();
            loadingTimer?.Pause();

            if (listWindow == null) return;

            var list = listWindow;
            listWindow = null;
            list.Owner = null;
            list.Close();
        }

        private void Cancel(bool isByClick) {
            var cancel = onCanceled;
            onCanceled = null;
            if (isPicked) return;

            if (!isByClick || mode is not SearchWindowMode.DropDown) {
                cancel?.Invoke();
                return;
            }

            RunPendingCancel();
            isClickPending = true;
            pendingCancel  = cancel;
            pendingButton = button;
            pendingSince  = EditorApplication.timeSinceStartup;
            EditorApplication.update -= ResolvePendingCancel;
            EditorApplication.update += ResolvePendingCancel;
        }

        // True when the window about to open is the one the click in progress just closed: the cancel is dropped, and nothing opens.
        private static bool ClaimClosingClick(Rect screenButton) {
            if (!isClickPending) return false;

            var isSame = IsSameButton(screenButton, pendingButton);
            if (isSame) {
                isClickPending = false;
                pendingCancel  = null;
            } else {
                RunPendingCancel();
            }

            return isSame;
        }

        // True when the drop-down open now is the one the button opens, which then only closes, picking and canceling nothing;
        // false when there is none, or when another button opens a new one, which closes the one open as a click elsewhere would.
        private static bool ToggleOpenDropDown(Rect screenButton) {
            var open = openDropDown;
            if (open == null || open.isClosing) return false;

            if (!IsSameButton(screenButton, open.button)) {
                open.Dismiss(isByClick: true);
                return false;
            }

            open.onCanceled = null;
            open.Dismiss();
            return true;
        }

        private static bool IsSameButton(Rect a, Rect b) {
            return Vector2.Distance(a.position, b.position) < 1f && Vector2.Distance(a.size, b.size) < 1f;
        }

        // The click has ended once no control holds the mouse; the button that opened the window holds it until released, and reopens it then.
        private static void ResolvePendingCancel() {
            if (isClickPending && (EditorApplication.timeSinceStartup - pendingSince < CancelWait || GUIUtility.hotControl != 0)) return;

            EditorApplication.update -= ResolvePendingCancel;
            RunPendingCancel();
        }

        private static void RunPendingCancel() {
            var cancel = pendingCancel;
            isClickPending = false;
            pendingCancel  = null;
            cancel?.Invoke();
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

        /// <summary>
        /// Fits a drop-down to its controls and its rows once the layout knows them, then runs <paramref name="afterLayout"/>, as a scroll needs the layout too.<br/>
        /// The first fit unfolds it. While searching the list fits the results by its far edge alone, the controls staying where they are:<br/>
        /// rows its side has no room for scroll, where any other change of size moves the anchor to make room.
        /// </summary>
        private void FitDropDown(Action afterLayout = null) {
            if (mode is not SearchWindowMode.DropDown || isClosing || controls == null) return;

            if (afterLayout != null) afterFitting.Add(afterLayout);
            if (fitting != null) return;

            // Polled on each editor update, out of the panels' own loops, drawing both windows at once so their layout runs there and then.
            fitting = () => {
                if (this == null || isClosing || controls == null) {
                    StopFitting();
                    return;
                }

                repaintNow(this);
                if (listWindow != null) repaintNow(listWindow);

                var measured = controls.layout.height;
                if (float.IsNaN(measured) || measured <= 0f) return;

                controlsHeight = lastControlsHeight = measured;

                // Rows the screen has no room for scroll; the list waits for the layout to take the height it is given.
                var isTyping = IsTyping;
                var height   = listHeight = GetListHeight();
                UpdateScroller();
                if (Mathf.Abs(content.resolvedStyle.height - height) > .5f) {
                    content.style.height = height;
                    return;
                }

                if (page.Root.resolvedStyle.display is DisplayStyle.Flex && page.List.layout.height <= 0f) return;

                StopFitting();
                var afterLayouts = afterFitting.ToArray();
                afterFitting.Clear();
                foreach (var after in afterLayouts) after();

                var toAnchor = isTyping ? anchor : GetAnchor(openingAnchor ?? anchor, height);
                openingAnchor = null;
                MoveTo(toAnchor, GetControlsFrameHeight(), height + DropDownBorder);
            };
            EditorApplication.update += fitting;
        }

        private void StopFitting() {
            if (fitting != null) EditorApplication.update -= fitting;
            fitting = null;
        }

        /// <summary>
        /// Decides the side a drop-down opens to, before it shows, from the height it is likely to take: its controls as tall as the last drop-down's,<br/>
        /// its list as tall as the rows of its tab, when built. Under its button when it fits there, else above it, turned over,<br/>
        /// else over the field, as under it, centered on the field and unfolding from its middle.
        /// </summary>
        private void Place() {
            controlsHeight = lastControlsHeight > 0f ? lastControlsHeight : 2f * ItemHeight;

            var section = ActiveSection;
            var rows    = section is { IsBuilding: false }
                ? (section.NavigationStack.Count > 1 ? BreadcrumbHeight : 0f) + Mathf.Max(1, section.CurrentItems.Count) * ItemHeight
                : DropDownLoadingHeight;

            var bounds = GetBounds(button);
            var total  = GetControlsFrameHeight() + Mathf.Min(rows, GetMaxListHeight()) + DropDownBorder;
            if (total <= bounds.yMax - button.yMax) {
                side   = DropDownSide.Below;
                anchor = button.yMax;
            } else if (total <= button.y - bounds.y) {
                side   = DropDownSide.Above;
                anchor = button.y;
            } else {
                side          = DropDownSide.Below;
                anchor        = button.center.y;
                openingAnchor = Mathf.Clamp(button.center.y - total / 2f, bounds.y, Mathf.Max(bounds.y, bounds.yMax - total));
            }
        }

        private bool IsTyping => isInSearchMode && listShown > 0f;

        // The list's height once it settles: as tall as its rows, up to the room it has; while searching, the room on its side with the anchor kept.
        private float GetListHeight() {
            return Mathf.Min(contentHeight, IsTyping ? GetListRoomAtAnchor() : GetMaxListHeight());
        }

        // Rows the list cannot show once settled scroll, the scroll bar showing as the list stops growing: while it grows, the rows come out with it.
        private void UpdateScroller() {
            if (mode is not SearchWindowMode.DropDown || page == null) return;

            page.ShowsScroller = contentHeight > listHeight + .5f && listShown - DropDownBorder >= listHeight - .5f;
        }

        private float GetControlsFrameHeight() {
            return controlsHeight + 2f * DropDownBorder;
        }

        // As tall as a list goes: the screen's height; the anchor moves to give it room.
        private float GetMaxListHeight() {
            return GetBounds(button).height - GetControlsFrameHeight() - DropDownBorder;
        }

        // What the list has room for on its side with the anchor where it is.
        private float GetListRoomAtAnchor() {
            var bounds = GetBounds(button);
            var room   = side is DropDownSide.Below ? bounds.yMax - anchor : anchor - bounds.y;
            return room - GetControlsFrameHeight() - DropDownBorder;
        }

        // Where the anchor goes for a list of the given height: where it is, unless the list needs more room than its side has;
        // then it moves just far enough, away from the list, and stays there.
        private float GetAnchor(float from, float height) {
            var bounds = GetBounds(button);
            var total  = GetControlsFrameHeight() + height + DropDownBorder;
            return side is DropDownSide.Below
                ? Mathf.Max(bounds.y, Mathf.Min(from, bounds.yMax - total))
                : Mathf.Min(bounds.yMax, Mathf.Max(from, bounds.y + total));
        }

        // The screen the button is on, which a drop-down keeps inside, over the editor's windows or past them.
        private static Rect GetBounds(Rect button) {
            return InternalEditorUtility.GetBoundsOfDesktopAtPoint(button.center);
        }

        /// <summary>
        /// Eases a drop-down to <paramref name="toControls"/> of controls and <paramref name="toList"/> of list shown, both window heights,<br/>
        /// its anchor to <paramref name="toAnchor"/>, from wherever they are, even midway through an earlier move, then runs <paramref name="onDone"/>.<br/>
        /// Unfolding and folding run as one block: the controls come out of the anchor and, once whole, the list out of them; folding, the other way round.<br/>
        /// Each step moves the windows and draws them in the same tick.
        /// </summary>
        private void MoveTo(float toAnchor, float toControls, float toList, double duration = ResizeDuration, Action onDone = null) {
            StopMoving();

            var fromAnchor   = anchor;
            var fromControls = controlsShown;
            var fromList     = listShown;
            var isFolding    = fromControls <= 0f || toControls <= 0f;
            var wholeControls = Mathf.Max(fromControls, toControls);
            if (Mathf.Abs(fromAnchor - toAnchor) < .5f && Mathf.Abs(fromControls - toControls) < .5f && Mathf.Abs(fromList - toList) < .5f) {
                SetFrames(toAnchor, toControls, toList);
                onDone?.Invoke();
                return;
            }

            var start = EditorApplication.timeSinceStartup;
            moving = () => {
                if (this == null) {
                    StopMoving();
                    return;
                }

                var progress = Mathf.Clamp01((float)((EditorApplication.timeSinceStartup - start) / duration));
                var eased    = 1f - Mathf.Pow(1f - progress, 3f);
                float shownControls, shownList;
                if (isFolding) {
                    var shown = Mathf.Lerp(fromControls + fromList, toControls + toList, eased);
                    shownControls = Mathf.Min(shown, wholeControls);
                    shownList     = shown - shownControls;
                } else {
                    shownControls = Mathf.Lerp(fromControls, toControls, eased);
                    shownList     = Mathf.Lerp(fromList, toList, eased);
                }

                var isControlsMoved = SetFrames(Mathf.Lerp(fromAnchor, toAnchor, eased), shownControls, shownList);
                if (listWindow != null) repaintNow(listWindow);
                if (isControlsMoved) repaintNow(this);

                if (progress < 1f) return;

                StopMoving();
                onDone?.Invoke();
            };
            EditorApplication.update += moving;
        }

        // Puts the controls against the anchor and the list against them, on the far side; true when the controls' window moved or changed size.
        private bool SetFrames(float anchorAt, float shownControls, float shownList) {
            var isControlsMoved = !isFramed || Mathf.Abs(anchorAt - anchor) >= .01f || Mathf.Abs(shownControls - controlsShown) >= .01f;
            anchor        = anchorAt;
            controlsShown = shownControls;
            listShown     = shownList;
            isFramed      = true;

            var frames = GetFrames(anchorAt, shownControls, shownList);
            if (isControlsMoved) Frame(this, frames.Controls);
            if (listWindow != null) Frame(listWindow, frames.List);
            UpdateScroller();
            return isControlsMoved;
        }

        // The controls' window against the anchor and the list's against it, on the far side, each at least a line, its edge.
        private (Rect Controls, Rect List) GetFrames(float anchorAt, float shownControls, float shownList) {
            var controlsFrame = Mathf.Max(DropDownBorder, shownControls);
            var listFrame     = Mathf.Max(DropDownBorder, shownList);
            var controlsTop   = side is DropDownSide.Above ? anchorAt - controlsFrame : anchorAt;
            var listTop       = side is DropDownSide.Above ? controlsTop - listFrame : controlsTop + controlsFrame;
            return (new Rect(frameX, controlsTop, frameWidth, controlsFrame), new Rect(frameX, listTop, frameWidth, listFrame));
        }

        // Each window is held to its size through its min and max size, so those change with it. Changed alone, they would resize the window
        // where it is, its top-left kept, before it moves: one moved by its top, the list above its button, would show a gap at its bottom.
        // So they first let both sizes through, the window then moves and resizes in one step, and they hold it at its new size.
        private static void Frame(EditorWindow window, Rect rect) {
            window.minSize  = Vector2.Min(window.minSize, rect.size);
            window.maxSize  = Vector2.Max(window.maxSize, rect.size);
            window.position = rect;
            window.minSize  = window.maxSize = rect.size;
        }

        private void StopMoving() {
            if (moving != null) EditorApplication.update -= moving;
            moving = null;
        }

        // The list's window, shown beside the controls without taking their focus; it holds the content.
        private void OpenListWindow() {
            if (listWindow != null) return;

            listWindow = CreateInstance<SearchListWindow>();
            listWindow.Owner = this;

            var root = listWindow.rootVisualElement;
            root.AddToClassList(Uss.Root);
            root.AddToClassList(Uss.DropDown);
            root.AddToClassList(Uss.ListFrame);
            root.EnableInClassList(Uss.Above, side is DropDownSide.Above);
            var styleSheet = LoadStyleSheet();
            if (styleSheet != null) root.styleSheets.Add(styleSheet);
            TrackPointer(root);
            // A click ends with the pointer's release, a drag of the scroll bar with the bar letting go of the pointer, wherever it is released:
            // the bar holds the pointer while dragged and keeps the release to itself.
            root.RegisterCallback<PointerUpEvent>(_ => root.schedule.Execute(TakeFocusBack), TrickleDown.TrickleDown);
            root.RegisterCallback<PointerCaptureOutEvent>(_ => root.schedule.Execute(TakeFocusBack), TrickleDown.TrickleDown);

            // Sized again once shown, as showing it gives it a size of its own.
            var folded = GetFrames(anchor, 0f, 0f).List;
            Frame(listWindow, folded);
            showWithoutFocus(listWindow);
            setFrameFloor(listWindow, folded.size);
            Frame(listWindow, folded);
        }

        private static Action<EditorWindow, Vector2> FindSetFrameFloor() {
            const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var parent    = typeof(EditorWindow).GetField("m_Parent", Members);
            var container = parent?.FieldType.GetProperty("window", Members);
            var setSizes  = container?.PropertyType.GetMethod("SetMinMaxSizes", Members, null, new[] { typeof(Vector2), typeof(Vector2) }, null);
            if (setSizes != null) return (window, size) => setSizes.Invoke(container.GetValue(parent.GetValue(window)), new object[] { size, size });

            Debug.LogWarning($"[{nameof(SearchWindow)}] ContainerWindow.SetMinMaxSizes is gone from this version of Unity: a drop-down may show taller for a moment as it opens.");
            return (_, _) => { };
        }

        private static Action<EditorWindow> FindShowWithoutFocus() {
            var showMode = typeof(EditorWindow).Assembly.GetType("UnityEditor.ShowMode");
            var method   = showMode == null ? null : typeof(EditorWindow).GetMethod("ShowPopupWithMode", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { showMode, typeof(bool) }, null);
            if (method != null) {
                var popupMenu = Enum.Parse(showMode, "PopupMenu");
                return window => method.Invoke(window, new[] { popupMenu, false });
            }

            Debug.LogWarning($"[{nameof(SearchWindow)}] EditorWindow.ShowPopupWithMode is gone from this version of Unity: a drop-down's list takes the focus as it shows.");
            return window => window.ShowPopup();
        }

        private static Action<EditorWindow> FindInternal(string name, Action<EditorWindow> fallback) {
            var method = typeof(EditorWindow).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            if (method != null) return (Action<EditorWindow>)Delegate.CreateDelegate(typeof(Action<EditorWindow>), method);

            Debug.LogWarning($"[{nameof(SearchWindow)}] EditorWindow.{name} is gone from this version of Unity: a drop-down's list may show a little less smoothly.");
            return fallback;
        }

        /// <summary>Takes <paramref name="element"/> from one opacity to another, in place of switching it on or off at once, then runs <paramref name="onDone"/>.</summary>
        private static void Fade(VisualElement element, float from, float to, Action onDone = null) {
            element.style.opacity = from;

            var start = EditorApplication.timeSinceStartup;
            IVisualElementScheduledItem fade = null;
            fade = element.schedule.Execute(() => {
                var progress = Mathf.Clamp01((float)((EditorApplication.timeSinceStartup - start) / FadeDuration));
                element.style.opacity = Mathf.Lerp(from, to, progress);
                if (progress < 1f) return;

                fade.Pause();
                onDone?.Invoke();
            }).Every(10);
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
                if (node.Data != null && node.DisabledReason == null) state.PickableCount++;
            }

            // A real object pulls the window over to its tab. None sits in every tab, so it is highlighted in each and
            // moves nothing.
            if (isInitialDataPending && RevealInitialData(state) && initialData != null) {
                isInitialDataPending = false;
                if (!userTookOver) activeSectionIndex = sections.IndexOf(state);
            }

            // The window may not have built its UI yet; CreateGUI shows the section then.
            if (content == null) return;

            if (state != ActiveSection) {
                UpdateTabs();
                return;
            }

            // Shown before the loading view was due: the list appears as it is. Shown after: it fades in once the view has been up long enough.
            loadingTimer?.Pause();
            if (!isLoadingShown) {
                Reveal(false);
                return;
            }

            var wait = LoadingMinimumMs - (long)((EditorApplication.timeSinceStartup - loadingShownAt) * 1000.0);
            if (wait <= 0) Reveal(true);
            else rootVisualElement.schedule.Execute(() => Reveal(true)).StartingIn(wait);
        }

        private void Reveal(bool fade) {
            isLoadingShown = false;
            if (content == null || ActiveSection is not { IsBuilding: false }) return;

            ApplyFilter();
            RefreshView(restoreScroll: true);
            if (fade) Fade(page.Root, 0f, 1f);
        }

        // The loading view waits before it shows, so a section built quickly never flashes it.
        private void ScheduleLoading() {
            loadingTimer?.Pause();
            isLoadingShown = false;
            if (ActiveSection is not { IsBuilding: true }) return;

            loadingTimer = rootVisualElement.schedule.Execute(() => {
                if (ActiveSection is not { IsBuilding: true }) return;

                isLoadingShown = true;
                loadingShownAt = EditorApplication.timeSinceStartup;
                RefreshView();
            }).StartingIn(LoadingDelayMs);
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

        // Each node also learns its folder and the path of folders above it, for a search across the whole tree.
        private static void FlattenTree(SearchNode node, List<SearchNode> result, string path = "") {
            if (node?.Children == null) return;

            foreach (var child in node.Children) {
                if (child == null) continue;

                child.Parent = node;
                child.Path   = path;
                result.Add(child);
                if (child.HasChildren) FlattenTree(child, result, path.Length == 0 ? child.PlainLabel : $"{path} › {child.PlainLabel}");
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
            if (item.DisabledReason != null || isClosing) return;

            isShowingNext = false;
            isKeptOpen    = false;
            onItemSelected?.Invoke(item);
            if (isShowingNext || isKeptOpen) return;

            isPicked = true;
            Dismiss();
        }

        /// <summary>
        /// Keeps the window open after the pick being handled, for a pick that toggles something and may be followed by more:<br/>
        /// call it from the pick's callback.
        /// </summary>
        protected void KeepOpen() {
            isKeptOpen = true;
        }

        /// <summary>
        /// Draws the rows on screen again, for a change to what they show, such as their icons.
        /// </summary>
        protected void RefreshRows() {
            page?.List.RefreshItems();
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

        /// <summary>Leaves the results for <paramref name="folder"/>, wherever it is in the tree, sliding in as when entering it step by step.</summary>
        private void OpenFolder(SearchNode folder) {
            var section = ActiveSection;
            if (section?.Root == null) return;

            var chain = new List<SearchNode>();
            for (var node = folder; node != null && node != section.Root; node = node.Parent) chain.Add(node);
            chain.Reverse();

            userTookOver = true;
            section.NavigationStack.Clear();
            section.NavigationStack.Push(section.Root);
            foreach (var node in chain) section.NavigationStack.Push(node);
            OpenLevel(section, page, forward: true);
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

            state.ScrollOffset  = page.ScrollFromStart;
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

        // The new tab slides in from the side it sits on, as a deeper level does.
        private void SwitchSection(int index) {
            if (index == activeSectionIndex || sections[index].IsBuilding) return;

            userTookOver = true;
            FinishAnimation();
            if (ActiveSection != null) ActiveSection.ScrollOffset = page.ScrollFromStart;

            var forward = index > activeSectionIndex;
            activeSectionIndex = index;
            ApplyFilter();

            var previousPage = page;
            page = CreatePage();
            content.Add(page.Root);
            RefreshView(restoreScroll: true);
            StartAnimation(previousPage, forward);
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

            if (previous != index && previous >= 0 && previous < page.Items.Count) page.List.RefreshItem(page.VisualOf(previous));
            if (index < 0 || index >= page.Items.Count) return;

            page.List.RefreshItem(page.VisualOf(index));
            if (scrollTo) page.List.ScrollToItem(page.VisualOf(index));
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

            root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationMoveEvent>(IgnoreNavigation, TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationSubmitEvent>(IgnoreNavigation, TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationCancelEvent>(IgnoreNavigation, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerUpEvent>(_ => FocusSearchField());
            if (mode is SearchWindowMode.Window) TrackPointer(root);

            BuildUI();
        }

        // Where the pointer is over the list, for a scroll to move the highlight to the row now under it.
        private void TrackPointer(VisualElement root) {
            root.RegisterCallback<PointerMoveEvent>(evt => {
                lastPointerPosition = evt.position;
                isPointerInside     = true;
            }, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerLeaveEvent>(_ => isPointerInside = false);
        }

        /// <summary>
        /// The window's content under the root: the controls, a title in a drop-down, which has no title bar to carry it, the search field and the tabs, then the list;<br/>
        /// in a drop-down, the list in a window of its own.
        /// </summary>
        private void BuildUI() {
            var root = rootVisualElement;
            root.EnableInClassList(Uss.DropDown, mode is SearchWindowMode.DropDown);
            root.EnableInClassList(Uss.Above, mode is SearchWindowMode.DropDown && side is DropDownSide.Above);

            controls = new VisualElement();
            controls.AddToClassList(Uss.Controls);
            root.Add(controls);

            header = null;
            if (mode is SearchWindowMode.DropDown && !string.IsNullOrEmpty(headerText)) {
                header = new Label(headerText);
                header.AddToClassList(Uss.Header);
                controls.Add(header);
            }

            searchField = new ToolbarSearchField();
            searchField.AddToClassList(Uss.Search);
            searchField.SetValueWithoutNotify(searchText);
            searchField.RegisterValueChangedCallback(evt => OnSearchChanged(evt.newValue));
            controls.Add(searchField);

            if (sections.Count > 1) controls.Add(CreateTabBar());

            content = new VisualElement();
            content.AddToClassList(Uss.Content);
            if (mode is SearchWindowMode.DropDown) {
                // The list's window shows after this one, so this one takes the focus back for the keys to reach the search field.
                OpenListWindow();
                listWindow.rootVisualElement.Add(content);
                Focus();
            } else {
                root.Add(content);
            }

            buildProgressBar = new ProgressBar { title = "Building...", lowValue = 0f, highValue = 1f };
            buildProgressBar.AddToClassList(Uss.BuildProgress);
            content.Add(buildProgressBar);

            page = CreatePage();
            content.Add(page.Root);
            ScheduleLoading();

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
            buildProgressBar.style.display = isBuilding && isLoadingShown ? DisplayStyle.Flex : DisplayStyle.None;
            page.Root.style.display        = isBuilding ? DisplayStyle.None : DisplayStyle.Flex;
            if (isBuilding) {
                buildProgressBar.value = section?.BuildProgress ?? 0f;

                // A drop-down opens on the loading view only once it is due, folded until then.
                if (mode is SearchWindowMode.DropDown && isLoadingShown) {
                    contentHeight = DropDownLoadingHeight;
                    FitDropDown();
                }

                return;
            }

            page.IsInSearchMode = isInSearchMode;
            page.SelectedIndex  = section.SelectedIndex;
            page.Items.Clear();
            page.Items.AddRange(section.FilteredItems);
            page.ShowsIcons = page.Items.Exists(item => item.HasIcon);

            var hasBreadcrumb = section.NavigationStack.Count > 1;
            page.Breadcrumb.style.display = hasBreadcrumb ? DisplayStyle.Flex : DisplayStyle.None;
            page.BreadcrumbLabel.text     = hasBreadcrumb ? section.NavigationStack.Peek().Label ?? string.Empty : string.Empty;

            // A drop-down's list is as tall as its rows, up to what the screen allows; told before the rows change, so the scroll bar
            // does not show for rows the list is about to grow to.
            if (mode is SearchWindowMode.DropDown) {
                contentHeight = (hasBreadcrumb ? BreadcrumbHeight : 0f) + Mathf.Max(1, page.Items.Count) * ItemHeight;
                listHeight    = GetListHeight();
                UpdateScroller();
            }

            page.List.RefreshItems();

            // Search results and a value revealed on open both bring the selected row into view.
            var scrollToSelection = isInSearchMode || section.RevealPending;
            section.RevealPending = false;

            // The scroll view clamps offsets until it has a size, so both run once the layout has run: in a drop-down, once it has placed itself, which may turn the list over.
            var scrollOffset = section.ScrollOffset;
            var boundPage    = page;
            if (mode is SearchWindowMode.DropDown) FitDropDown(Scroll);
            else page.List.schedule.Execute(Scroll);

            void Scroll() {
                if (restoreScroll) boundPage.ScrollFromStart = scrollOffset;
                if (scrollToSelection && boundPage.SelectedIndex >= 0) boundPage.List.ScrollToItem(boundPage.VisualOf(boundPage.SelectedIndex));
            }
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
            newPage.IsReversed = mode is SearchWindowMode.DropDown && side is DropDownSide.Above;
            if (mode is SearchWindowMode.DropDown) newPage.ShowsScroller = false;

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
                if (evt.clickCount == 2 || !item.HasChildren) SelectItem(item);
                else if (isInSearchMode) OpenFolder(item);
                else NavigateInto(item);
            });

            return row;
        }

        /// <summary>Moves the highlight to the row under the pointer, so it keeps covering the list's own hover shade.</summary>
        private void SelectRowUnderPointer() {
            var panel = page.List.panel;
            if (!isPointerInside || isAnimating || panel == null) return;

            for (var element = panel.Pick(lastPointerPosition); element != null; element = element.parent) {
                if (!element.ClassListContains(Uss.Row)) continue;

                if (page.List.Contains(element) && element.userData is int index && index != page.SelectedIndex) {
                    Select(index, scrollTo: false);
                }

                return;
            }
        }

        // The list counts its rows from the top, the page from the search field.
        private static void BindRow(Page owner, VisualElement row, int index) {
            index = owner.VisualOf(index);
            var item = owner.Items[index];
            row.userData = index;
            row.tooltip  = item.PlainLabel;
            row.EnableInClassList(Uss.SelectedRow, index == owner.SelectedIndex);
            row.EnableInClassList(Uss.DisabledRow, item.DisabledReason != null);

            row[0].style.display  = owner.ShowsIcons ? DisplayStyle.Flex : DisplayStyle.None;
            ((Image)row[0]).image = item.Icon;
            ((Label)row[1]).text = (owner.IsInSearchMode ? item.GetSearchLabel() : item.Label ?? string.Empty)
                                 + (item.DisabledReason == null ? string.Empty : $"   ({item.DisabledReason})");
            row[2].style.display = item.HasChildren ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnKeyDown(KeyDownEvent evt) {
            if (evt.keyCode is KeyCode.Escape) {
                Consume(evt);
                Dismiss();
                return;
            }

            var section = ActiveSection;
            if (isAnimating || section == null || section.IsBuilding) {
                if (evt.keyCode is KeyCode.UpArrow or KeyCode.DownArrow or KeyCode.Return or KeyCode.KeypadEnter) Consume(evt);
                return;
            }

            switch (evt.keyCode) {
                case KeyCode.UpArrow:
                case KeyCode.DownArrow:
                    userTookOver = true;
                    Step(evt.keyCode is KeyCode.UpArrow ? -1 : 1);
                    Consume(evt);
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    Consume(evt);
                    if (!TryGetSelected(out var chosen)) break;

                    if (!chosen.HasChildren) SelectItem(chosen);
                    else if (isInSearchMode) OpenFolder(chosen);
                    else NavigateInto(chosen);
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

        // Moves the highlight a row up or down on screen, wrapping around; with none, from the top or the bottom.
        private void Step(int rows) {
            var count = page.Items.Count;
            if (count is 0) return;

            var row = page.SelectedIndex < 0 ? (rows > 0 ? -1 : count) : page.VisualOf(page.SelectedIndex);
            Select(page.VisualOf((row + rows + count) % count), scrollTo: true);
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

        /// <summary>
        /// One level of the tree on screen. Navigating creates a new page and slides the old one out.<br/>
        /// Turned over in a drop-down above its button: its first item at the bottom, next to the search field, and scrolled from there.
        /// </summary>
        private sealed class Page {
            public readonly List<SearchNode> Items = new();

            public VisualElement Root;
            public VisualElement Breadcrumb;
            public Label         BreadcrumbLabel;
            public ListView      List;
            public int           SelectedIndex = -1;
            public bool          IsInSearchMode;
            public bool          IsReversed;
            public bool          ShowsIcons;

            /// <summary>How far the list is scrolled from where it starts: its top, or its bottom when turned over.</summary>
            public float ScrollFromStart {
                get {
                    var offset = Scroll.scrollOffset.y;
                    return IsReversed ? MaxScroll - offset : offset;
                }
                set => Scroll.scrollOffset = new Vector2(0f, IsReversed ? MaxScroll - value : value);
            }

            /// <summary>Whether the scroll bar shows, a drop-down deciding it rather than the scroll view, which would show it for rows the list is still growing to.</summary>
            public bool ShowsScroller {
                set => Scroll.verticalScrollerVisibility = value ? ScrollerVisibility.AlwaysVisible : ScrollerVisibility.Hidden;
            }

            private ScrollView Scroll => List.Q<ScrollView>();

            private float MaxScroll => Mathf.Max(0f, Items.Count * ItemHeight - Scroll.contentViewport.layout.height);

            /// <summary>An item's row from the top on screen, and back: the same, or counted from the bottom when turned over.</summary>
            public int VisualOf(int index) {
                return IsReversed && index >= 0 ? Items.Count - 1 - index : index;
            }
        }

        private enum DropDownSide {
            Below,
            Above
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
            public const string Root     = "search-window";
            public const string DropDown = "search-window--drop-down";
            public const string Above    = "search-window--above";
            public const string ListFrame = "search-window--list-frame";
            public const string Controls = "search-window__controls";

            public const string Header = "search-window__header";

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
            public const string DisabledRow = "search-window__row--disabled";
            public const string RowIcon     = "search-window__row-icon";
            public const string RowLabel    = "search-window__row-label";
            public const string RowArrow    = "search-window__row-arrow";
        }

        #endregion
    }
}