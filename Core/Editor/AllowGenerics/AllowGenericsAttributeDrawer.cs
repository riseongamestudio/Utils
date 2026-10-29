using System;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Leaves a <see cref="AllowGenericsAttribute"/> field to Odin's own drawer, so it looks and behaves as any polymorphic field.<br/>
    /// When Odin opens its type selector for it, from its arrow button or the keyboard, the selector is closed at once<br/>
    /// and <see cref="GenericTypePicker"/> opens in its place, at the place and width Odin gave it.<br/>
    /// Odin's Change Type menu item opens its selector outside the field's drawing, so it keeps Odin's own selector.
    /// </summary>
    [DrawerPriority(0, 0, 3000)]
    public sealed class AllowGenericsAttributeDrawer<TAttribute> : OdinAttributeDrawer<TAttribute> where TAttribute : AllowGenericsAttribute {
        protected override bool CanDrawAttributeProperty(InspectorProperty property) {
            var type = property.ValueEntry?.BaseValueType;
            return type != null && !type.IsValueType && !typeof(Object).IsAssignableFrom(type);
        }

        protected override void DrawPropertyLayout(GUIContent label) {
            // Odin opens its selector on an input event; only then is it worth looking for a window it opened.
            var isInput = Event.current.type is EventType.MouseDown or EventType.MouseUp or EventType.KeyDown;
            var before = isInput ? new HashSet<OdinEditorWindow>(Resources.FindObjectsOfTypeAll<OdinEditorWindow>()) : null;

            var whole = EditorGUILayout.BeginVertical();
            CallNextDrawer(label);
            EditorGUILayout.EndVertical();

            if (!isInput) return;

            foreach (var window in Resources.FindObjectsOfTypeAll<OdinEditorWindow>()) {
                if (before.Contains(window)) continue;

                // Odin makes its selector as wide as the field, which ends where the row ends; a drop-down does not report where it is,
                // so the field is placed from the row, on its first line, while the Inspector still draws.
                var width = window.position.width;
                window.Close();

                var field = new Rect(whole.xMax - width, whole.y, width, EditorGUIUtility.singleLineHeight);
                GenericTypePicker.Pick(field, Property.ValueEntry.BaseValueType, Attribute, Assign);
                return;
            }
        }

        // The value changes on the Inspector's next repaint, which is made to happen now.
        private void Assign(Type type) {
            Property.Tree.DelayActionUntilRepaint(() => Property.ValueEntry.WeakSmartValue = type == null ? null : Activator.CreateInstance(type));
            InternalEditorUtility.RepaintAllViews();
        }
    }
}