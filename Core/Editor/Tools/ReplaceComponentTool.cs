using System;
using System.Collections.Generic;
using System.Linq;
using RiseOn.Utils.Editor.SearchWindow;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace RiseOn.Utils.Editor {
    public static class ReplaceComponentTool {
        private static bool isHandlingCommand;
        private static int totalCommandAmount;

        private static readonly List<Component> handlingComponents = new();

        private static readonly HashSet<string> IgnoredProperties = new() {
            "m_Script"
          , "m_ObjectHideFlags"
          , "m_CorrespondingSourceObject"
          , "m_PrefabInstance"
          , "m_PrefabAsset"
        };

        /// <summary>
        /// Caution: References to the old component are only replaced within the currently active scene.
        /// </summary>
        [MenuItem("CONTEXT/Component/Replace Component")]
        public static void ReplaceComponent(MenuCommand command) {
            if (!isHandlingCommand) {
                isHandlingCommand  = true;
                totalCommandAmount = Selection.gameObjects.Length;
            }

            handlingComponents.Add(command.context as Component);

            if (handlingComponents.Count < totalCommandAmount) return;

            var storedHandlingComponents = new List<Component>(handlingComponents);
            ComponentSearchWindow.Open(
                new(EditorWindow.focusedWindow?.position.center ?? new Vector2(Screen.width, Screen.height) / 2, default)
              , newCpnType => ReplaceComponent(storedHandlingComponents, newCpnType)
              , searchText: handlingComponents[0].GetType().Name);

            isHandlingCommand = false;
            handlingComponents.Clear();
        }

        private static void ReplaceComponent(List<Component> components, Type newCpnType) {
            if (components[0].GetType() == newCpnType) {
                Debug.LogWarning($"RiseOn {nameof(ReplaceComponentTool)}: You are replacing {newCpnType.Name} with the same type (Action skipped)!");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Replace Component");

            var refHolders = Object.FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToHashSet();
            foreach (var oldCpn in components) {
                if (!oldCpn.TryGetRootPrefab(out var rootPrefab)) continue;
                refHolders.UnionWith(rootPrefab.GetComponentsInChildren<Component>(includeInactive: true));
            }

            var refLocs     = new List<(Component holder, string path)>();
            var selfRefLocs = new List<string>();
            foreach (var oldCpn in components) {
                // Scan all refs
                refLocs.Clear();
                selfRefLocs.Clear();
                foreach (var holder in refHolders) {
                    using var so   = new SerializedObject(holder);
                    var       prop = so.GetIterator();
                    while (prop.Next(true)) {
                        if (prop.propertyType is not SerializedPropertyType.ObjectReference) continue;
                        if (prop.objectReferenceValue != oldCpn) continue;
                        if (holder == oldCpn) selfRefLocs.Add(prop.propertyPath);
                        else refLocs.Add((holder, prop.propertyPath));
                    }
                }

                // Create temp component
                var       tmpGO  = new GameObject { hideFlags = HideFlags.HideAndDontSave };
                var       tmpCpn = tmpGO.AddComponent(newCpnType);
                using var tmpSO  = new SerializedObject(tmpCpn);
                using var oldSO  = new SerializedObject(oldCpn);
                CopySerializedObject(oldSO, tmpSO);

                // Destroy old component => Then create new component
                var go       = oldCpn.gameObject;
                var oldIndex = Array.IndexOf(go.GetComponents<Component>(), oldCpn);

                Undo.DestroyObjectImmediate(oldCpn);
                var newCpn = Undo.AddComponent(go, newCpnType);
                refHolders.Remove(oldCpn);
                refHolders.Add(newCpn);

                // AddComponent appends at the end, so walk the new one back to the old slot.
                for (var i = go.GetComponents<Component>().Length - 1; i > oldIndex; --i) {
                    if (!UnityEditorInternal.ComponentUtility.MoveComponentUp(newCpn)) break;
                }

                // Copy data from temp component and cleanup
                using var newSO = new SerializedObject(newCpn);
                CopySerializedObject(tmpSO, newSO);
                foreach (var path in selfRefLocs) newSO.FindProperty(path).objectReferenceValue = newCpn;
                newSO.ApplyModifiedProperties();
                Object.DestroyImmediate(tmpGO);

                // Replace refs
                // Tried the best to replace reference in [all visible prefab asset] + [active scene]
                foreach (var (holder, path) in refLocs) {
                    using var so = new SerializedObject(holder);
                    so.FindProperty(path).objectReferenceValue = newCpn;
                    so.ApplyModifiedProperties();
                }

                UndoHelper.MarkDirty(go);
            }

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        }

        private static void CopySerializedObject(SerializedObject source, SerializedObject target) {
            var srcProp = source.GetIterator();

            if (!srcProp.Next(true)) return;

            do {
                if (IgnoredProperties.Contains(srcProp.name)) continue;

                if (target.FindProperty(srcProp.name) is not { } targetProp) continue;

                if (targetProp.propertyType != srcProp.propertyType) continue;

                target.CopyFromSerializedProperty(srcProp);
            } while (srcProp.Next(false));
        }
    }
}