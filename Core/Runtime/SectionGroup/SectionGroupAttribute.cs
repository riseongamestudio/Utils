using System.Diagnostics;
using Sirenix.OdinInspector;

namespace RiseOn.Utils {
    /// <summary>
    /// Groups the members that carry it into a section of a settings page, in place of Odin's FoldoutGroup: the same bar, border and fold as every RiseOn settings page draws with <c>SettingsSection</c>.<br/>
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

        public SectionGroupAttribute(string title, float order = 0) : base(title, order) { }

        protected override void CombineValuesWith(PropertyGroupAttribute other) {
            var section = (SectionGroupAttribute) other;
            Paddingless |= section.Paddingless;
            Foldable    |= section.Foldable;
            Closed      |= section.Closed;
            Tooltip     ??= section.Tooltip;
        }
    }
}