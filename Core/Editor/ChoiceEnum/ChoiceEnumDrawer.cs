using System;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Every enum field Odin draws opens the pick window of <see cref="ChoiceFieldUtils"/> rather than Odin's selector, [Flags] ones as toggles.<br/>
    /// Just above Odin's own enum drawer (0.999) and below any other: an attribute that draws its enum its own way, as [EnumToggleButtons]<br/>
    /// or [ValueDropdown] do, still does, and so does a drawer written for one enum, at the default 1.
    /// </summary>
    [DrawerPriority(0, 0, 0.9995)]
    public sealed class ChoiceEnumDrawer<T> : OdinValueDrawer<T> {
        public override bool CanDrawTypeFilter(Type type) => type.IsEnum;

        protected override void DrawPropertyLayout(GUIContent label) {
            var rect = EditorGUILayout.GetControlRect(label != null);
            if (label != null) rect = EditorGUI.PrefixLabel(rect, label);
            RectFieldUtils.Draw(Property, rect);
        }
    }
}