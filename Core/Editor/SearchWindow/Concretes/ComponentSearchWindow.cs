using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Utils.Editor.SearchWindow {
    /// <summary>
    /// Picks a component type from the same set Unity's Add Component offers, in the same folders: native components as
    /// the Component menu files them, scripts by their AddComponentMenu path or else by namespace.
    /// </summary>
    public class ComponentSearchWindow : SearchWindow {
        private const string ComponentsLabel   = "Components";
        private const string ScriptsFolder     = "Scripts";
        private const string ComponentMenuRoot = "Component";

        // Component types only change with a domain reload, which also clears this.
        private static SearchNode cachedRoot;

        public static void Open(Rect btnRect, Action<Type> onSelected, string searchText = "") {
            var window = CreateInstance<ComponentSearchWindow>();
            window.Show(btnRect, ComponentsLabel, node => onSelected?.Invoke(node.Data as Type), new Vector2(333, 333), searchText);
        }

        protected override void RegisterSections() {
            AddSection(ComponentsLabel, ctx => ctx.Complete(cachedRoot ??= BuildTree()));
        }

        private static SearchNode BuildTree() {
            var root              = new SearchNode { Label = ComponentsLabel };
            var defaultScriptIcon = EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;
            var folderIcon        = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
            var rootFolder        = new FolderBuilder();
            var nativeFolders     = GetNativeMenuFolders();
            var scriptTypes       = GetScriptTypes();

            foreach (var type in TypeCache.GetTypesDerivedFrom<Component>()) {
                if (!type.IsClass || type.IsAbstract || !type.IsPublic || type.IsGenericTypeDefinition) continue;

                // AddComponentMenu("") hides a component from the menu on purpose.
                var menu = (AddComponentMenu)Attribute.GetCustomAttribute(type, typeof(AddComponentMenu), false);
                if (menu != null && string.IsNullOrEmpty(menu.componentMenu)) continue;

                string folder;
                if (typeof(MonoBehaviour).IsAssignableFrom(type)) {
                    // Unity only stores a script component whose class sits in a file of the same name; any other one
                    // turns into a missing script on the next reload.
                    if (!scriptTypes.Contains(type)) continue;

                    folder = menu != null ? GetFolder(menu.componentMenu) : GetScriptFolder(type);
                } else if (!nativeFolders.TryGetValue(type.Name, out folder)) {
                    // Not in the Component menu: a base class (Collider, Renderer), one Unity adds by itself (Transform,
                    // ParticleSystemRenderer), or one from a built-in module that is turned off.
                    continue;
                }

                var componentNode = new SearchNode {
                    Label       = type.Name,
                    LabelSearch = string.IsNullOrEmpty(type.Namespace) ? null : $"{type.Name} <color=#888888>({type.Namespace})</color>",
                    SearchName  = type.Name,
                    IconLoader  = () => AssetPreview.GetMiniTypeThumbnail(type) ?? defaultScriptIcon,
                    Data        = type
                };

                AddNodeToFolderBuilder(rootFolder, folder, componentNode);
            }

            BuildNodeHierarchy(rootFolder, root, folderIcon);
            return root;
        }

        /// <summary>
        /// The folder of every native component in Unity's Component menu, keyed by type name: "Component/Physics/Box
        /// Collider" gives BoxCollider → "Physics". The menu already leaves out what cannot be added by hand.
        /// </summary>
        private static Dictionary<string, string> GetNativeMenuFolders() {
            var folders = new Dictionary<string, string>();
            foreach (var item in Unsupported.GetSubmenus(ComponentMenuRoot)) {
                var firstSlash = item.IndexOf('/');
                var lastSlash  = item.LastIndexOf('/');
                if (lastSlash <= firstSlash) continue; // a command right under Component, like "Add..."

                var typeName = item.Substring(lastSlash + 1).Replace(" ", string.Empty);
                folders.TryAdd(typeName, item.Substring(firstSlash + 1, lastSlash - firstSlash - 1));
            }

            return folders;
        }

        /// <summary>Classes that have a script asset of their own, the ones Add Component lists.</summary>
        private static HashSet<Type> GetScriptTypes() {
            var types = new HashSet<Type>();
            foreach (var script in MonoImporter.GetAllRuntimeMonoScripts()) {
                var type = script.GetClass();
                if (type != null) types.Add(type);
            }

            return types;
        }

        /// <summary>An AddComponentMenu path ends with the item's own name, so the folder is everything before it.</summary>
        private static string GetFolder(string menuPath) {
            var lastSlash = menuPath.LastIndexOf('/');
            return lastSlash < 0 ? string.Empty : menuPath.Substring(0, lastSlash);
        }

        /// <summary>Scripts without AddComponentMenu are grouped by namespace.</summary>
        private static string GetScriptFolder(Type type) {
            return string.IsNullOrEmpty(type.Namespace) ? ScriptsFolder : $"{ScriptsFolder}/{type.Namespace.Replace('.', '/')}";
        }

        private class FolderBuilder {
            // SortedDictionary keeps subfolders in alphabetical order.
            public readonly SortedDictionary<string, FolderBuilder> SubFolders = new();
            public readonly List<SearchNode>                        Items      = new();
        }

        private static void AddNodeToFolderBuilder(FolderBuilder rootFolder, string path, SearchNode leafNode) {
            if (string.IsNullOrEmpty(path)) {
                rootFolder.Items.Add(leafNode);
                return;
            }

            var currentFolder = rootFolder;
            foreach (var rawPart in path.Split('/')) {
                var part = rawPart.Trim();
                if (string.IsNullOrEmpty(part)) continue;

                if (!currentFolder.SubFolders.TryGetValue(part, out var childFolder)) {
                    childFolder                    = new FolderBuilder();
                    currentFolder.SubFolders[part] = childFolder;
                }

                currentFolder = childFolder;
            }

            currentFolder.Items.Add(leafNode);
        }

        private static void BuildNodeHierarchy(FolderBuilder folder, SearchNode targetNode, Texture2D folderIcon) {
            foreach (var subFolder in folder.SubFolders) {
                var folderNode = new SearchNode { Label = subFolder.Key, Icon = folderIcon };
                BuildNodeHierarchy(subFolder.Value, folderNode, folderIcon);
                targetNode.AddChild(folderNode);
            }

            // Components come after the folders, in alphabetical order.
            folder.Items.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase));
            foreach (var item in folder.Items) targetNode.AddChild(item);
        }
    }
}
