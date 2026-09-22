using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

namespace RiseOn.Utils.Editor {
    public sealed class ForwardAttributeProcessor : OdinAttributeProcessor {
        private IEnumerable<Type> DefaultForwardableAttributes { get; } = new[] {
            typeof(InlineEditorAttribute)
          , typeof(RequiredAttribute)
          , typeof(PreviewFieldAttribute)
        };

        private ForwardAttributesToAttribute GetForwardingInfo(Type type) {
            if (type == null) return null;

            var attr = type.GetCustomAttribute<ForwardAttributesToAttribute>();
            if (attr != null) return attr;

            if (type.IsGenericType && !type.IsGenericTypeDefinition) {
                attr = type.GetGenericTypeDefinition().GetCustomAttribute<ForwardAttributesToAttribute>();
                if (attr != null) return attr;
            }

            if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string)) {
                if (type.IsArray) return GetForwardingInfo(type.GetElementType());

                foreach (var arg in type.GetGenericArguments()) {
                    var childAttr = GetForwardingInfo(arg);
                    if (childAttr != null) return childAttr;
                }
            }

            return null;
        }

        private HashSet<Type> GetTargetAttributeTypes(ForwardAttributesToAttribute info) {
            var types = new HashSet<Type>();

            if (info.IncludeProcessorDefaults) {
                foreach (var defaultType in DefaultForwardableAttributes) {
                    types.Add(defaultType);
                }
            }

            if (info.AdditionalAttributes != null) {
                foreach (var specificType in info.AdditionalAttributes) {
                    types.Add(specificType);
                }
            }

            return types;
        }

        public override bool CanProcessSelfAttributes(InspectorProperty property) {
            return GetForwardingInfo(property.Info.TypeOfValue) != null;
        }

        public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes) {
            var info = GetForwardingInfo(property.Info.TypeOfValue);
            if (info == null) return;

            var typesToForward = GetTargetAttributeTypes(info);

            for (int i = attributes.Count - 1; i >= 0; i--) {
                if (typesToForward.Contains(attributes[i].GetType())) {
                    attributes.RemoveAt(i);
                }
            }
        }

        public override bool CanProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member) {
            var info = GetForwardingInfo(parentProperty.Info.TypeOfValue);
            return info != null && info.TargetNames.Contains(member.Name);
        }

        public override void ProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member, List<Attribute> attributes) {
            var info = GetForwardingInfo(parentProperty.Info.TypeOfValue);
            if (info == null) return;

            var typesToForward = GetTargetAttributeTypes(info);
            var rawAttributes = parentProperty.Parent is { ChildResolver: ICollectionResolver }
                ? parentProperty.Parent.Info.Attributes
                : parentProperty.Info.Attributes;

            foreach (var rawAttr in rawAttributes) {
                var attrType = rawAttr.GetType();

                if (!typesToForward.Contains(attrType)) continue;
                
                bool alreadyExists = false;
                foreach (var attr in attributes) {
                    if (attr.GetType() != attrType) continue;

                    alreadyExists = true;
                    break;
                }
                
                if (alreadyExists) continue;

                attributes.Add(rawAttr);
            }
        }
    }
}