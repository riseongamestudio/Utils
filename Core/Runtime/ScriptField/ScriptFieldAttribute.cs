using System;
using System.Diagnostics;

namespace RiseOn.Utils {
    /// <summary>
    /// Shows a member holding a <see cref="Type"/> as the Inspector shows a component's script: read only, a click pings the script, a double click opens it.
    /// </summary>
    [Conditional("UNITY_EDITOR"), AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ScriptFieldAttribute : Attribute { }
}