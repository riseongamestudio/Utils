using System;

namespace RiseOn.Utils {
    /// <summary>
    /// Leaves Replace Type out of the context menu of a <c>[SerializeReference]</c> field, for values whose type cannot change by carrying fields over:<br/>
    /// a generic type whose other fields hold values of its type argument, say, which would land in fields of another type.<br/>
    /// On a field it applies to that field; on a class or an interface, to every field declared as that type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Interface)]
    public sealed class HideReplaceTypeAttribute : Attribute { }
}