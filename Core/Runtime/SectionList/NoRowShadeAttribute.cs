using System;
using System.Diagnostics;

namespace RiseOn.Utils {
    /// <summary>
    /// The rows of a list drawn as a section of a settings page, a <see cref="SectionListAttribute"/> list's or a map's, without the shade on every other row: for rows that are sections of their own, whose bars already set them apart.<br/>
    /// On the list, or on the member that wraps it, such as a list type whose one field is the list.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class NoRowShadeAttribute : Attribute { }
}