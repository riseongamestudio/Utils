using UnityEditor;

namespace RiseOn.Utils.Editor.SearchWindow {
    /// <summary>
    /// The list of a drop-down <see cref="SearchWindow"/>, a window of its own against the one with the search field:<br/>
    /// the list grows and shrinks by moving this window's edges, never the search field's, which stays where it is.<br/>
    /// It takes no keyboard: a click on it hands the focus back to the search field's window once it ends.
    /// </summary>
    internal sealed class SearchListWindow : EditorWindow {
        internal SearchWindow Owner;

        // A domain reload recreates the window without its owner, so there is nothing to show.
        private void CreateGUI() {
            if (Owner == null) Close();
        }

        private void OnLostFocus() {
            if (Owner != null) Owner.OnListLostFocus();
        }

        private void OnDestroy() {
            if (Owner != null) Owner.OnListClosed();
        }
    }
}