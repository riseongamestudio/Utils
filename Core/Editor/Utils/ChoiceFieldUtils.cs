using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RiseOn.Utils.Editor.SearchWindow;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// An enum field whose list is the drop-down pick window rather than the system menu or Odin's selector, so every choice opens the same window.<br/>
    /// The pack's drawers give it to every enum field, Odin's and Unity's alike; code that draws one by hand calls it.<br/>
    /// A list of <see cref="SearchFrom"/> choices or more gets the search field; a [Flags] enum gets toggles, the window staying open.
    /// </summary>
    /// <example><code>
    /// ChoiceFieldUtils.Enum(new GUIContent("Region"), settings.FindProperty("region"));
    /// ChoiceFieldUtils.Enum(new GUIContent("Mode"), mode, picked => mode = picked);
    /// </code></example>
    public static class ChoiceFieldUtils {
        /// <summary>The fewest choices that get a search field: a shorter list shows whole and is read at a glance.</summary>
        public const int SearchFrom = 11;

        /// <summary>
        /// The enum property under its label, on a row of its own.<br/>
        /// Drawn by <see cref="ChoiceEnumPropertyDrawer"/>, which knows the field's enum: [Flags] ones get toggles, and objects that differ show a dash.
        /// </summary>
        public static void Enum(GUIContent label, SerializedProperty property) {
            EditorGUILayout.PropertyField(property, label);
        }

        /// <summary>The enum property's button in <paramref name="rect"/>, drawn as <see cref="Enum(GUIContent, SerializedProperty)"/> is.</summary>
        public static void Enum(Rect rect, SerializedProperty property) {
            EditorGUI.PropertyField(rect, property, GUIContent.none);
        }

        /// <summary>A value of an enum under its label, on a row of its own; a pick arrives after the draw, through <paramref name="onPicked"/>.</summary>
        public static void Enum<T>(GUIContent label, T current, Action<T> onPicked, bool? search = null) where T : Enum {
            var rect = EditorGUILayout.GetControlRect(true);
            Enum(EditorGUI.PrefixLabel(rect, label), current, picked => onPicked((T) picked), search);
        }

        /// <summary>A value of any enum, [Flags] ones too, in <paramref name="rect"/>; a pick arrives after the draw, through <paramref name="onPicked"/>.</summary>
        public static void Enum(Rect rect, Enum current, Action<Enum> onPicked, bool? search = null) {
            if (current.GetType().IsDefined(typeof(FlagsAttribute), false)) Flags(rect, current, onPicked, search);
            else Single(rect, current, onPicked, search);
        }

        /// <summary>An Odin property holding an enum, set back through Odin, which records the pick for Undo.</summary>
        public static void Enum(Rect rect, InspectorProperty property, bool? search = null) {
            if (property.ValueEntry?.WeakSmartValue is not Enum current) return;

            Enum(rect, current, picked => property.Tree.DelayActionUntilRepaint(() => property.ValueEntry.WeakSmartValue = picked), search);
        }

        /// <summary>One of a list of named values under its label, on a row of its own, drawn as <see cref="Value{T}(Rect, IReadOnlyList{ValueTuple{string, T}}, T, Action{T}, bool?)"/> is.</summary>
        public static void Value<T>(GUIContent label, IReadOnlyList<(string Name, T Value)> choices, T current, Action<T> onPicked, bool? search = null) {
            var rect = EditorGUILayout.GetControlRect(true);
            Value(EditorGUI.PrefixLabel(rect, label), choices, current, onPicked, search);
        }

        /// <summary>
        /// One of a list of named values in <paramref name="rect"/>, such as the sorting layers by id; a pick arrives after the draw, through <paramref name="onPicked"/>.<br/>
        /// A value the list does not hold shows as it reads.
        /// </summary>
        public static void Value<T>(Rect rect, IReadOnlyList<(string Name, T Value)> choices, T current, Action<T> onPicked, bool? search = null) {
            var shown = choices.FirstOrDefault(choice => EqualityComparer<T>.Default.Equals(choice.Value, current)).Name ?? current?.ToString() ?? "None";
            if (!EditorGUI.DropdownButton(rect, new GUIContent(EditorGUI.showMixedValue ? MixedValue : shown), FocusType.Keyboard, EditorStyles.popup)) return;

            ChoiceSearchWindow.Open(rect, "", choices.Select(choice => new SearchChoice<T>(choice.Name, choice.Value)), (T picked) => {
                onPicked(picked);
                InternalEditorUtility.RepaintAllViews();
            }, selected: current, search: search ?? choices.Count >= SearchFrom);
        }

        /// <summary>A choice's name as Unity's Inspector shows it: its [InspectorName], else its name made readable.</summary>
        public static string NameOf(Enum value) {
            var field = value.GetType().GetField(value.ToString());
            return field?.GetCustomAttribute<InspectorNameAttribute>()?.displayName ?? ObjectNames.NicifyVariableName(value.ToString());
        }

        // What a field shows for objects that hold different values, as Unity's own fields do.
        private const string MixedValue = "\u2014";

        private static void Single(Rect rect, Enum current, Action<Enum> onPicked, bool? search) {
            if (!EditorGUI.DropdownButton(rect, new GUIContent(EditorGUI.showMixedValue ? MixedValue : NameOf(current)), FocusType.Keyboard, EditorStyles.popup)) return;

            var values = System.Enum.GetValues(current.GetType()).Cast<Enum>().ToArray();
            ChoiceSearchWindow.Open(rect, "", values.Select(value => new SearchChoice<Enum>(NameOf(value), value)), (Enum picked) => {
                onPicked(picked);
                InternalEditorUtility.RepaintAllViews();
            }, selected: current, search: search ?? values.Length >= SearchFrom);
        }

        // Toggles: each pick turns a flag on or off and leaves the list open for more. A flag of no bits, such as Nothing, clears them all;
        // one of several bits, such as Everything, is on while they all are.
        private static void Flags(Rect rect, Enum current, Action<Enum> onPicked, bool? search) {
            var type   = current.GetType();
            var values = System.Enum.GetValues(type).Cast<Enum>().ToArray();
            var bits   = Convert.ToInt64(current);
            if (!EditorGUI.DropdownButton(rect, new GUIContent(EditorGUI.showMixedValue ? MixedValue : Describe()), FocusType.Keyboard, EditorStyles.popup)) return;

            ChoiceSearchWindow.OpenToggles(rect, string.Empty, values.Select(value => new SearchChoice<Enum>(NameOf(value), value)), IsOn, value => {
                var flag = Convert.ToInt64(value);
                bits = flag == 0 ? 0 : IsOn(value) ? bits & ~flag : bits | flag;
                onPicked((Enum) System.Enum.ToObject(type, bits));
                InternalEditorUtility.RepaintAllViews();
            }, search: search ?? values.Length >= SearchFrom);

            bool IsOn(Enum value) {
                var flag = Convert.ToInt64(value);
                return flag == 0 ? bits == 0 : (bits & flag) == flag;
            }

            // The flags on, by name, as Unity names them: Nothing, Everything when every one is, else the names joined.
            string Describe() {
                var on = values.Where(value => Convert.ToInt64(value) != 0 && IsOn(value)).ToArray();
                if (on.Length == 0) return "Nothing";

                var everything = values.Aggregate(0L, (all, value) => all | Convert.ToInt64(value));
                if (bits == everything && on.Length > 1) return "Everything";

                return string.Join(", ", on.Select(NameOf));
            }
        }
    }
}