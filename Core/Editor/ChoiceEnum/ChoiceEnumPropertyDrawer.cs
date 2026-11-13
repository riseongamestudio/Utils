using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Every enum field Unity draws, in an Inspector without Odin or by <see cref="EditorGUILayout.PropertyField(SerializedProperty)"/>, opens the pick window of <see cref="ChoiceFieldUtils"/> rather than the system menu, [Flags] ones as toggles.
    /// </summary>
    [CustomPropertyDrawer(typeof(Enum), true)]
    public sealed class ChoiceEnumPropertyDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            using var scope = new EditorGUI.PropertyScope(position, label, property);
            var rect = EditorGUI.PrefixLabel(position, scope.content);
            var type = ElementType(fieldInfo.FieldType);
            if (!type.IsEnum) {
                EditorGUI.PropertyField(rect, property, GUIContent.none);
                return;
            }

            // The pick comes after this draw, so it reads and writes the object on its own.
            var serialized = property.serializedObject;
            var path       = property.propertyPath;
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            ChoiceFieldUtils.Enum(rect, (Enum) Enum.ToObject(type, property.longValue), picked => {
                if (serialized.targetObject == null) return;

                serialized.Update();
                serialized.FindProperty(path).longValue = Convert.ToInt64(picked);
                serialized.ApplyModifiedProperties();
            });
            EditorGUI.showMixedValue = false;
        }

        // The field's own type, or what its array or list holds.
        private static Type ElementType(Type type) {
            if (type.IsArray) return type.GetElementType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) return type.GetGenericArguments()[0];
            return type;
        }
    }
}