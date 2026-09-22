#if UNITY_EDITOR

using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RiseOn.Utils.Renderers {
    public partial class RendererExt<TRenderer> where TRenderer : Renderer {
        private Editor rdrEditor;
        private Object[] rdrTargets = new Object[1];

        private static readonly List<RendererExt<TRenderer>> validTargets = new(1);
        private static readonly List<RendererExt<TRenderer>> invalidTargets = new(1);
        private static readonly List<RendererExt<TRenderer>> indirectTargets = new(1);

        [OnInspectorGUI, PropertyOrder(-1)]
        private void DrawRendererProps(InspectorProperty property) {
            var targets  = property.Tree.WeakTargets;
            var isSingle = targets.Count == 1;

            validTargets.Clear();
            invalidTargets.Clear();
            indirectTargets.Clear();
            foreach (var target in targets) {
                var castedTarget = (RendererExt<TRenderer>)target;
                if (castedTarget.Rdr != null) {
                    validTargets.Add(castedTarget);
                    if (castedTarget.Rdr.transform.parent != castedTarget.transform) indirectTargets.Add(castedTarget);
                } else invalidTargets.Add(castedTarget);
            }

            if (invalidTargets.Count > 0) {
                SirenixEditorGUI.ErrorMessageBox(isSingle
                    ? $"{nameof(Renderer)} is required!"
                    : $"{nameof(Renderer)} is missing on {invalidTargets.Count} object(s)!");

                if (GUILayout.Button(isSingle
                    ? $"Create {nameof(Renderer)}"
                    : $"Create Missing {nameof(Renderer)}s ({invalidTargets.Count})")) {
                    foreach (var invalidTarget in invalidTargets) invalidTarget.CreateRenderer();
                }
            }

            if (indirectTargets.Count > 0) {
                SirenixEditorGUI.WarningMessageBox(isSingle
                    ? $"{nameof(Renderer)} needs to be direct child of this to work property!"
                    : $"{nameof(Renderer)} is not a direct child on {indirectTargets.Count} object(s)!");

                if (GUILayout.Button(isSingle
                    ? $"Set {nameof(Renderer)}'s parent to this"
                    : $"Fix indirect {nameof(Renderer)}s ({indirectTargets.Count})")) {
                    foreach (var invalidTarget in indirectTargets) invalidTarget.SetParentRdrThenUpdate(resetScale: false);
                }
            }

            if (validTargets.Count > 0) {
                if (validTargets.Count != rdrTargets.Length) {
                    rdrTargets = new Object[validTargets.Count];
                }

                for (int i = 0; i < validTargets.Count; ++i) {
                    rdrTargets[i] = validTargets[i].Rdr;
                }

                Editor.CreateCachedEditor(rdrTargets, null, ref rdrEditor);
                rdrEditor.serializedObject.Update();

                EditorGUILayout.BeginVertical();
                rdrEditor.DrawHeader();
                EditorGUILayout.EndVertical();

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(rdrEditor.serializedObject.FindProperty("m_Enabled"), new GUIContent("Enabled"));
                if (EditorGUI.EndChangeCheck()) {
                    rdrEditor.serializedObject.ApplyModifiedProperties();
                    rdrEditor.serializedObject.Update();
                }

                EditorGUI.BeginChangeCheck();
                rdrEditor.OnInspectorGUI();
                if (EditorGUI.EndChangeCheck()) {
                    rdrEditor.serializedObject.ApplyModifiedProperties();
                    UpdateRdrPosFromPivot();
                }
            }
        }

        private void OnDisable() {
            if (rdrEditor != null) {
                DestroyImmediate(rdrEditor);
                rdrEditor = null;
            }
        }
    }
}

#endif
