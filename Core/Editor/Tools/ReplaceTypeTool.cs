using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Replaces the value of a <c>[SerializeReference]</c> field with a new instance of another type, keeping what the two types share.<br/>
    /// Every serialized field the new type has, with the name of a serialized field of the old value and a type that value fits, is carried over as it is:<br/>
    /// a held reference moves across rather than being rebuilt, so whatever the old value held stays.<br/>
    /// Other fields of the same object that shared the old value are pointed to the new one; a <c>[SerializeReference]</c> value cannot be shared across objects.
    /// </summary>
    public static class ReplaceTypeTool {
        private const BindingFlags FIELD_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        /// <param name="targets">The objects whose field is replaced, as many as are selected.</param>
        /// <param name="propertyPath">The Unity property path of the field in each of them.</param>
        /// <param name="type">The new type, or null to clear the field.</param>
        public static void Replace(IEnumerable<Object> targets, string propertyPath, Type type) {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Replace Type");

            foreach (var target in targets) {
                using var serializedObject = new SerializedObject(target);
                var property = serializedObject.FindProperty(propertyPath);
                if (property is not { propertyType: SerializedPropertyType.ManagedReference }) continue;

                var old = property.managedReferenceValue;
                if (old != null && old.GetType() == type) {
                    Debug.LogWarning($"RiseOn {nameof(ReplaceTypeTool)}: {target.name} already holds a {type.Name} (skipped).", target);
                    continue;
                }

                var oldId = property.managedReferenceId;
                var replacement = type == null ? null : Create(type);
                if (old != null && replacement != null) CopySharedFields(old, replacement);

                property.managedReferenceValue = replacement;
                if (oldId != ManagedReferenceUtility.RefIdNull) RepointShared(serializedObject, oldId, replacement);
                serializedObject.ApplyModifiedProperties();
            }

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            static object Create(Type type) {
                try {
                    return Activator.CreateInstance(type, true);
                } catch (MissingMethodException) {
                    return FormatterServices.GetUninitializedObject(type);
                }
            }

            // Other fields of the object that shared the old value now share the new one.
            static void RepointShared(SerializedObject serializedObject, long oldId, object replacement) {
                var iterator = serializedObject.GetIterator();
                while (iterator.Next(true)) {
                    if (iterator.propertyType is SerializedPropertyType.ManagedReference && iterator.managedReferenceId == oldId) iterator.managedReferenceValue = replacement;
                }
            }
        }

        private static void CopySharedFields(object from, object to) {
            foreach (var target in ListSerializedFields(to.GetType())) {
                var source = FindSerializedField(from.GetType(), target.Name);
                if (source == null) continue;

                var value = source.GetValue(from);
                if (value == null || target.FieldType.IsInstanceOfType(value)) target.SetValue(to, value);
            }

            static FieldInfo FindSerializedField(Type type, string name) {
                for (var current = type; current != null; current = current.BaseType) {
                    var field = current.GetField(name, FIELD_FLAGS);
                    if (field != null) return IsSerialized(field) ? field : null;
                }

                return null;
            }

            static IEnumerable<FieldInfo> ListSerializedFields(Type type) {
                for (var current = type; current != null; current = current.BaseType) {
                    foreach (var field in current.GetFields(FIELD_FLAGS)) {
                        if (IsSerialized(field)) yield return field;
                    }
                }
            }

            static bool IsSerialized(FieldInfo field) {
                if (field.IsNotSerialized || field.IsInitOnly || field.IsLiteral) return false;

                return field.IsPublic || field.IsDefined(typeof(SerializeField), false) || field.IsDefined(typeof(SerializeReference), false);
            }
        }
    }
}