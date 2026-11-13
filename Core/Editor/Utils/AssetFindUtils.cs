using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// <see cref="AssetDatabase.FindAssets(string)"/>'s paths, kept until the project changes: a search takes some 25 ms, too slow for a page that looks its assets up every draw.
    /// </summary>
    /// <example><code>
    /// var path = AssetFindUtils.Paths("t:MySettings").FirstOrDefault();
    /// </code></example>
    public static class AssetFindUtils {
        private static readonly Dictionary<string, string[]> found = new();

        static AssetFindUtils() {
            EditorApplication.projectChanged += found.Clear;
        }

        /// <summary>The paths of the assets <paramref name="filter"/> finds, as <see cref="AssetDatabase.FindAssets(string)"/> takes it.</summary>
        public static IReadOnlyList<string> Paths(string filter) {
            if (!found.TryGetValue(filter, out var paths)) found[filter] = paths = AssetDatabase.FindAssets(filter).Select(AssetDatabase.GUIDToAssetPath).ToArray();
            return paths;
        }
    }
}