using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Makes an Alt click on any foldout of an Odin Inspector open or close everything under it, as Unity's Inspector and Hierarchy do:<br/>
    /// it wraps every property, and when the property's foldout state changes while Alt is held, gives every property under it the same state.<br/>
    /// Odin's own foldouts, lists and custom drawers alike work so, with nothing to write in them.
    /// </summary>
    [DrawerPriority(DrawerPriorityLevel.SuperPriority)]
    public sealed class RecursiveFoldoutDrawer : OdinDrawer {
        protected override void DrawPropertyLayout(GUIContent label) {
            var wasExpanded = Property.State.Expanded;
            CallNextDrawer(label);

            var isExpanded = Property.State.Expanded;
            if (isExpanded != wasExpanded && Event.current is { alt: true }) Property.SetExpandedRecursively(isExpanded);
        }
    }
}