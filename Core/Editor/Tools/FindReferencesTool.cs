using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    public static class FindReferencesTool {
        [MenuItem("CONTEXT/Component/Find References In Scene BETTER")]
        public static void FindReferences(MenuCommand command) {
            FindAndSelectReferences(command.context as Component);
        }

        [MenuItem("GameObject/Find References In Scene BETTER")]
        public static void FindReferencesGameObject(MenuCommand command) {
            FindAndSelectReferences(Selection.activeGameObject);
        }

        private static void FindAndSelectReferences(Object target) {
            if (target == null) return;

            var refHolders = target.TryGetRootPrefab(out var rootPrefab)
                ? rootPrefab.GetComponentsInChildren<Component>(includeInactive: true)
                : Object.FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            var trueRefHolders = new Dictionary<Component, List<string>>();

            foreach (var holder in refHolders) {
                using var so   = new SerializedObject(holder);
                var       prop = so.GetIterator();

                while (prop.Next(true)) {
                    if (prop.name == "m_GameObject") continue;
                    if (prop.propertyType is not SerializedPropertyType.ObjectReference) continue;
                    if (prop.objectReferenceValue != target) continue;

                    if (!trueRefHolders.ContainsKey(holder)) trueRefHolders[holder] = new(1);
                    trueRefHolders[holder].Add(prop.propertyPath);
                }
            }

            if (trueRefHolders.Count == 0) {
                Debug.Log($"<color=grey><b>[Find Reference]</b></color> No references found for <b>{target.name}</b>.");
                return;
            }

            // Log each component
            var refCount = 0;
            foreach (var (holder, paths) in trueRefHolders) {
                refCount += paths.Count;

                var sb = new StringBuilder();
                sb.AppendLine($"<color=cyan><b>[Find Ref]</b></color> Found <b>{paths.Count}</b> ref(s) in Component: <color=orange>{holder.GetType().Name}</color> ({holder.GetPath(withSceneName: false)})");

                foreach (var path in paths) sb.AppendLine($"\t↳{path}");

                Debug.Log(sb.ToString(), holder.gameObject);
            }

            // Select and ping
            var goArray = trueRefHolders.Keys.Select(i => (Object)i.gameObject).ToArray();
            Selection.objects = goArray;
            goArray[^1].PingObject();

            // Log Summary
            Debug.Log($"<color=green><b>[Summary]</b></color> Found a total of <b>{refCount}</b> reference(s) across <b>{goArray.Length}</b> GameObject(s) pointing to <b>{target.name}</b>.");
        }
    }
}