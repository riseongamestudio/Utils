using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RiseOn.Utils.Editor {
    public class InlineEditorImitator : IDisposable {
        private static readonly PropertyInfo MaterialForceVisibleProperty = typeof(MaterialEditor).GetProperty(
            "forceVisible", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
        
        private static readonly Type AnimationClipEditorType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.AnimationClipEditor");
        private static readonly Stack<LayoutSettings> LayoutSettingsStack = new Stack<LayoutSettings>();
        private static readonly GUIContent hiddenLabel = new GUIContent(" ");
        
        private readonly bool isGameObjectType;
        private readonly Func<object, Object> extractUnderlyingObject;

        private UnityEditor.Editor cachedEditor;
        private UnityEditor.Editor previewEditor;
        private Object currentEditorTarget;
        
        private Vector2 scrollPos;
        private bool targetIsOpenForEdit;
        private bool alwaysVisible;

        public InlineEditorImitator(bool isGameObjectType, Func<object, Object> extractUnderlyingObject) {
            this.isGameObjectType = isGameObjectType;
            this.extractUnderlyingObject = extractUnderlyingObject;
        }

        public void DrawLayout(InspectorProperty property, IPropertyValueEntry valueEntry, InlineEditorAttribute attr, GUIContent label, Action<Rect, GUIContent> drawBaseFieldAction) {
            var obj = extractUnderlyingObject(valueEntry.WeakValues[0]);
            switch (attr.ObjectFieldMode) {
                case InlineEditorObjectFieldModes.Boxed:
                    alwaysVisible = false;
                    SirenixEditorGUI.BeginToolbarBox();
                    SirenixEditorGUI.BeginToolbarBoxHeader();
                    var boxedHeaderRect = EditorGUILayout.GetControlRect();
                    if (obj != null) {
                        drawBaseFieldAction(boxedHeaderRect, hiddenLabel);
                        property.State.Expanded = SirenixEditorGUI.Foldout(boxedHeaderRect, property.State.Expanded, label);
                    } else {
                        drawBaseFieldAction(boxedHeaderRect, label);
                    }
                    SirenixEditorGUI.EndToolbarBoxHeader();

                    GUIHelper.PushHierarchyMode(false);
                    DrawEditor(property, valueEntry, attr, obj);
                    GUIHelper.PopHierarchyMode();
                    
                    SirenixEditorGUI.EndToolbarBox();
                    break;

                case InlineEditorObjectFieldModes.Foldout:
                    alwaysVisible = false;
                    var foldoutHeaderRect = EditorGUILayout.GetControlRect();
                    if (obj != null) {
                        drawBaseFieldAction(foldoutHeaderRect, hiddenLabel);
                        property.State.Expanded = SirenixEditorGUI.Foldout(foldoutHeaderRect, property.State.Expanded, label);
                    } else {
                        drawBaseFieldAction(foldoutHeaderRect, label);
                    }

                    EditorGUI.indentLevel++;
                    DrawEditor(property, valueEntry, attr, obj);
                    EditorGUI.indentLevel--;
                    break;

                case InlineEditorObjectFieldModes.Hidden:
                    alwaysVisible = true;
                    if (obj == null) drawBaseFieldAction(EditorGUILayout.GetControlRect(), label);
                    DrawEditor(property, valueEntry, attr, obj);
                    break;

                case InlineEditorObjectFieldModes.CompletelyHidden:
                    alwaysVisible = true;
                    DrawEditor(property, valueEntry, attr, obj);
                    break;
                
                default: 
                    Debug.LogError($"Unknown {nameof(InlineEditorObjectFieldModes)}: {attr.ObjectFieldMode.ToString()}");
                    break;
            }
        }

        private void DrawEditor(InspectorProperty property, IPropertyValueEntry valueEntry, InlineEditorAttribute attr, Object targetObject) {
            if (valueEntry.ValueState == PropertyValueState.ReferencePathConflict) {
                SirenixEditorGUI.InfoMessageBox("Reference path conflict detected.");
                return;
            }

            if (alwaysVisible || SirenixEditorGUI.BeginFadeGroup(this, property.State.Expanded)) {
                UpdateEditors(valueEntry, targetObject, attr);

                if (cachedEditor == null) {
                    if (!alwaysVisible) SirenixEditorGUI.EndFadeGroup();
                    return;
                }

                if (attr.MaxHeight > 0f) {
                    scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(attr.MaxHeight));
                }

                bool showMixedValue = EditorGUI.showMixedValue;
                EditorGUI.showMixedValue = false;
                
                EditorGUI.BeginChangeCheck();
                DoTheDrawing(attr);

                if (EditorGUI.EndChangeCheck()) {
                    property.RecordForUndo();
                    valueEntry.ApplyChanges();
                    
                    GUI.changed = true;
                    property.Tree.DelayActionUntilRepaint(() => {
                        property.RefreshSetup();
                        property.Tree.UpdateTree();
                    });
                }

                EditorGUI.showMixedValue = showMixedValue;
                if (attr.MaxHeight > 0f) EditorGUILayout.EndScrollView();

            } else if (cachedEditor != null) {
                DestroyEditors();
            }

            if (!alwaysVisible) SirenixEditorGUI.EndFadeGroup();
        }

        private void DoTheDrawing(InlineEditorAttribute attr) {
            if (isGameObjectType && !attr.DrawPreview) {
                SirenixEditorGUI.MessageBox("Odin does not currently have a full GameObject inspector window substitute implemented. Choose an InlineEditorMode that includes a preview.");
                DrawGameObjectFallbackButtons();
                return;
            }

            if (cachedEditor == null) return;

            SaveLayoutSettings();

            try {
                if (!targetIsOpenForEdit) GUIHelper.PushGUIEnabled(false);

                bool drawHeader = attr.DrawHeader;
                bool drawGUI = !isGameObjectType && attr.DrawGUI;
                bool drawPreview = attr.DrawPreview || (isGameObjectType && attr.DrawGUI);
                
                PreviewAlignment alignment = attr.PreviewAlignment;
                bool isHorizontal = drawPreview && (alignment == PreviewAlignment.Left || alignment == PreviewAlignment.Right);
                bool isVertical = drawPreview && (alignment == PreviewAlignment.Top || alignment == PreviewAlignment.Bottom);

                if (!drawGUI && isHorizontal) {
                    isHorizontal = false;
                    isVertical = true;
                    alignment = alignment == PreviewAlignment.Left ? PreviewAlignment.Top : PreviewAlignment.Bottom;
                }

                if (isHorizontal) {
                    GUILayout.BeginHorizontal();
                    if (alignment == PreviewAlignment.Left) {
                        GUILayout.BeginVertical();
                        DrawPreview(attr, alignment);
                        GUILayout.EndVertical();
                    }
                    GUILayout.BeginVertical(); 
                } 
                else if (isVertical && alignment == PreviewAlignment.Top) {
                    DrawPreview(attr, alignment);
                }

                DrawHeaderLogic(drawHeader);
                if (drawGUI) DrawGUILogic(drawHeader);

                if (isHorizontal) {
                    GUILayout.EndVertical(); 
                    if (alignment == PreviewAlignment.Right) {
                        GUILayout.BeginVertical();
                        DrawPreview(attr, alignment);
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();
                } 
                else if (isVertical && alignment == PreviewAlignment.Bottom) {
                    DrawPreview(attr, alignment);
                }
            } 
            catch (Exception ex) when (ex.IsExitGUIException()) { throw ex.AsExitGUIException(); }
            catch (Exception ex) { Debug.LogException(ex); }
            finally {
                if (!targetIsOpenForEdit) GUIHelper.PopGUIEnabled();
                RestoreLayout();
            }
        }

        private void DrawGameObjectFallbackButtons() {
            GUILayout.BeginHorizontal();
            GUIHelper.PushGUIEnabled(currentEditorTarget != null);
            
            if (GUILayout.Button(currentEditorTarget != null ? $"Open Inspector window for {currentEditorTarget.name}" : "Open Inspector window (null)")) {
                GUIHelper.OpenInspectorWindow(currentEditorTarget);
                GUIHelper.ExitGUI(true);
            }
            if (GUILayout.Button(currentEditorTarget != null ? $"Select {currentEditorTarget.name}" : "Select GO (null)")) {
                Selection.activeObject = currentEditorTarget;
                GUIHelper.ExitGUI(true);
            }
            
            GUIHelper.PopGUIEnabled();
            GUILayout.EndHorizontal();
        }

        private void DrawHeaderLogic(bool drawHeader) {
            if (drawHeader) {
                var rawType = Event.current.rawType;
                EditorGUILayout.BeginFadeGroup(0.9999f);
                Event.current.type = rawType;
                GUILayout.Space(0f);
                cachedEditor.DrawHeader();
                GUILayout.Space(1f);
                EditorGUILayout.EndFadeGroup();
            } else {
                GUIHelper.BeginDrawToNothing();
                cachedEditor.DrawHeader();
                GUIHelper.EndDrawToNothing();
            }
        }

        private void DrawGUILogic(bool drawHeader) {
            var config = GlobalConfig<GeneralDrawerConfig>.Instance;
            bool prevShowScript = config.ShowMonoScriptInEditor;
            try {
                config.ShowMonoScriptInEditor = false;
                EditorGUILayout.BeginVertical();
                
                bool inspectorExpanded = InternalEditorUtility.GetIsInspectorExpanded(cachedEditor.target);
                if (!drawHeader) InternalEditorUtility.SetIsInspectorExpanded(cachedEditor.target, true);
                
                cachedEditor.OnInspectorGUI();
                
                if (!drawHeader) InternalEditorUtility.SetIsInspectorExpanded(cachedEditor.target, inspectorExpanded);
                EditorGUILayout.EndVertical();
            } finally {
                config.ShowMonoScriptInEditor = prevShowScript;
            }
        }

        private void UpdateEditors(IPropertyValueEntry valueEntry, Object targetObject, InlineEditorAttribute attr) {
            targetIsOpenForEdit = true;

            if (targetObject == null) {
                if (cachedEditor != null) DestroyEditors();
                return;
            }

            if (HasConflict(valueEntry)) {
                SirenixEditorGUI.InfoMessageBox("Cannot perform multi-editing on objects of different types.");
                return;
            }

            if (currentEditorTarget != targetObject || cachedEditor == null) {
                DestroyEditors();
                
                var targets = valueEntry.WeakValues
                    .Cast<object>()
                    .Select(extractUnderlyingObject)
                    .Where(x => x != null)
                    .ToArray();

                if (targets.Length == 0) return;

                currentEditorTarget = targetObject;
                cachedEditor = UnityEditor.Editor.CreateEditor(targets);
                
                var comp = targetObject as Component;
                previewEditor = comp != null ? UnityEditor.Editor.CreateEditor(comp.gameObject) : cachedEditor;

                if (cachedEditor is MaterialEditor matEditor && MaterialForceVisibleProperty != null) {
                    MaterialForceVisibleProperty.SetValue(matEditor, true, null);
                }
            }

            if (attr.DisableGUIForVCSLockedAssets && AssetDatabase.Contains(targetObject)) {
                targetIsOpenForEdit = AssetDatabase.IsOpenForEdit(targetObject);
            }
        }

        private bool HasConflict(IPropertyValueEntry valueEntry) {
            if (valueEntry.ValueState != PropertyValueState.ReferenceValueConflict) return false;

            var underlyingObjects = valueEntry.WeakValues
                .Cast<object>()
                .Select(extractUnderlyingObject)
                .Where(x => x != null)
                .ToArray();

            if (underlyingObjects.Length == 0) return false;

            Type expectedType = underlyingObjects[0].GetType();
            return !underlyingObjects.All(v => v.GetType() == expectedType);
        }

        private void DrawPreview(InlineEditorAttribute attr, PreviewAlignment alignment) {
            bool isHorizontal = alignment == PreviewAlignment.Left || alignment == PreviewAlignment.Right;
            
            if (!attr.DrawPreview && !(isGameObjectType && attr.DrawGUI)) return;
            if (!previewEditor.HasPreviewGUI() && !(previewEditor.target is GameObject)) return;
            
            bool isAnimationClip = currentEditorTarget is AnimationClip;
            float size = isHorizontal ? attr.PreviewWidth : attr.PreviewHeight;
            
            if (isAnimationClip) size = isHorizontal ? Mathf.Max(size, 200f) : Mathf.Max(size, 90f);
            else if (size <= 0f) size = 100f;

            GUILayoutOption[] options = isHorizontal 
                ? new[] { GUILayout.Width(size), GUILayout.ExpandHeight(true) } 
                : new[] { GUILayout.Height(size), GUILayout.ExpandWidth(true) };

            Rect previewRect = EditorGUILayout.GetControlRect(false, size, options);
            
            bool enabledState = GUI.enabled;
            GUI.enabled = true;

            if (isAnimationClip && previewEditor.GetType() == AnimationClipEditorType) {
                DrawAnimationClipEditorPreview(previewRect, attr);
            } else {
                previewEditor.DrawPreview(previewRect);
            }

            GUI.enabled = enabledState;
        }

        private void DrawAnimationClipEditorPreview(Rect rect, InlineEditorAttribute attr) {
            if (!attr.DrawGUI) {
                GUIHelper.BeginDrawToNothing();
                previewEditor.OnInspectorGUI();
                GUIHelper.EndDrawToNothing();
            }

            var evt = Event.current;
            if ((evt.type == EventType.ScrollWheel || evt.type == EventType.DragPerform) && !evt.IsMouseOver(rect)) {
                GUI.enabled = false;
            }

            previewEditor.DrawPreview(rect);
        }

        private void DestroyEditors() {
            targetIsOpenForEdit = true;
            if (previewEditor != null && previewEditor != cachedEditor) {
                try { Object.DestroyImmediate(previewEditor); } catch { }
            }
            if (cachedEditor != null) {
                try { Object.DestroyImmediate(cachedEditor); } catch { }
            }
            previewEditor = null;
            cachedEditor = null;
            currentEditorTarget = null;
        }

        public void Dispose() => DestroyEditors();

        private static void SaveLayoutSettings() {
            LayoutSettingsStack.Push(new LayoutSettings {
                Skin = GUI.skin, Color = GUI.color, ContentColor = GUI.contentColor,
                BackgroundColor = GUI.backgroundColor, Enabled = GUI.enabled,
                IndentLevel = EditorGUI.indentLevel, FieldWidth = EditorGUIUtility.fieldWidth,
                LabelWidth = GUIHelper.ActualLabelWidth, HierarchyMode = EditorGUIUtility.hierarchyMode,
                WideMode = EditorGUIUtility.wideMode
            });
        }

        private static void RestoreLayout() {
            if (LayoutSettingsStack.Count == 0) return;
            var layout = LayoutSettingsStack.Pop();
            GUI.skin = layout.Skin; GUI.color = layout.Color; GUI.contentColor = layout.ContentColor;
            GUI.backgroundColor = layout.BackgroundColor; GUI.enabled = layout.Enabled;
            EditorGUI.indentLevel = layout.IndentLevel; EditorGUIUtility.fieldWidth = layout.FieldWidth;
            GUIHelper.BetterLabelWidth = layout.LabelWidth; EditorGUIUtility.hierarchyMode = layout.HierarchyMode;
            EditorGUIUtility.wideMode = layout.WideMode;
        }

        private struct LayoutSettings {
            public GUISkin Skin; public Color Color; public Color ContentColor;
            public Color BackgroundColor; public bool Enabled; public int IndentLevel;
            public float FieldWidth; public float LabelWidth; public bool HierarchyMode;
            public bool WideMode;
        }
    }
}