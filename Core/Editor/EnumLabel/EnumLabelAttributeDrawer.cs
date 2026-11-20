using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RiseOn.Utils.Editor.SearchWindow;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Drawers;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RiseOn.Utils.Editor {
    public sealed class EnumLabelAttributeDrawer : OdinAttributeDrawer<EnumLabelAttribute> {
        private const string ClearEnumTypeUndoName   = "Clear Enum Type";
        private const string ChangeEnumTypeUndoName  = "Change Enum Type";
        private const string ContextKeyPrefix        = "RiseOn.Utils.EnumLabel";
        private const string ElementLabelsContextKey = "RiseOn.Utils.EnumLabel.ElementLabels";
        private const string EnumTypeLabel           = "Enum Type";
        private const string MixedValueLabel         = "Mixed";
        private const string NoneLabel               = "None";
        private const string ResetEnumTypeUndoName   = "Reset Enum Type";
        private const string ResetLabel              = "Reset";

        private const float ContextHighlightOffset = 4f;
        private const float ContextHighlightWidth  = 3f;

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
                var context = this.GetPersistentValue(ContextKeyPrefix, defaultEnumTypeName);
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

            if (Attribute.DefaultType != null) {
                enumAssemblies.Add(Attribute.DefaultType.Assembly);
            }

            foreach (var weakTarget in Property.Tree.WeakTargets) {
                if (weakTarget == null) continue;

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

            return $"{ContextKeyPrefix}|{targetKey}|{Property.UnityPropertyPath}";
        }

        private string GetFallbackUndoStateKey() {
            var weakTargetTypeName = Property.Tree.WeakTargets
                .FirstOrDefault(target => target != null)
                ?.GetType()
                .AssemblyQualifiedName;

            return $"{ContextKeyPrefix}|{weakTargetTypeName}|{Property.UnityPropertyPath}";
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
                ? MixedValueLabel
                : enumType?.FullName ?? NoneLabel;

            var position = EditorGUILayout.GetControlRect();
            var buttonPosition = EditorGUI.PrefixLabel(
                position
              , new GUIContent(EnumTypeLabel));

            DrawEnumTypeContextMenu(position, enumType, hasMixedValue);

            if (GUI.Button(buttonPosition, buttonLabel, EditorStyles.popup)) {
                ChoiceSearchWindow.Open(buttonPosition, "", enumTypes.Select(type => new SearchChoice<Type>(type.FullName, type)), (Type picked) => SetSelectedEnumType(picked), selected: enumType);
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
            position.x    -= ContextHighlightOffset;
            position.width = ContextHighlightWidth;

            SirenixEditorGUI.DrawSolidRect(
                position
              , SirenixGUIStyles.HighlightedTextColor
              , true);
        }

        private void ShowEnumTypeContextMenu(Type enumType, bool hasMixedValue) {
            var menu = new GenericMenu();

            var canClear = hasMixedValue || enumType != null;

            if (canClear) {
                menu.AddItem(
                    new GUIContent(NoneLabel)
                  , false
                  , ClearSelectedEnumType);
            } else {
                menu.AddDisabledItem(new GUIContent(NoneLabel), true);
            }

            if (Attribute.DefaultType != null) {
                var isDefaultType = !hasMixedValue
                                 && ReferenceEquals(enumType, Attribute.DefaultType);

                if (isDefaultType) {
                    menu.AddDisabledItem(new GUIContent(ResetLabel));
                } else {
                    menu.AddItem(
                        new GUIContent(ResetLabel)
                      , false
                      , ResetSelectedEnumType);
                }
            }

            menu.ShowAsContext();
        }

        private void SetSelectedEnumType(Type selectedType) {
            if (selectedType == null) return;

            var selectedTypeName = selectedType.AssemblyQualifiedName;
            if (SetSelectedEnumTypeName(selectedTypeName, ChangeEnumTypeUndoName)) {
                GUIHelper.RequestRepaint();
            }
        }

        private void ClearSelectedEnumType() {
            if (SetSelectedEnumTypeName(string.Empty, ClearEnumTypeUndoName)) {
                GUIHelper.RequestRepaint();
            }
        }

        private void ResetSelectedEnumType() {
            var defaultEnumTypeName = Attribute.DefaultType?.AssemblyQualifiedName;
            if (defaultEnumTypeName == null) return;

            if (SetSelectedEnumTypeName(defaultEnumTypeName, ResetEnumTypeUndoName)) {
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

        private void UpdateElementLabels(Type enumType, bool hasMixedValue) {
            var enumNames = hasMixedValue || enumType == null
                ? Array.Empty<string>()
                : Enum.GetNames(enumType);

            Property.Context
                .GetGlobal(ElementLabelsContextKey, Array.Empty<string>())
                .Value = enumNames;
        }

        private void DrawArrayElement() {
            var enumNames = Property.Parent.Context
                .GetGlobal(ElementLabelsContextKey, Array.Empty<string>())
                .Value;

            var label = Property.Index is >= 0 && Property.Index < enumNames.Length
                ? new GUIContent(enumNames[Property.Index])
                : null;

            CallNextDrawer(label);
        }

        private void DrawValidation(Type enumType, bool hasMixedValue) {
            if (hasMixedValue || enumType == null) return;

            var enumNames = Enum.GetNames(enumType);
            if (Property.Children.Count != enumNames.Length) {
                HelpBoxUtils.Draw($"Array size ({Property.Children.Count}) does not match {enumType.Name} value count ({enumNames.Length}).", MessageType.Warning);
            }

            if (!HasSequentialValues(enumType)) {
                HelpBoxUtils.Draw($"{enumType.Name} values must start at 0 and be sequential to match array indexes.", MessageType.Warning);
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
            return parent != null
                && IsOneDimensionalArray(parent.Info.TypeOfValue)
                && parent.GetAttribute<EnumLabelAttribute>() != null;
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
                return exception.Types.Where(type => type != null);
            } catch {
                return Type.EmptyTypes;
            }
        }
    }
}