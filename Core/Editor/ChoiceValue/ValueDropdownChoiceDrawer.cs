using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Odin's [ValueDropdown] on a single value opens the pick window of <see cref="ChoiceFieldUtils"/>, as every enum field does, rather than Odin's selector: the attribute and its values member stay Odin's.<br/>
    /// Just above Odin's own drawer (2002); a list carrying it is still Odin's, which also adds its items.
    /// </summary>
    [DrawerPriority(0, 0, 2003)]
    public sealed class ValueDropdownChoiceDrawer<T> : OdinAttributeDrawer<ValueDropdownAttribute, T> {
        private ValueResolver<object> values;

        protected override bool CanDrawAttributeValueProperty(InspectorProperty property) {
            return property.ChildResolver is not ICollectionResolver;
        }

        protected override void Initialize() {
            values = ValueResolver.Get<object>(Property, Attribute.ValuesGetter);
        }

        protected override void DrawPropertyLayout(GUIContent label) {
            if (values.HasError) {
                values.DrawError();
                CallNextDrawer(label);
                return;
            }

            var rect = EditorGUILayout.GetControlRect(label != null);
            if (label != null) rect = EditorGUI.PrefixLabel(rect, label);
            EditorGUI.showMixedValue = ValueEntry.ValueState is PropertyValueState.PrimitiveValueConflict or PropertyValueState.ReferenceValueConflict;
            ChoiceFieldUtils.Value(rect, Choices(), ValueEntry.SmartValue, picked => Property.Tree.DelayActionUntilRepaint(() => ValueEntry.SmartValue = picked));
            EditorGUI.showMixedValue = false;
        }

        // An item named by the member, else by its value.
        private List<(string Name, T Value)> Choices() {
            var choices = new List<(string Name, T Value)>();
            if (values.GetValue() is not IEnumerable items) return choices;

            foreach (var item in items) {
                if (item is IValueDropdownItem named) choices.Add((named.GetText(), named.GetValue() is T value ? value : default));
                else if (item is T plain) choices.Add((plain.ToString(), plain));
            }

            return choices;
        }
    }
}