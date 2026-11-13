using System;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Draws <see cref="ScriptFieldAttribute"/> with <see cref="ScriptUtils.Field(Rect, GUIContent, Type)"/>.<br/>
    /// Generic, as Odin's own TypeDrawer is: Odin matches a drawer to the value's runtime type, System.RuntimeType.<br/>
    /// Ranked just above that TypeDrawer, which would otherwise draw the type picker in its place.
    /// </summary>
    [DrawerPriority(0, 0, 2002)]
    public sealed class ScriptFieldAttributeDrawer<T> : OdinAttributeDrawer<ScriptFieldAttribute, T> where T : Type {
        protected override void DrawPropertyLayout(GUIContent label) {
            ScriptUtils.Field(EditorGUILayout.GetControlRect(label != null), label, ValueEntry.SmartValue);
        }
    }
}