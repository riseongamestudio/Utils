using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Drawers;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RiseOn.Utils.Editor {
    public sealed class EnumLabelAttributeDrawer : OdinAttributeDrawer<EnumLabelAttribute> {
        private const string CLEAR_ENUM_TYPE_UNDO_NAME = "Clear Enum Type";
        private const string CHANGE_ENUM_TYPE_UNDO_NAME = "Change Enum Type";
        private const string CONTEXT_KEY_PREFIX        = "RiseOn.Utils.EnumLabel";
        private const string ELEMENT_LABELS_CONTEXT_KEY = "RiseOn.Utils.EnumLabel.ElementLabels";
        private const string ENUM_TYPE_LABEL           = "Enum Type";
        private const string MIXED_VALUE_LABEL         = "Mixed";
        private const string NONE_LABEL                = "None";
        private const string RESET_ENUM_TYPE_UNDO_NAME = "Reset Enum Type";
        private const string RESET_LABEL               = "Reset";

        private const float CONTEXT_HIGHLIGHT_OFFSET = 4f;
        private const float CONTEXT_HIGHLIGHT_WIDTH  = 3f;

        private readonly List<LocalPersistentContext<string>> enumTypeNameContexts = new();
        private readonly List<EnumLabelUndoState> enumTypeUndoStates = new();
        private Type[] enumTypes = Type.EmptyTypes;

        protected override bool CanDrawAttributeProperty(InspectorProperty property) {
            var type = property.Info.TypeOfValue;
            return IsOneDimensionalArray(type) || IsArrayElement(property);
        }

        protected override void Initialize() {
            if (IsArrayElement(Property)) return;

            InitializeEnumTypes();

            var defaultEnumTypeName = Attribute.DefaultType?.AssemblyQualifiedName ?? string.Empty;

            foreach (var weakTarget in Property.Tree.WeakTargets) {
                if (weakTarget is not Object target) continue;

                var contextKey = GetContextKey(target);
                var context = PersistentContext.GetLocal(contextKey, defaultEnumTypeName);

                enumTypeNameContexts.Add(context);
                enumTypeUndoStates.Add(EnumLabelUndoStateRegistry.GetOrCreate(
                    contextKey
                  , target
                  , context.Value));
            }

            if (enumTypeNameContexts.Count is 0) {
                var context = this.GetPersistentValue(CONTEXT_KEY_PREFIX, defaultEnumTypeName);
                var undoStateKey = GetFallbackUndoStateKey();

                enumTypeNameContexts.Add(context);
                enumTypeUndoStates.Add(EnumLabelUndoStateRegistry.GetOrCreateFallback(
                    undoStateKey
                  , context.Value));
            }
        }

        protected override void DrawPropertyLayout(GUIContent label) {
            if (IsArrayElement(Property)) {
                DrawArrayElement();
                return;
            }

            var enumType = GetSelectedEnumType(out var hasMixedValue);

            DrawEnumTypeField(enumType, hasMixedValue);

            enumType = GetSelectedEnumType(out hasMixedValue);
            UpdateElementLabels(enumType, hasMixedValue);

            DrawValidation(enumType, hasMixedValue);

            GUILayout.BeginVertical();
            CallNextDrawer(label);
            GUILayout.EndVertical();

            var collectionPosition = GUILayoutUtility.GetLastRect();
            PropertyContextMenuDrawer.AddRightClickArea(Property, collectionPosition);
        }

        private void InitializeEnumTypes() {
            var enumAssemblies = new HashSet<Assembly>();

            if (Attribute.DefaultType is not null) {
                enumAssemblies.Add(Attribute.DefaultType.Assembly);
            }

            foreach (var weakTarget in Property.Tree.WeakTargets) {
                if (weakTarget is null) continue;

                enumAssemblies.Add(weakTarget.GetType().Assembly);
            }

            enumTypes = enumAssemblies
                .SelectMany(GetTypesSafely)
                .Where(type => type.IsEnum)
                .OrderBy(type => type.FullName)
                .ToArray();
        }

        private string GetContextKey(Object target) {
            var globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(target);
            var targetKey = globalObjectId.targetObjectId is 0
                ? target.GetInstanceID().ToString()
                : globalObjectId.ToString();

            return $"{CONTEXT_KEY_PREFIX}|{targetKey}|{Property.UnityPropertyPath}";
        }

        private string GetFallbackUndoStateKey() {
            var weakTargetTypeName = Property.Tree.WeakTargets
                .FirstOrDefault(target => target is not null)
                ?.GetType()
                .AssemblyQualifiedName;

            return $"{CONTEXT_KEY_PREFIX}|{weakTargetTypeName}|{Property.UnityPropertyPath}";
        }

        private Type GetSelectedEnumType(out bool hasMixedValue) {
            SyncPersistentContextsFromUndoStates();

            hasMixedValue = false;
            if (enumTypeUndoStates.Count is 0) return null;

            var enumTypeName = enumTypeUndoStates[0].EnumTypeName;
            for (var i = 1; i < enumTypeUndoStates.Count; ++i) {
                if (string.Equals(
                    enumTypeUndoStates[i].EnumTypeName
                  , enumTypeName
                  , StringComparison.Ordinal)) continue;

                hasMixedValue = true;
                return null;
            }

            return ResolveEnumType(enumTypeName);
        }

        private void SyncPersistentContextsFromUndoStates() {
            for (var i = 0; i < enumTypeNameContexts.Count; ++i) {
                var enumTypeName = enumTypeUndoStates[i].EnumTypeName;
                if (string.Equals(
                    enumTypeNameContexts[i].Value
                  , enumTypeName
                  , StringComparison.Ordinal)) continue;

                enumTypeNameContexts[i].Value = enumTypeName;
            }
        }

        private void DrawEnumTypeField(Type enumType, bool hasMixedValue) {
            var previousMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = hasMixedValue;

            var buttonLabel = hasMixedValue
                ? MIXED_VALUE_LABEL
                : enumType?.FullName ?? NONE_LABEL;

            var position = EditorGUILayout.GetControlRect();
            var buttonPosition = EditorGUI.PrefixLabel(
                position
              , new GUIContent(ENUM_TYPE_LABEL));

            DrawEnumTypeContextMenu(position, enumType, hasMixedValue);

            if (GUI.Button(buttonPosition, buttonLabel, EditorStyles.popup)) {
                var selector = CreateTypeSelector(enumType);
                selector.EnableSingleClickToSelect();
                selector.SelectionConfirmed += SetSelectedEnumType;
                selector.ShowInPopup(buttonPosition);
            }

            EditorGUI.showMixedValue = previousMixedValue;
        }

        private void DrawEnumTypeContextMenu(
            Rect position
          , Type enumType
          , bool hasMixedValue) {
            var currentEvent = Event.current;
            var controlId    = GUIUtility.GetControlID(FocusType.Passive);

            if (currentEvent.type is EventType.MouseDown
             && currentEvent.button is 1
             && position.Contains(currentEvent.mousePosition)) {
                GUIUtility.hotControl = controlId;

                currentEvent.Use();
                GUIHelper.RequestRepaint();
                return;
            }

            if (currentEvent.type is EventType.Repaint
             && GUIUtility.hotControl == controlId) {
                DrawContextHighlight(position);
            }

            if (currentEvent.type is not EventType.MouseUp
             || GUIUtility.hotControl != controlId) return;

            var shouldOpenMenu = currentEvent.button is 1
                              && position.Contains(currentEvent.mousePosition);

            currentEvent.Use();

            if (shouldOpenMenu) {
                ShowEnumTypeContextMenu(enumType, hasMixedValue);
                return;
            }

            GUIUtility.hotControl = 0;
        }

        private static void DrawContextHighlight(Rect position) {
            position.x    -= CONTEXT_HIGHLIGHT_OFFSET;
            position.width = CONTEXT_HIGHLIGHT_WIDTH;

            SirenixEditorGUI.DrawSolidRect(
                position
              , SirenixGUIStyles.HighlightedTextColor
              , true);
        }

        private void ShowEnumTypeContextMenu(Type enumType, bool hasMixedValue) {
            var menu = new GenericMenu();

            var canClear = hasMixedValue || enumType is not null;

            if (canClear) {
                menu.AddItem(
                    new GUIContent(NONE_LABEL)
                  , false
                  , ClearSelectedEnumType);
            } else {
                menu.AddDisabledItem(new GUIContent(NONE_LABEL), true);
            }

            if (Attribute.DefaultType is not null) {
                var isDefaultType = !hasMixedValue
                                 && ReferenceEquals(enumType, Attribute.DefaultType);

                if (isDefaultType) {
                    menu.AddDisabledItem(new GUIContent(RESET_LABEL));
                } else {
                    menu.AddItem(
                        new GUIContent(RESET_LABEL)
                      , false
                      , ResetSelectedEnumType);
                }
            }

            menu.ShowAsContext();
        }

        private void SetSelectedEnumType(IEnumerable<Type> selection) {
            if (selection is null) return;

            var selectedType = selection.FirstOrDefault();
            if (selectedType is null) return;

            var selectedTypeName = selectedType.AssemblyQualifiedName;
            if (SetSelectedEnumTypeName(selectedTypeName, CHANGE_ENUM_TYPE_UNDO_NAME)) {
                GUIHelper.RequestRepaint();
            }
        }

        private void ClearSelectedEnumType() {
            if (SetSelectedEnumTypeName(string.Empty, CLEAR_ENUM_TYPE_UNDO_NAME)) {
                GUIHelper.RequestRepaint();
            }
        }

        private void ResetSelectedEnumType() {
            var defaultEnumTypeName = Attribute.DefaultType?.AssemblyQualifiedName;
            if (defaultEnumTypeName is null) return;

            if (SetSelectedEnumTypeName(defaultEnumTypeName, RESET_ENUM_TYPE_UNDO_NAME)) {
                GUIHelper.RequestRepaint();
            }
        }

        private bool SetSelectedEnumTypeName(string enumTypeName, string undoName) {
            return SetEnumTypeName(
                Enumerable.Range(0, enumTypeNameContexts.Count)
              , enumTypeName
              , undoName);
        }

        private bool SetEnumTypeName(
            IEnumerable<int> indices
          , string enumTypeName
          , string undoName) {
            var changedIndices = indices
                .Where(index =>
                    !string.Equals(
                        enumTypeUndoStates[index].EnumTypeName
                      , enumTypeName
                      , StringComparison.Ordinal)
                    || !string.Equals(
                        enumTypeNameContexts[index].Value
                      , enumTypeName
                      , StringComparison.Ordinal))
                .ToArray();

            if (changedIndices.Length is 0) return false;

            var changedUndoStates = changedIndices
                .Select(index => enumTypeUndoStates[index])
                .Where(state => !string.Equals(
                    state.EnumTypeName
                  , enumTypeName
                  , StringComparison.Ordinal))
                .Distinct()
                .ToArray();

            if (changedUndoStates.Length is not 0) {
                Undo.RecordObjects(changedUndoStates, undoName);
            }

            foreach (var index in changedIndices) {
                var undoState = enumTypeUndoStates[index];

                undoState.EnumTypeName = enumTypeName;
                enumTypeNameContexts[index].Value = enumTypeName;
                EditorUtility.SetDirty(undoState);
            }

            return true;
        }

        private OdinSelector<Type> CreateTypeSelector(Type selectedType) {
            var selector = new TypeSelector(enumTypes, false) {
                FlattenTree    = true
              , HideNamespaces = true
            };

            if (selectedType is not null) selector.SetSelection(selectedType);

            return selector;
        }

        private void UpdateElementLabels(Type enumType, bool hasMixedValue) {
            var enumNames = hasMixedValue || enumType is null
                ? Array.Empty<string>()
                : Enum.GetNames(enumType);

            Property.Context
                .GetGlobal(ELEMENT_LABELS_CONTEXT_KEY, Array.Empty<string>())
                .Value = enumNames;
        }

        private void DrawArrayElement() {
            var enumNames = Property.Parent.Context
                .GetGlobal(ELEMENT_LABELS_CONTEXT_KEY, Array.Empty<string>())
                .Value;

            var label = Property.Index is >= 0 && Property.Index < enumNames.Length
                ? new GUIContent(enumNames[Property.Index])
                : null;

            CallNextDrawer(label);
        }

        private void DrawValidation(Type enumType, bool hasMixedValue) {
            if (hasMixedValue || enumType is null) return;

            var enumNames = Enum.GetNames(enumType);
            if (Property.Children.Count != enumNames.Length) {
                SirenixEditorGUI.WarningMessageBox(
                    $"Array size ({Property.Children.Count}) does not match {enumType.Name} value count ({enumNames.Length}).");
            }

            if (!HasSequentialValues(enumType)) {
                SirenixEditorGUI.WarningMessageBox(
                    $"{enumType.Name} values must start at 0 and be sequential to match array indexes.");
            }
        }

        private static bool HasSequentialValues(Type enumType) {
            var values = Enum.GetValues(enumType);

            for (var i = 0; i < values.Length; ++i) {
                if (Convert.ToDecimal(values.GetValue(i)) != i) return false;
            }

            return true;
        }

        private static bool IsArrayElement(InspectorProperty property) {
            var parent = property.Parent;
            return parent is not null
                && IsOneDimensionalArray(parent.Info.TypeOfValue)
                && parent.GetAttribute<EnumLabelAttribute>() is not null;
        }

        private static bool IsOneDimensionalArray(Type type) {
            return type.IsArray && type.GetArrayRank() is 1;
        }

        private static Type ResolveEnumType(string assemblyQualifiedName) {
            if (string.IsNullOrEmpty(assemblyQualifiedName)) return null;

            var type = Type.GetType(assemblyQualifiedName);
            return type is { IsEnum: true } ? type : null;
        }

        private static IEnumerable<Type> GetTypesSafely(Assembly assembly) {
            try {
                return assembly.GetTypes();
            } catch (ReflectionTypeLoadException exception) {
                return exception.Types.Where(type => type is not null);
            } catch {
                return Type.EmptyTypes;
            }
        }
    }
}
