using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace RiseOn.Utils.Editor.SearchWindow {
    public class ObjectSearchWindow : SearchWindow {
        private const int MaxMillisecondsPerFrame = 16;

        // Asset scans per filter type, kept until an asset changes (see ObjectSearchCacheInvalidator). Holding the matched
        // objects also keeps their prefabs loaded, so reopening the window neither scans nor reloads anything.
        private static readonly Dictionary<Type, List<AssetMatch>> assetCache = new();
        private static          int                                assetCacheVersion;

        private readonly Dictionary<Type, bool> typeMatches = new();

        private Type       filterType;
        private GameObject rootPrefab;
        private bool       allowSceneObjects;

        /// <param name="current">
        /// The value the field holds now. The window opens on its tab with it highlighted; empty highlights None.
        /// </param>
        public static void Open(
            Rect btnRect,
            string title,
            Action<SearchNode> onSelected,
            Type filterType,
            GameObject rootPrefab = null,
            bool allowSceneObjects = true,
            Object current = null) {

            var window = CreateInstance<ObjectSearchWindow>();
            window.filterType        = filterType ?? typeof(Object);
            window.rootPrefab        = rootPrefab;
            window.allowSceneObjects = allowSceneObjects;

            // Unity's == on purpose: a destroyed or missing object counts as empty.
            window.PreselectOnOpen(current == null ? null : current);
            window.Show(btnRect, title, onSelected, defaultSize: new Vector2(777, 555));
        }

        /// <summary>The last tab is remembered per filter type, since a material and a component live in different tabs.</summary>
        protected override string StateKey => $"{nameof(ObjectSearchWindow)}.{filterType.FullName}";

        internal static void ClearAssetCache() {
            assetCache.Clear();
            assetCacheVersion++;
        }

        protected override void RegisterSections() {
            AddSection("Scene", BuildSceneSection);
            AddSection("Assets", BuildAssetsSectionAsync);
        }

        #region Type matching

        private bool TypeMatches(Type candidate) {
            if (candidate == null) return false;
            if (typeMatches.TryGetValue(candidate, out var matches)) return matches;

            matches                = MatchesUncached(candidate);
            typeMatches[candidate] = matches;
            return matches;
        }

        private bool MatchesUncached(Type candidate) {
            if (filterType.IsAssignableFrom(candidate)) return true;
            if (!filterType.IsGenericTypeDefinition) return false;

            foreach (var iface in candidate.GetInterfaces()) {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == filterType) return true;
            }

            for (var current = candidate; current != null && current != typeof(object); current = current.BaseType) {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == filterType) return true;
            }

            return false;
        }

        /// <summary>
        /// Unity can look the filter up by itself (FindObjectsByType, GetComponents) only for a component class or an
        /// interface. An open generic type has no instances, so it goes through <see cref="TypeMatches"/>.
        /// </summary>
        private bool CanQueryComponentsDirectly => !filterType.IsGenericTypeDefinition
                                                && (filterType.IsInterface || typeof(Component).IsAssignableFrom(filterType));

        /// <summary>Concrete <see cref="Object"/> types the filter accepts.</summary>
        private List<Type> GetConcreteTypes() {
            var concreteTypes = new List<Type>();
            if (!filterType.IsAbstract && !filterType.IsInterface && !filterType.IsGenericTypeDefinition) {
                concreteTypes.Add(filterType);
            }

            // TypeCache answers interfaces and open generic types too, far faster than testing every Object type.
            foreach (var type in TypeCache.GetTypesDerivedFrom(filterType)) {
                if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition) continue;
                if (!typeof(Object).IsAssignableFrom(type)) continue;

                concreteTypes.Add(type);
            }

            return concreteTypes;
        }

        #endregion

        #region Scene

        /// <summary>A GameObject is not a component, so a GameObject or Object filter lists the objects themselves too.</summary>
        private bool AcceptsGameObjects => filterType.IsAssignableFrom(typeof(GameObject));

        /// <summary>False for filters like Material or GameObject, which no component can match; the component scan is skipped.</summary>
        private bool AcceptsComponents => filterType.IsInterface
                                       || filterType.IsGenericTypeDefinition
                                       || filterType.IsAssignableFrom(typeof(Component))
                                       || typeof(Component).IsAssignableFrom(filterType);

        private void BuildSceneSection(SectionBuildContext ctx) {
            var root = new SearchNode { Label = "Scene" };
            root.AddChild(ConstructNoneNode());

            var gameObjects = new List<GameObject>();
            var components  = new List<Component>();
            if (rootPrefab != null) {
                if (AcceptsComponents) CollectComponents(rootPrefab.GetComponentsInChildren<Component>(includeInactive: true), components);
                if (AcceptsGameObjects) {
                    foreach (var transform in rootPrefab.GetComponentsInChildren<Transform>(includeInactive: true)) gameObjects.Add(transform.gameObject);
                }
            } else if (allowSceneObjects) {
                if (AcceptsComponents) FindSceneComponents(components);
                if (AcceptsGameObjects) gameObjects.AddRange(FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            }

            BuildSceneTree(root, gameObjects, components);
            ctx.Complete(root);
        }

        private void FindSceneComponents(List<Component> results) {
            // A component class is looked up by Unity directly, several times faster than listing every component.
            // FindObjectsByType does not take interfaces or open generic types, so those filter the full list.
            if (CanQueryComponentsDirectly && !filterType.IsInterface) {
                foreach (var found in FindObjectsByType(filterType, FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                    results.Add((Component)found);
                }

                return;
            }

            CollectComponents(FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.None), results);
        }

        private void CollectComponents(Component[] candidates, List<Component> results) {
            foreach (var component in candidates) {
                // Missing scripts come back as null entries.
                if (component != null && TypeMatches(component.GetType())) results.Add(component);
            }
        }

        /// <summary>
        /// One row per GameObject, in the order found. An object with several matches (itself, its components) becomes a
        /// folder named by its path.
        /// </summary>
        private static void BuildSceneTree(SearchNode root, List<GameObject> gameObjects, List<Component> components) {
            var groups = new Dictionary<GameObject, SceneGroup>();
            var order  = new List<GameObject>();

            SceneGroup GroupOf(GameObject gameObject) {
                if (groups.TryGetValue(gameObject, out var group)) return group;

                group = new SceneGroup();
                groups.Add(gameObject, group);
                order.Add(gameObject);
                return group;
            }

            foreach (var gameObject in gameObjects) {
                if (gameObject != null) GroupOf(gameObject).IncludesSelf = true;
            }

            foreach (var component in components) {
                if (component != null) GroupOf(component.gameObject).Components.Add(component);
            }

            var pathParts = new List<string>();
            foreach (var gameObject in order) {
                var group = groups[gameObject];
                var name  = RichText.Literal(gameObject.name);
                var path  = RichText.Literal(GetHierarchyPath(gameObject.transform, pathParts));

                if (group.Count == 1) {
                    root.AddChild(group.IncludesSelf
                        ? CreateGameObjectNode(gameObject, path)
                        : CreateComponentNode(group.Components[0], name, path));
                    continue;
                }

                var groupNode = new SearchNode { Label = path, IconLoader = () => PrefabUtility.GetIconForGameObject(gameObject) };
                if (group.IncludesSelf) {
                    groupNode.AddChild(new SearchNode {
                        Label       = $"<color=#888888>{name}</color> GameObject",
                        LabelSearch = $"{name} {RichText.Literal("<GameObject>")} <color=#888888>({path})</color>",
                        SearchName  = $"{gameObject.name} GameObject",
                        IconLoader  = () => PrefabUtility.GetIconForGameObject(gameObject),
                        Data        = gameObject
                    });
                }

                AddComponentNodes(groupNode, group.Components, _ => name, path);
                root.AddChild(groupNode);
            }
        }

        /// <summary>A row of its own: "Name &lt;Type&gt; (where it is)".</summary>
        private static SearchNode CreateComponentNode(Component component, string name, string where) {
            var gameObject = component.gameObject;
            var typeName   = component.GetType().Name;
            return new SearchNode {
                Label      = $"{name} {RichText.Literal($"<{typeName}>")} <color=#888888>({where})</color>",
                SearchName = $"{gameObject.name} {typeName}",
                IconLoader = () => PrefabUtility.GetIconForGameObject(gameObject),
                Data       = component
            };
        }

        private static SearchNode CreateGameObjectNode(GameObject gameObject, string path) {
            return new SearchNode {
                Label      = $"{RichText.Literal(gameObject.name)} {RichText.Literal("<GameObject>")} <color=#888888>({path})</color>",
                SearchName = $"{gameObject.name} GameObject",
                IconLoader = () => PrefabUtility.GetIconForGameObject(gameObject),
                Data       = gameObject
            };
        }

        /// <summary>
        /// One child per component, labelled with the object it sits on. Repeated types on the same object get an index,
        /// like "Collider (1)".
        /// </summary>
        private static void AddComponentNodes(SearchNode groupNode, List<Component> components, Func<Component, string> nameOf, string path) {
            var indices = new Dictionary<(GameObject, Type), int>();
            foreach (var component in components) {
                var type   = component.GetType();
                var key    = (component.gameObject, type);
                var suffix = string.Empty;
                if (indices.TryGetValue(key, out var index)) {
                    indices[key] = ++index;
                    suffix       = $" ({index})";
                } else {
                    indices.Add(key, 0);
                }

                var name = nameOf(component);
                groupNode.AddChild(new SearchNode {
                    Label       = $"<color=#888888>{name}</color> {type.Name}{suffix}",
                    LabelSearch = $"{name} {RichText.Literal($"<{type.Name}{suffix}>")} <color=#888888>({path})</color>",
                    SearchName  = $"{component.gameObject.name} {type.Name}",
                    IconLoader  = () => EditorGUIUtility.ObjectContent(component, component.GetType()).image as Texture2D,
                    Data        = component
                });
            }
        }

        private sealed class SceneGroup {
            public readonly List<Component> Components = new();
            public          bool            IncludesSelf;

            public int Count => Components.Count + (IncludesSelf ? 1 : 0);
        }

        private static string GetHierarchyPath(Transform transform, List<string> parts) {
            parts.Clear();
            for (var current = transform; current != null; current = current.parent) parts.Add(current.name);

            parts.Reverse();
            return string.Join("/", parts);
        }

        #endregion

        #region Assets

        private async void BuildAssetsSectionAsync(SectionBuildContext ctx) {
            var root = new SearchNode { Label = "Assets" };
            root.AddChild(ConstructNoneNode());

            if (assetCache.TryGetValue(filterType, out var cachedMatches) && AreAlive(cachedMatches)) {
                BuildAssetTree(root, cachedMatches);
                ctx.Complete(root);
                return;
            }

            ctx.ReportProgress(0f);

            try {
                var cacheVersion = assetCacheVersion;
                var matches      = new List<AssetMatch>();

                bool isComponentType        = typeof(Component).IsAssignableFrom(filterType);
                bool isScriptableObjectType = typeof(ScriptableObject).IsAssignableFrom(filterType);

                if (filterType.IsInterface || (filterType.IsAbstract && !isComponentType && !isScriptableObjectType)) {
                    await ScanPrefabsAsync(GetAllPrefabPaths(), matches, ctx);
                    if (this == null) return;

                    var paths = await CollectAssetPathsAsync(GetConcreteTypes(), ctx);
                    if (this == null) return;

                    await LoadAssetsFromPathsAsync(paths, matches, ctx);
                } else if (isComponentType) {
                    await ScanPrefabsAsync(GetAllPrefabPaths(), matches, ctx);
                } else if (isScriptableObjectType) {
                    var paths = await CollectAssetPathsAsync(GetConcreteTypes(), ctx);
                    if (this == null) return;

                    await LoadAssetsFromPathsAsync(paths, matches, ctx);
                } else {
                    var paths = new List<string>();
                    foreach (var guid in AssetDatabase.FindAssets($"t:{filterType.Name}")) paths.Add(AssetDatabase.GUIDToAssetPath(guid));

                    await LoadAssetsFromPathsAsync(paths, matches, ctx);
                }

                // Closed mid-scan: the matches are partial, so they are neither shown nor cached.
                if (this == null) return;

                // An asset that changed during the scan may be missing from the matches, so only cache a clean run.
                if (cacheVersion == assetCacheVersion) assetCache[filterType] = matches;

                BuildAssetTree(root, matches);
                ctx.Complete(root);
            } catch (Exception ex) {
                Debug.LogError($"[{nameof(ObjectSearchWindow)}] Asset scanning failed: {ex.Message}\n{ex.StackTrace}");
                if (this != null) ctx.Complete(root);
            }
        }

        private static bool AreAlive(List<AssetMatch> matches) {
            foreach (var match in matches) {
                if (match.Component == null && match.Asset == null) return false;
            }

            return true;
        }

        private static List<string> GetAllPrefabPaths() {
            var guids = AssetDatabase.FindAssets("t:Prefab");
            var paths = new List<string>(guids.Length);
            foreach (var guid in guids) paths.Add(AssetDatabase.GUIDToAssetPath(guid));

            return paths;
        }

        /// <summary>
        /// Loads every prefab and keeps the components that match, on the root or any child, active or not. Filtering
        /// prefabs first through AssetDatabase.GetDependencies was measured slower than just loading them (about 0.6 ms
        /// per prefab either way), so there is no pre-filter.
        /// </summary>
        private async Task ScanPrefabsAsync(List<string> paths, List<AssetMatch> results, SectionBuildContext ctx) {
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < paths.Count; i++) {
                var path   = paths[i];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) AddPrefabComponents(prefab, path, results);

                if (stopwatch.ElapsedMilliseconds > MaxMillisecondsPerFrame) {
                    ctx.ReportProgress((float)(i + 1) / paths.Count);
                    await Task.Yield();
                    if (this == null) return;

                    stopwatch.Restart();
                }
            }
        }

        private void AddPrefabComponents(GameObject prefab, string path, List<AssetMatch> results) {
            if (CanQueryComponentsDirectly) {
                foreach (var component in prefab.GetComponentsInChildren(filterType, includeInactive: true)) {
                    if (component != null) results.Add(new AssetMatch { Component = component, AssetPath = path });
                }

                return;
            }

            foreach (var component in prefab.GetComponentsInChildren<Component>(includeInactive: true)) {
                if (component != null && TypeMatches(component.GetType())) {
                    results.Add(new AssetMatch { Component = component, AssetPath = path });
                }
            }
        }

        private async Task<List<string>> CollectAssetPathsAsync(List<Type> concreteTypes, SectionBuildContext ctx) {
            var paths     = new List<string>();
            var seen      = new HashSet<string>();
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < concreteTypes.Count; i++) {
                var concreteType = concreteTypes[i];
                if (typeof(Component).IsAssignableFrom(concreteType) || typeof(GameObject).IsAssignableFrom(concreteType)) continue;

                foreach (var guid in AssetDatabase.FindAssets($"t:{concreteType.Name}")) {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!string.IsNullOrEmpty(path) && seen.Add(path)) paths.Add(path);
                }

                if (stopwatch.ElapsedMilliseconds > MaxMillisecondsPerFrame) {
                    ctx.ReportProgress((float)(i + 1) / concreteTypes.Count * 0.3f);
                    await Task.Yield();
                    if (this == null) return paths;

                    stopwatch.Restart();
                }
            }

            return paths;
        }

        private async Task LoadAssetsFromPathsAsync(List<string> paths, List<AssetMatch> results, SectionBuildContext ctx) {
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < paths.Count; i++) {
                var path = paths[i];
                if (string.IsNullOrEmpty(path)) continue;

                var mainType = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (mainType == null) continue;

                bool mainTypeMatches = TypeMatches(mainType);
                if (mainTypeMatches) {
                    var asset = AssetDatabase.LoadMainAssetAtPath(path);
                    if (asset != null) results.Add(new AssetMatch { Asset = asset, AssetPath = path });
                }

                if (AssetDatabase.IsValidFolder(path)) continue;

                if (!mainTypeMatches || typeof(Object).IsAssignableFrom(filterType)) {
                    foreach (var subAsset in AssetDatabase.LoadAllAssetRepresentationsAtPath(path)) {
                        if (subAsset != null && TypeMatches(subAsset.GetType())) {
                            results.Add(new AssetMatch { Asset = subAsset, AssetPath = path });
                        }
                    }
                }

                if (stopwatch.ElapsedMilliseconds > MaxMillisecondsPerFrame) {
                    ctx.ReportProgress(0.3f + (float)(i + 1) / paths.Count * 0.7f);
                    await Task.Yield();
                    if (this == null) return;

                    stopwatch.Restart();
                }
            }
        }

        private static void BuildAssetTree(SearchNode root, List<AssetMatch> matches) {
            var groupedByAsset = new Dictionary<string, List<AssetMatch>>();
            var assetPaths     = new List<string>();
            foreach (var match in matches) {
                if (!groupedByAsset.TryGetValue(match.AssetPath, out var list)) {
                    list                            = new List<AssetMatch>();
                    groupedByAsset[match.AssetPath] = list;
                    assetPaths.Add(match.AssetPath);
                }

                list.Add(match);
            }

            var pathParts = new List<string>();
            foreach (var assetPath in assetPaths) {
                var assetMatches = groupedByAsset[assetPath];
                var path         = RichText.Literal(assetPath);

                if (assetMatches[0].Component == null) {
                    var fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                    foreach (var match in assetMatches) {
                        var asset    = match.Asset;
                        var name     = AssetDatabase.IsSubAsset(asset) ? asset.name : fileName; // a sub-asset has its own name
                        var typeName = asset.GetType().Name;
                        root.AddChild(new SearchNode {
                            Label      = $"{RichText.Literal(name)} {RichText.Literal($"<{typeName}>")} <color=#888888>({path})</color>",
                            SearchName = $"{name} {typeName}",
                            IconLoader = () => AssetPreview.GetMiniThumbnail(asset),
                            Data       = asset
                        });
                    }

                    continue;
                }

                // Components can sit anywhere in the prefab, so each is named by its path inside it, like "Enemy/Weapon".
                var components = new List<Component>(assetMatches.Count);
                foreach (var match in assetMatches) components.Add(match.Component);

                if (components.Count == 1) {
                    var component = components[0];
                    root.AddChild(CreateComponentNode(component, RichText.Literal(GetHierarchyPath(component.transform, pathParts)), path));
                    continue;
                }

                var prefabRoot = components[0].transform.root.gameObject;
                var groupNode  = new SearchNode { Label = path, IconLoader = () => PrefabUtility.GetIconForGameObject(prefabRoot) };
                AddComponentNodes(groupNode, components, component => RichText.Literal(GetHierarchyPath(component.transform, pathParts)), path);
                root.AddChild(groupNode);
            }
        }

        private class AssetMatch {
            public Component Component;
            public Object    Asset;
            public string    AssetPath;
        }

        #endregion
    }

    /// <summary>Drops the asset scans of <see cref="ObjectSearchWindow"/> whenever an asset is imported, deleted or moved.</summary>
    internal sealed class ObjectSearchCacheInvalidator : AssetPostprocessor {
        private static void OnPostprocessAllAssets(
            string[] importedAssets
          , string[] deletedAssets
          , string[] movedAssets
          , string[] movedFromAssetPaths) {
            ObjectSearchWindow.ClearAssetCache();
        }
    }
}
