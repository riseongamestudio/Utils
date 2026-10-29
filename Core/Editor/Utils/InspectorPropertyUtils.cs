using Sirenix.OdinInspector.Editor;

namespace RiseOn.Utils.Editor {
    public static class InspectorPropertyUtils {
        /// <summary>
        /// Opens or closes <paramref name="property"/> and every property under it.
        /// </summary>
        public static void SetExpandedRecursively(this InspectorProperty property, bool isExpanded) {
            property.State.Expanded = isExpanded;
            foreach (var child in property.Children) child.SetExpandedRecursively(isExpanded);
        }
    }
}