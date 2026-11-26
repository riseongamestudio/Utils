using System.Diagnostics;
using Sirenix.OdinInspector;

namespace RiseOn.Utils {
    /// <summary>
    /// Groups the members that carry it into a section of a settings page, in place of Odin's FoldoutGroup: the same bar, border and fold as every RiseOn settings page draws with <c>Section</c>.<br/>
    /// Alt+click folds everything under it.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    public sealed class SectionGroupAttribute : PropertyGroupAttribute {
        /// <summary>The members meet the border: for a member with a frame of its own, such as tabs.</summary>
        public bool Paddingless;

        /// <summary>What the bar explains under the mouse.</summary>
        public string Tooltip;

        /// <summary>The bar folds the section: only for one worth putting away, long or seldom used.</summary>
        public bool Foldable;

        /// <summary>A foldable section starts folded the first time it shows in a session: for one seldom used.</summary>
        public bool Closed;

        /// <summary>
        /// A bool member of the group drawn as a checkbox on the bar, before the title, for a section that is a feature turned on or off, such as a shadow: it opens as the box is ticked, and folds and edits as any other whether ticked or not, so its settings can be filled before it is on.<br/>
        /// Makes the section foldable; set on any one member.<br/>
        /// A bool marked <c>[ReadOnly]</c> shows unticked and locked, for a feature the project cannot use now, such as one needing a package it lacks; its <c>[Tooltip]</c> says why.
        /// </summary>
        public string Toggle;

        public SectionGroupAttribute(string title, float order = 0) : base(title, order) { }

        protected override void CombineValuesWith(PropertyGroupAttribute other) {
            var section = (SectionGroupAttribute) other;
            Paddingless |= section.Paddingless;
            Foldable    |= section.Foldable;
            Closed      |= section.Closed;
            Tooltip     ??= section.Tooltip;
            Toggle      ??= section.Toggle;
        }
    }
}