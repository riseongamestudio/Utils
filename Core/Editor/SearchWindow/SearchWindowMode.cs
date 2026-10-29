namespace RiseOn.Utils.Editor.SearchWindow {
    /// <summary>
    /// How a <see cref="SearchWindow"/> opens.<br/>
    /// <see cref="Window"/>: a window with a title bar, like Unity's object picker; it moves, resizes by its edges, and remembers its size per window type.<br/>
    /// <see cref="DropDown"/>: only the content, under the button or field, as wide as it and as tall as the rows it lists, like Odin's selectors.
    /// </summary>
    public enum SearchWindowMode {
        Window
      , DropDown
    }
}