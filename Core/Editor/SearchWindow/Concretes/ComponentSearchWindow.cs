using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Utils.Editor.SearchWindow {
    public class ComponentSearchWindow : SearchWindow {
        public static void Open(Rect btnRect, Action<Type> onSelected, string searchText = "") {
            var window = CreateInstance<ComponentSearchWindow>();
            window.minSize = new Vector2(333, 333);
            window.Show(btnRect, "Components", node => onSelected?.Invoke(node.Data as Type), searchText);
        }

        protected override void RegisterSections() {
            AddSection("Components", BuildComponentSection);
        }

        private void BuildComponentSection(SectionBuildContext ctx) {
            var root              = new SearchNode { Label = "Components" };
            var defaultScriptIcon = EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;
            var folderIcon        = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;

            // Fetch and rigorously filter Component types from the Unity TypeCache
            var componentTypes = TypeCache.GetTypesDerivedFrom<Component>()
                .Where(type => 
                        type.IsClass && 
                        !type.IsAbstract && 
                        type.IsPublic && 
                        !type.IsGenericTypeDefinition && // Lọc bỏ các class Generic (chưa xác định kiểu)
                        !IsHiddenByAttribute(type)       // Lọc bỏ các class bị giấu bởi AddComponentMenu("")
                )
                .ToList();

            var rootFolder = new FolderBuilder();

            for (int i = 0; i < componentTypes.Count; i++) {
                var    type     = componentTypes[i];
                string menuPath = GetMenuPath(type);

                var componentNode = new SearchNode {
                    Label       = type.Name,
                    LabelSearch = $"{type.Name} <color=#888888>({type.Namespace})</color>",
                    Icon        = AssetPreview.GetMiniTypeThumbnail(type) ?? defaultScriptIcon,
                    Data        = type
                };

                AddNodeToFolderBuilder(rootFolder, menuPath, componentNode);
            }

            BuildNodeHierarchy(rootFolder, root, folderIcon);
            ctx.Complete(root);
        }

        // Hàm hỗ trợ kiểm tra xem Component có bị cố tình giấu đi không
        private bool IsHiddenByAttribute(Type type) {
            var attributes = type.GetCustomAttributes(typeof(AddComponentMenu), false);
            if (attributes.Length > 0) {
                var addComponentMenu = attributes[0] as AddComponentMenu;
                // Nếu path rỗng hoặc null, tức là tác giả muốn giấu nó khỏi menu
                return string.IsNullOrEmpty(addComponentMenu?.componentMenu);
            }
            return false;
        }

        // Helper class to manage sorting before constructing the final read-only SearchTreeNodes
        private class FolderBuilder {
            // SortedDictionary automatically keeps subfolders sorted alphabetically
            public readonly SortedDictionary<string, FolderBuilder> SubFolders = new SortedDictionary<string, FolderBuilder>();
            public readonly List<SearchNode> Items = new List<SearchNode>();
        }

        private void AddNodeToFolderBuilder(FolderBuilder rootFolder, string path, SearchNode leafNode) {
            if (string.IsNullOrEmpty(path)) {
                rootFolder.Items.Add(leafNode);
                return;
            }

            var parts = path.Split('/');
            var currentFolder = rootFolder;

            for (int i = 0; i < parts.Length; i++) {
                var part = parts[i].Trim();
                if (string.IsNullOrEmpty(part)) continue;

                // Create intermediate folders if they don't exist
                if (!currentFolder.SubFolders.TryGetValue(part, out var childFolder)) {
                    childFolder = new FolderBuilder();
                    currentFolder.SubFolders[part] = childFolder;
                }

                currentFolder = childFolder;
            }

            currentFolder.Items.Add(leafNode);
        }

        private void BuildNodeHierarchy(FolderBuilder folder, SearchNode targetNode, Texture2D folderIcon) {
            // Traverse and add subfolders first (guaranteed alphabetical by SortedDictionary)
            foreach (var kvp in folder.SubFolders) {
                var folderNode = new SearchNode {
                    Label = kvp.Key,
                    Icon = folderIcon
                };
                
                BuildNodeHierarchy(kvp.Value, folderNode, folderIcon);
                targetNode.AddChild(folderNode);
            }

            // Sort individual components alphabetically before adding them below the folders
            folder.Items.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase));
            foreach (var item in folder.Items) {
                targetNode.AddChild(item);
            }
        }

        private string GetMenuPath(Type type) {
            var attrs = type.GetCustomAttributes(typeof(AddComponentMenu), false);
            
            if (attrs.Length > 0) {
                var attr = attrs[0] as AddComponentMenu;
                
                // Directly access the public property instead of using Reflection
                if (!string.IsNullOrEmpty(attr.componentMenu)) {
                    return attr.componentMenu;
                }
            }

            // Group built-in Unity components cleanly
            if (type.Namespace != null && type.Namespace.StartsWith("UnityEngine")) {
                return GetUnityComponentCategory(type);
            }

            // Fallback for custom scripts without the AddComponentMenu attribute
            if (!string.IsNullOrEmpty(type.Namespace)) {
                var ns = type.Namespace;
                if (ns.StartsWith("UnityEngine.")) {
                    ns = ns.Substring("UnityEngine.".Length);
                }
                return "Scripts/" + ns.Replace('.', '/');
            }

            return "Scripts";
        }

        private string GetUnityComponentCategory(Type type) {
            var typeName = type.Name;
            
            if (type.Namespace != null && type.Namespace.Contains("Physics") || typeName.Contains("Rigidbody") || typeName.Contains("Collider") || typeName.Contains("Joint")) {
                return "Physics";
            }
            if (type.Namespace != null && type.Namespace.Contains("Audio") || typeName.Contains("Audio")) {
                return "Audio";
            }
            if (type.Namespace != null && type.Namespace.Contains("UI")) {
                return "UI";
            }
            if (typeName.Contains("Renderer") || typeName.Contains("Light") || typeName.Contains("Camera")) {
                return "Rendering";
            }
            if (typeName.Contains("ParticleSystem") || typeName.Contains("Trail") || typeName.Contains("Line")) {
                return "Effects";
            }
            if (typeName.Contains("Mesh")) {
                return "Mesh";
            }
            if (type.Namespace != null && type.Namespace.Contains("Animation") || typeName.Contains("Animator") || typeName.Contains("Animation")) {
                return "Animation";
            }

            return "Miscellaneous";
        }
    }
}