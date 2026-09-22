using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RiseOn.Utils.Editor.SearchWindow {
    public class ObjectSearchWindow : SearchWindow {
        private const int MAX_MS_PER_FRAME = 16;

        private Type filterType;
        private GameObject rootPrefab;
        private bool allowSceneObjects;
        private List<Type> cachedConcreteTypes;

        public static void Open(
            Rect btnRect,
            string title,
            Action<SearchNode> onSelected,
            Type filterType,
            GameObject rootPrefab = null,
            bool allowSceneObjects = true) {

            var window = CreateInstance<ObjectSearchWindow>();
            window.filterType = filterType ?? typeof(Object);
            window.rootPrefab = rootPrefab;
            window.allowSceneObjects = allowSceneObjects;
            window.minSize = new Vector2(777, 555);
            window.Show(btnRect, title, onSelected);
        }

        protected override void RegisterSections() {
            AddSection("Scene", BuildSceneSection);
            AddSection("Assets", BuildAssetsSectionAsync);
        }

        private List<Type> GetConcreteTypes() {
            if (cachedConcreteTypes != null) return cachedConcreteTypes;
            cachedConcreteTypes = new List<Type>();

            if (filterType.IsInterface || filterType.IsAbstract) {
                var allUnityTypes = TypeCache.GetTypesDerivedFrom<Object>();
                foreach (var t in allUnityTypes) {
                    if (t.IsAbstract || t.IsInterface || t.IsGenericTypeDefinition) continue;
                    if (TypeMatches(t)) cachedConcreteTypes.Add(t);
                }
            } else {
                var derivedTypes = TypeCache.GetTypesDerivedFrom(filterType);
                foreach (var t in derivedTypes) {
                    if (t.IsAbstract || t.IsInterface || t.IsGenericTypeDefinition) continue;
                    cachedConcreteTypes.Add(t);
                }
                if (!cachedConcreteTypes.Contains(filterType))
                    cachedConcreteTypes.Add(filterType);
            }

            return cachedConcreteTypes;
        }

        private bool TypeMatches(Type candidate) {
            if (filterType == null || candidate == null) return filterType == null;
            if (filterType.IsAssignableFrom(candidate)) return true;

            if (filterType.IsGenericTypeDefinition) {
                foreach (var iface in candidate.GetInterfaces()) {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == filterType) return true;
                }
                for (var cur = candidate; cur != null && cur != typeof(object); cur = cur.BaseType) {
                    if (cur.IsGenericType && cur.GetGenericTypeDefinition() == filterType) return true;
                }
                return false;
            }

            if (filterType.IsGenericType) {
                var def = filterType.GetGenericTypeDefinition();
                foreach (var iface in candidate.GetInterfaces()) {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == def && iface == filterType) return true;
                }
                for (var cur = candidate; cur != null && cur != typeof(object); cur = cur.BaseType) {
                    if (cur.IsGenericType && cur.GetGenericTypeDefinition() == def && cur == filterType) return true;
                }
            }

            return false;
        }

        #region Scene Hierarchy Scanning

        private void BuildSceneSection(SectionBuildContext ctx) {
            var root = new SearchNode { Label = "Scene" };
            root.AddChild(ConstructNoneNode());

            var sceneComponents = new List<Component>();
            if (rootPrefab != null) {
                sceneComponents.AddRange(rootPrefab.GetComponentsInChildren(filterType, includeInactive: true));
            } else if (allowSceneObjects) {
                foreach (var cpn in FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                    if (!TypeMatches(cpn.GetType())) continue;
                    sceneComponents.Add(cpn);
                }
            }

            BuildSceneTree(root, sceneComponents);
            ctx.Complete(root);
        }

        private void BuildSceneTree(SearchNode root, List<Component> components) {
            var groupedByGameObject = new Dictionary<GameObject, List<Component>>();
            foreach (var comp in components) {
                if (comp == null || comp.gameObject == null) continue;
                if (!groupedByGameObject.TryGetValue(comp.gameObject, out var list)) {
                    list = new List<Component>();
                    groupedByGameObject[comp.gameObject] = list;
                }
                list.Add(comp);
            }

            foreach (var kvp in groupedByGameObject) {
                var go = kvp.Key;
                var cpns = kvp.Value;
                var path = GetGameObjectPath(go);
                var goIcon = PrefabUtility.GetIconForGameObject(go);

                if (cpns.Count == 1) {
                    var cpn = cpns[0];
                    root.AddChild(new SearchNode {
                        Label = $"{go.name} <{cpn.GetType().Name}> <color=#888888>({path})</color>",
                        Icon = goIcon,
                        Data = cpn
                    });
                } else {
                    var groupItem = new SearchNode { Label = path, Icon = goIcon };
                    var nameIndex = new Dictionary<string, int>();

                    foreach (var cpn in cpns) {
                        var typeName = cpn.GetType().Name;
                        string cpnIdx = string.Empty;
                        if (nameIndex.TryGetValue(typeName, out int idx)) {
                            idx++;
                            nameIndex[typeName] = idx;
                            cpnIdx = $" ({idx})";
                        } else {
                            nameIndex.Add(typeName, 0);
                        }
                        groupItem.AddChild(new SearchNode {
                            Label = $"<color=#888888>{go.name}</color> {typeName}{cpnIdx}",
                            LabelSearch = $"{go.name} <{typeName}{cpnIdx}> <color=#888888>({path})</color>",
                            Icon = EditorGUIUtility.ObjectContent(cpn, cpn.GetType()).image as Texture2D,
                            Data = cpn
                        });
                    }
                    root.AddChild(groupItem);
                }
            }
        }

        private string GetGameObjectPath(GameObject obj) {
            if (obj == null) return string.Empty;
            var pathBuilder = new StringBuilder(obj.name);
            Transform parent = obj.transform.parent;
            while (parent != null) {
                pathBuilder.Insert(0, "/");
                pathBuilder.Insert(0, parent.name);
                parent = parent.parent;
            }
            return pathBuilder.ToString();
        }

        #endregion

        #region Asset Database Scanning

        private async void BuildAssetsSectionAsync(SectionBuildContext ctx) {
            var root = new SearchNode { Label = "Assets" };
            root.AddChild(ConstructNoneNode());
            ctx.ReportProgress(0f);

            try {
                var matchingAssets = new List<AssetMatch>();

                bool isComponentType = typeof(Component).IsAssignableFrom(filterType);
                bool isScriptableObjectType = typeof(ScriptableObject).IsAssignableFrom(filterType);

                if (filterType.IsInterface || (filterType.IsAbstract && !isComponentType && !isScriptableObjectType)) {
                    var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
                    var prefabPaths = new List<string>(prefabGuids.Length);
                    foreach (var guid in prefabGuids)
                        prefabPaths.Add(AssetDatabase.GUIDToAssetPath(guid));

                    await ScanPrefabsAsync(prefabPaths, matchingAssets, ctx);

                    var concreteTypes = GetConcreteTypes();
                    var guidSet = await CollectAssetGuidsAsync(concreteTypes, ctx);
                    await LoadAssetsFromPathsAsync(new List<string>(guidSet), matchingAssets, ctx);
                } else if (isComponentType) {
                    var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
                    var prefabPaths = new List<string>(prefabGuids.Length);
                    foreach (var guid in prefabGuids)
                        prefabPaths.Add(AssetDatabase.GUIDToAssetPath(guid));

                    await ScanPrefabsAsync(prefabPaths, matchingAssets, ctx);
                } else if (isScriptableObjectType) {
                    var concreteTypes = GetConcreteTypes();
                    var guidSet = await CollectAssetGuidsAsync(concreteTypes, ctx);
                    await LoadAssetsFromPathsAsync(new List<string>(guidSet), matchingAssets, ctx);
                } else {
                    var guids = AssetDatabase.FindAssets($"t:{filterType.Name}");
                    var paths = new List<string>(guids.Length);
                    foreach (var g in guids) paths.Add(AssetDatabase.GUIDToAssetPath(g));
                    await LoadAssetsFromPathsAsync(paths, matchingAssets, ctx);
                }

                if (this != null) BuildAssetTree(root, matchingAssets, ctx);
            } catch (Exception ex) {
                Debug.LogError($"[ObjectSearchWindow] Asset scanning failed: {ex.Message}\n{ex.StackTrace}");
                if (this != null) ctx.Complete(root);
            }
        }

        private async Task<HashSet<string>> CollectAssetGuidsAsync(List<Type> concreteTypes, SectionBuildContext ctx) {
            var pathSet = new HashSet<string>();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < concreteTypes.Count; i++) {
                var ct = concreteTypes[i];
                if (typeof(Component).IsAssignableFrom(ct) || typeof(GameObject).IsAssignableFrom(ct))
                    continue;

                var guids = AssetDatabase.FindAssets($"t:{ct.Name}");
                foreach (var g in guids) {
                    var path = AssetDatabase.GUIDToAssetPath(g);
                    if (!string.IsNullOrEmpty(path)) pathSet.Add(path);
                }

                if (stopwatch.ElapsedMilliseconds > MAX_MS_PER_FRAME) {
                    ctx.ReportProgress((float)(i + 1) / concreteTypes.Count * 0.3f);
                    await Task.Yield();
                    if (this == null) return pathSet;
                    stopwatch.Restart();
                }
            }

            return pathSet;
        }

        private async Task ScanPrefabsAsync(List<string> paths, List<AssetMatch> results, SectionBuildContext ctx) {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var filteredPaths = PreFilterPrefabPaths(paths);

            for (int i = 0; i < filteredPaths.Count; i++) {
                var path = filteredPaths[i];

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab != null) {
                    var components = prefab.GetComponents(filterType);
                    foreach (var comp in components) {
                        if (comp == null) continue;
                        results.Add(new AssetMatch {
                            Component = comp,
                            AssetPath = path,
                            GameObject = comp.gameObject
                        });
                    }
                }

                if (stopwatch.ElapsedMilliseconds > MAX_MS_PER_FRAME) {
                    ctx.ReportProgress((float)(i + 1) / filteredPaths.Count);
                    await Task.Yield();
                    if (this == null) return;
                    stopwatch.Restart();
                }
            }
        }

        private List<string> PreFilterPrefabPaths(List<string> allPaths) {
            if (filterType.IsInterface || filterType.IsAbstract) return allPaths;

            var scriptGuids = AssetDatabase.FindAssets($"t:MonoScript {filterType.Name}");
            if (scriptGuids.Length == 0) return allPaths;

            var scriptPaths = new HashSet<string>();
            foreach (var guid in scriptGuids) {
                var scriptPath = AssetDatabase.GUIDToAssetPath(guid);
                var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                if (monoScript != null && monoScript.GetClass() != null
                    && TypeMatches(monoScript.GetClass())) {
                    scriptPaths.Add(scriptPath);
                }
            }

            if (scriptPaths.Count == 0) return allPaths;

            var filtered = new List<string>();
            foreach (var prefabPath in allPaths) {
                var deps = AssetDatabase.GetDependencies(prefabPath, false);
                foreach (var dep in deps) {
                    if (scriptPaths.Contains(dep)) {
                        filtered.Add(prefabPath);
                        break;
                    }
                }
            }

            return filtered.Count > 0 ? filtered : allPaths;
        }

        private async Task LoadAssetsFromPathsAsync(List<string> paths, List<AssetMatch> results, SectionBuildContext ctx) {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < paths.Count; i++) {
                var path = paths[i];
                if (string.IsNullOrEmpty(path)) continue;

                var mainType = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (mainType == null) continue;

                bool mainTypeMatches = TypeMatches(mainType);

                if (mainTypeMatches) {
                    var asset = AssetDatabase.LoadMainAssetAtPath(path);
                    if (asset != null)
                        results.Add(new AssetMatch { Asset = asset, AssetPath = path });
                }

                if (AssetDatabase.IsValidFolder(path)) continue;

                if (!mainTypeMatches || typeof(Object).IsAssignableFrom(filterType)) {
                    var subs = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
                    foreach (var sub in subs) {
                        if (sub != null && TypeMatches(sub.GetType()))
                            results.Add(new AssetMatch { Asset = sub, AssetPath = path });
                    }
                }

                if (stopwatch.ElapsedMilliseconds > MAX_MS_PER_FRAME) {
                    ctx.ReportProgress(0.3f + (float)(i + 1) / paths.Count * 0.7f);
                    await Task.Yield();
                    if (this == null) return;
                    stopwatch.Restart();
                }
            }
        }

        private void BuildAssetTree(SearchNode root, List<AssetMatch> matches, SectionBuildContext ctx) {
            if (matches.Count == 0) {
                ctx.Complete(root);
                return;
            }

            var groupedByAsset = new Dictionary<string, List<AssetMatch>>();
            foreach (var match in matches) {
                if (!groupedByAsset.TryGetValue(match.AssetPath, out var list)) {
                    list = new List<AssetMatch>();
                    groupedByAsset[match.AssetPath] = list;
                }
                list.Add(match);
            }

            foreach (var assetGroup in groupedByAsset) {
                var assetPath = assetGroup.Key;
                var assetMatches = assetGroup.Value;

                if (assetMatches[0].Component == null) {
                    foreach (var match in assetMatches) {
                        var asset = match.Asset;
                        var assetName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                        root.AddChild(new SearchNode {
                            Label = $"{assetName} <color=#888888>({assetPath})</color>",
                            Icon = AssetPreview.GetMiniThumbnail(asset),
                            Data = asset
                        });
                    }
                    continue;
                }

                var groupedByGO = new Dictionary<GameObject, List<Component>>();
                foreach (var match in assetMatches) {
                    var go = match.GameObject;
                    if (go == null) continue;
                    if (!groupedByGO.TryGetValue(go, out var list)) {
                        list = new List<Component>();
                        groupedByGO[go] = list;
                    }
                    list.Add(match.Component);
                }

                foreach (var goGroup in groupedByGO) {
                    var go = goGroup.Key;
                    var components = goGroup.Value;
                    var goPathInPrefab = GetGameObjectPathInAsset(go, assetPath);
                    var fullPath = string.IsNullOrEmpty(goPathInPrefab) ? assetPath : $"{assetPath}/{goPathInPrefab}";
                    var icon = PrefabUtility.GetIconForGameObject(go);

                    if (components.Count == 1) {
                        var comp = components[0];
                        root.AddChild(new SearchNode {
                            Label = $"{go.name} <{comp.GetType().Name}> <color=#888888>({fullPath})</color>",
                            Icon = icon,
                            Data = comp
                        });
                    } else {
                        var groupItem = new SearchNode { Label = fullPath, Icon = icon };
                        var nameIndex = new Dictionary<string, int>();
                        foreach (var comp in components) {
                            var typeName = comp.GetType().Name;
                            string compIdx = string.Empty;
                            if (nameIndex.TryGetValue(typeName, out int idx)) {
                                idx++;
                                nameIndex[typeName] = idx;
                                compIdx = $" ({idx})";
                            } else {
                                nameIndex.Add(typeName, 0);
                            }
                            groupItem.AddChild(new SearchNode {
                                Label = $"<color=#888888>{go.name}</color> {typeName}{compIdx}",
                                LabelSearch = $"{go.name} <{typeName}{compIdx}> <color=#888888>({fullPath})</color>",
                                Icon = EditorGUIUtility.ObjectContent(comp, comp.GetType()).image as Texture2D,
                                Data = comp
                            });
                        }
                        root.AddChild(groupItem);
                    }
                }
            }

            ctx.Complete(root);
        }

        private string GetGameObjectPathInAsset(GameObject obj, string assetPath) {
            if (obj == null) return string.Empty;
            var assetRoot = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (assetRoot == null || obj == assetRoot) return string.Empty;

            var pathBuilder = new StringBuilder(obj.name);
            Transform parent = obj.transform.parent;
            while (parent != null && parent.gameObject != assetRoot) {
                pathBuilder.Insert(0, "/");
                pathBuilder.Insert(0, parent.name);
                parent = parent.parent;
            }
            return pathBuilder.ToString();
        }

        private class AssetMatch {
            public Component Component;
            public Object Asset;
            public string AssetPath;
            public GameObject GameObject;
        }

        #endregion
    }
}