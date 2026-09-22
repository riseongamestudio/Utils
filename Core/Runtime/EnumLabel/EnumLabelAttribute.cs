using System;
using System.Diagnostics;

namespace RiseOn.Utils {
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class EnumLabelAttribute : Attribute {
        public Type DefaultType { get; }

        public EnumLabelAttribute(Type defaultType = null) {
            if (defaultType is not null && !defaultType.IsEnum) {
                throw new ArgumentException("Default type must be an enum.", nameof(defaultType));
            }

            DefaultType = defaultType;
        }
    }
}
