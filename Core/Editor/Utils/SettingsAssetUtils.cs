using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Finds, makes and moves a pack's settings asset, which the game loads from Resources by its name.<br/>
    /// A new asset goes to Assets/RiseOn/Resources; one found elsewhere is the project's still, under any folder, so a page can report it and offer <see cref="MoveToResources{T}"/>.
    /// </summary>
    /// <example><code>
    /// var settings = SettingsAssetUtils.LoadOrCreate&lt;MySettings&gt;(MySettings.ResourceName);
    /// if (SettingsAssetUtils.OutsideResources&lt;MySettings&gt;(MySettings.ResourceName) is { } path) Report($"{path} is outside a Resources folder");
    /// </code></example>
    public static class SettingsAssetUtils {
        /// <summary>Where a new settings asset goes.</summary>
        public static string DefaultPath(string resourceName) => "Assets/RiseOn/Resources/" + resourceName + ".asset";

        /// <summary>The path of the project's <typeparamref name="T"/> named <paramref name="resourceName"/>, in any folder; null when there is none.</summary>
        public static string FindPath<T>(string resourceName) where T : ScriptableObject =>
            AssetFindUtils.Paths("t:" + typeof(T).Name).FirstOrDefault(path => Path.GetFileNameWithoutExtension(path) == resourceName);

        /// <summary>The project's asset; null when there is none.</summary>
        public static T Find<T>(string resourceName) where T : ScriptableObject {
            var path = FindPath<T>(resourceName);
            return path == null ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        }

        /// <summary>The project's asset, made at <see cref="DefaultPath"/> when there is none.</summary>
        public static T LoadOrCreate<T>(string resourceName) where T : ScriptableObject {
            var found = Find<T>(resourceName);
            if (found) return found;

            var path = DefaultPath(resourceName);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var settings = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
            return settings;
        }

        /// <summary>The asset's path when the game cannot load it, being outside every Resources folder; null when it can, or when there is none.</summary>
        public static string OutsideResources<T>(string resourceName) where T : ScriptableObject {
            var path = FindPath<T>(resourceName);
            return path == null || path.Contains("/Resources/") ? null : path;
        }

        /// <summary>Moves the asset to <see cref="DefaultPath"/>, logging why when it cannot.</summary>
        public static void MoveToResources<T>(string resourceName) where T : ScriptableObject {
            var path  = FindPath<T>(resourceName);
            var error = path == null ? null : AssetDatabase.MoveAsset(path, DefaultPath(resourceName));
            if (!string.IsNullOrEmpty(error)) Debug.LogError($"{nameof(SettingsAssetUtils)}: {path} could not move to {DefaultPath(resourceName)} ({error})");
        }
    }
}