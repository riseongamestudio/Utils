using System;
using System.Diagnostics;

namespace RiseOn.Utils {
    /// <summary>
    /// Draws a list or array as a section of a settings page, the same bar, border and fold as <see cref="SectionGroupAttribute"/>'s, in place of Odin's list: + on the bar adds, x on a row removes, and a row's grip drags it to another place.<br/>
    /// Objects dragged onto the list from elsewhere, such as assets from the Project window, land between two items where they are let go: over a row's handle or x,
    /// the edge between two rows, or past the last; over a row's content they go to its fields, as an object field takes them. See <see cref="Drop"/>.<br/>
    /// A list of an enum is a set: + offers only the values it lacks, a row keeps its value and shows its name, and the values keep the enum's order.<br/>
    /// The bar takes the member's label and tooltip.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class SectionListAttribute : Attribute {
        /// <summary>The bar folds the section: only for one worth putting away, long or seldom used.</summary>
        public bool Foldable;

        /// <summary>A foldable section starts folded the first time it shows in a session: for one seldom used.</summary>
        public bool Closed;

        /// <summary>An item as the bar's and rows' tooltips name it, such as "format"; by default the item type's name.</summary>
        public string Item;

        /// <summary>
        /// A method or Odin expression that gives the item + adds, such as one numbered apart from the others, resolved on the parent.<br/>
        /// By default an empty string, a new instance of a class with a parameterless constructor, or the default.
        /// </summary>
        public string New;

        /// <summary>
        /// A method or Odin expression that turns an object dropped on the list into the item it adds, resolved on the parent with the object as <c>obj</c>
        /// (a <see cref="UnityEngine.Object"/>); the default, or null, turns it away.<br/>
        /// By default a list of a <see cref="UnityEngine.Object"/> type takes an object of that type, or the first component of it on a dropped game object; any other list takes no drop.
        /// </summary>
        public string Drop;

        /// <summary>
        /// A member or Odin expression that gives the <see cref="System.Type"/> of object + picks, resolved on the parent, such as the interface the items wrap; with <see cref="Drop"/>,
        /// which turns the picked object into the item. + then opens the object picker, as Odin's list of objects does, and the pick goes to the end of the list.<br/>
        /// By default a list of a <see cref="UnityEngine.Object"/> type picks that type; any other list adds as <see cref="New"/> says.
        /// </summary>
        public string Pick;

        /// <summary>
        /// A string member of the parent drawn as a text field in the bar's title, after the arrow, the section folding: for a list that stands for its parent,
        /// one item of an outer list the page names, such as a plan of steps. The parent's drawer draws the list alone.
        /// </summary>
        public string TitleField;

        /// <summary>A member of the parent drawn in the bar's value column, the title keeping to the label column: for the one value the list's parent has beside it, such as a plan's weight.</summary>
        public string BarField;
    }
}