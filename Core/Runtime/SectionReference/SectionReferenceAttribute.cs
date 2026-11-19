using System;
using System.Diagnostics;

namespace RiseOn.Utils {
    /// <summary>
    /// Draws a [SerializeReference] member as a folding section of a settings page: the value's script on the bar, read only, and its fields inside.<br/>
    /// For an instance the Editor picks itself, such as the project's one class that derives from a pack's base, so the bar offers no type picker.<br/>
    /// The bar takes the member's label and tooltip.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class SectionReferenceAttribute : Attribute { }
}