using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>Draws <see cref="SectionReferenceAttribute"/>: a foldout <see cref="Section"/> with the value's script on the bar; a value with no field to show is the bar alone, with no arrow.</summary>
    // Generic, as Odin matches a drawer to the value's own class; above Odin's drawer for a [SerializeReference] value,
    // which would put a type picker on the bar.
    [DrawerPriority(DrawerPriorityLevel.SuperPriority)]
    public sealed class SectionReferenceAttributeDrawer<T> : OdinAttributeDrawer<SectionReferenceAttribute, T> where T : class {
        protected override void Initialize() {
            Section.Restore(Property);
        }

        protected override void DrawPropertyLayout(GUIContent label) {
            label ??= Property.Label;
            using var section = Property.Children.Count == 0
                ? new Section(label.text, label.tooltip, barValue: true, barOnly: true)
                : Section.Foldout(Property, label.text, label.tooltip, barValue: true);
            ScriptUtils.Field(section.BarValue, GUIContent.none, ValueEntry.SmartValue?.GetType());
            if (!section.Open) return;

            foreach (var child in Property.Children) child.Draw();
        }
    }
}