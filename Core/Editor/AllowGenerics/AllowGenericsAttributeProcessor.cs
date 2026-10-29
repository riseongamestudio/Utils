using System;
using System.Collections.Generic;
using System.Reflection;
using Sirenix.OdinInspector.Editor;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Gives a field the <see cref="AllowGenericsAttribute"/> of the type it is declared as, unless the field has one of its own.
    /// </summary>
    public sealed class AllowGenericsAttributeProcessor : OdinAttributeProcessor {
        public override bool CanProcessSelfAttributes(InspectorProperty property) {
            return GetTypeAttribute(property.Info.TypeOfValue) != null;
        }

        public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes) {
            foreach (var attribute in attributes) {
                if (attribute is AllowGenericsAttribute) return;
            }

            attributes.Add(GetTypeAttribute(property.Info.TypeOfValue));
        }

        private static AllowGenericsAttribute GetTypeAttribute(Type type) {
            return type?.GetCustomAttribute<AllowGenericsAttribute>(true);
        }
    }
}