using System;

namespace RiseOn.Utils {
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class ForwardAttributesToAttribute : Attribute {
        public string[] TargetNames { get; }
        public Type[] AdditionalAttributes { get; set; } = Type.EmptyTypes;
        public bool IncludeProcessorDefaults { get; set; } = true;

        public ForwardAttributesToAttribute(params string[] targetNames) {
            TargetNames = targetNames;
        }
    }
}