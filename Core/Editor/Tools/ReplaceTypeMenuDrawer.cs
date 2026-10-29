using System.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Adds Replace Type to the context menu of every <c>[SerializeReference]</c> field, a <c>SerRef</c> value or a list element among them:<br/>
    /// the new type is picked with <see cref="GenericTypePicker"/>, among what Odin offers, plus generic types when the field has <see cref="AllowGenericsAttribute"/>,<br/>
    /// then <see cref="ReplaceTypeTool"/> carries over what the types share.<br/>
    /// The item is disabled while every target's value is None, as there is nothing to replace; the field's own picker sets a first type.<br/>
    /// A field, or a type, with <see cref="HideReplaceTypeAttribute"/> goes without it.<br/>
    /// It draws nothing itself.
    /// </summary>
    [DrawerPriority(0, 0, 3001)]
    public sealed class ReplaceTypeMenuDrawer<T> : OdinValueDrawer<T>, IDefinesGenericMenuItems where T : class {
        // The field's value part on its first line, where its picker button is, taken while the field draws: the picker opens under it, as wide as it.
        private Rect field;

        protected override bool CanDrawValueProperty(InspectorProperty property) {
            if (property.Info.SerializationBackend != SerializationBackend.UnityPolymorphic || property.Tree.UnitySerializedObject == null) return false;

            return property.GetAttribute<HideReplaceTypeAttribute>() == null && !property.ValueEntry.BaseValueType.IsDefined(typeof(HideReplaceTypeAttribute), true);
        }

        protected override void DrawPropertyLayout(GUIContent label) {
            var whole = EditorGUILayout.BeginVertical();
            CallNextDrawer(label);
            EditorGUILayout.EndVertical();

            if (Event.current.type is EventType.Repaint) field = SirenixEditorGUI.PrefixRect(whole.AlignTop(EditorGUIUtility.singleLineHeight), label, out _);
        }

        public void PopulateGenericMenu(InspectorProperty property, GenericMenu genericMenu) {
            var content = new GUIContent("Replace Type");
            if (property.ValueEntry.WeakValues.Cast<object>().All(value => value == null)) {
                genericMenu.AddDisabledItem(content);
                return;
            }

            // The menu item runs after the menu closes, outside any drawing; the picker opens on the next repaint, over the field.
            genericMenu.AddItem(content, false, () => {
                property.Tree.DelayActionUntilRepaint(() => Pick(property, field));
                InternalEditorUtility.RepaintAllViews();
            });
        }

        private static void Pick(InspectorProperty property, Rect at) {
            var rule = property.GetAttribute<AllowGenericsAttribute>();
            var targets = property.Tree.WeakTargets.OfType<Object>().ToArray();
            var path = property.UnityPropertyPath;

            GenericTypePicker.Pick(at, property.ValueEntry.BaseValueType, rule, type => {
                ReplaceTypeTool.Replace(targets, path, type);
                InternalEditorUtility.RepaintAllViews();
            });
        }
    }
}