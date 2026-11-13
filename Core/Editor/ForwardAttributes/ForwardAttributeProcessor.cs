using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Moves the attributes a <see cref="ForwardAttributesToAttribute"/> wrapper's field carries onto the wrapped member.<br/>
    /// Only attributes about the value itself that name no member move by default: a label, group or order stays with the field, and an expression naming the owner's members (ValidateInput, ValueDropdown, OnValueChanged...) would lose its owner.
    /// </summary>
    public sealed class ForwardAttributeProcessor : OdinAttributeProcessor {
        /// <summary>Moved from a wrapper's field, and from a list of wrappers onto each one.</summary>
        public static readonly HashSet<Type> ValueAttributes = new() {
            // Objects
            typeof(RequiredAttribute), typeof(RequiredInAttribute), typeof(InlineEditorAttribute), typeof(PreviewFieldAttribute),
            typeof(AssetsOnlyAttribute), typeof(SceneObjectsOnlyAttribute), typeof(ChildGameObjectsOnlyAttribute),
            typeof(AssetSelectorAttribute), typeof(DisallowModificationsInAttribute),
            // References
            typeof(HideReferenceObjectPickerAttribute), typeof(AllowGenericsAttribute),
            // Numbers
            typeof(MinValueAttribute), typeof(MaxValueAttribute), typeof(PropertyRangeAttribute), typeof(MinMaxSliderAttribute),
            typeof(WrapAttribute), typeof(ProgressBarAttribute), typeof(UnitAttribute), typeof(RangeAttribute), typeof(MinAttribute),
            // Text
            typeof(MultilineAttribute), typeof(TextAreaAttribute), typeof(MultiLinePropertyAttribute), typeof(FilePathAttribute),
            typeof(FolderPathAttribute), typeof(DisplayAsStringAttribute), typeof(DelayedAttribute), typeof(DelayedPropertyAttribute),
            // Enums and colours
            typeof(EnumToggleButtonsAttribute), typeof(EnumPagingAttribute), typeof(ColorUsageAttribute), typeof(ColorPaletteAttribute),
            // Beside the value
            typeof(SuffixLabelAttribute)
        };

        /// <summary>Moved only from a field whose wrapper itself wraps a list: a list of wrappers keeps them for itself.</summary>
        public static readonly HashSet<Type> CollectionAttributes = new() {
            typeof(ListDrawerSettingsAttribute), typeof(TableListAttribute), typeof(TableMatrixAttribute)
        };

        public override bool CanProcessSelfAttributes(InspectorProperty property) => Info(property.Info.TypeOfValue, out _) != null;

        // The field drops what moves, so nothing draws twice.
        public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes) {
            var info = Info(property.Info.TypeOfValue, out var ofElements);
            if (info == null) return;

            var moved = Moved(info, ofElements);
            attributes.RemoveAll(attribute => moved.Contains(attribute.GetType()));
        }

        public override bool CanProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member) {
            var info = Info(parentProperty.Info.TypeOfValue, out _);
            return info != null && info.TargetNames.Contains(member.Name);
        }

        public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes) {
            var info = Info(parentProperty.Info.TypeOfValue, out _);
            if (info == null) return;

            // A wrapper in a list takes what the list's field carries.
            var inList = parentProperty.Parent is { ChildResolver: ICollectionResolver };
            var source = inList ? parentProperty.Parent.Info.Attributes : parentProperty.Info.Attributes;
            var moved  = Moved(info, inList);
            foreach (var attribute in source) {
                var type = attribute.GetType();
                if (moved.Contains(type) && attributes.All(existing => existing.GetType() != type)) attributes.Add(attribute);
            }
        }

        private static HashSet<Type> Moved(ForwardAttributesToAttribute info, bool ofElements) {
            var moved = new HashSet<Type>();
            if (info.IncludeProcessorDefaults) {
                moved.UnionWith(ValueAttributes);
                if (!ofElements) moved.UnionWith(CollectionAttributes);
            }

            if (info.AdditionalAttributes != null) moved.UnionWith(info.AdditionalAttributes);
            return moved;
        }

        // The wrapper's forwarding, found on the type, its generic definition, or the element type of a list of wrappers.
        private static ForwardAttributesToAttribute Info(Type type, out bool ofElements) {
            ofElements = false;
            if (type == null) return null;

            var info = type.GetCustomAttribute<ForwardAttributesToAttribute>();
            if (info == null && type.IsGenericType && !type.IsGenericTypeDefinition) info = type.GetGenericTypeDefinition().GetCustomAttribute<ForwardAttributesToAttribute>();
            if (info != null || type == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(type)) return info;

            ofElements = true;
            if (type.IsArray) return Info(type.GetElementType(), out _);

            foreach (var argument in type.GetGenericArguments()) {
                info = Info(argument, out _);
                if (info != null) return info;
            }

            return null;
        }
    }
}