using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditorInternal;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// The package changes settings pages ask for, one at a time, as Unity's Package Manager window takes one: from the press until the Package Manager is done, every button that changes packages waits, since the Package Manager resolves every package again before anything compiles, and the one pressed spins in place of its text.<br/>
    /// A domain reload, which a change ends with, clears it.
    /// </summary>
    public static class PackageJobs {
        // The button pressed last, by the key its page gave it, which spins while the change it started runs.
        private static string pressed;

        /// <summary>What runs, such as "Installing LevelPlay"; null while nothing does.</summary>
        public static string Running { get; private set; }

        public static bool IsRunning => Running != null;

        // Packages asked to go: their classes stay loaded until the domain reloads, which clears this.
        private static readonly HashSet<string> removing = new();

        /// <summary>Whether the package was asked to go, so a page does not take its classes back in the meantime.</summary>
        public static bool Removes(string package) => removing.Contains(package);

        /// <summary>
        /// Adds and removes packages in one request, as Unity's Package Manager window does; <paramref name="done"/> gets whether it worked.<br/>
        /// A failure goes to the Console as "<paramref name="what"/> failed".
        /// </summary>
        public static void Change(string what, string[] add, string[] remove, Action<bool> done = null) {
            var request = Client.AddAndRemove(add, remove);
            if (remove != null) removing.UnionWith(remove);
            Begin(what);
            EditorApplication.update += Wait;

            void Wait() {
                if (!request.IsCompleted) return;

                EditorApplication.update -= Wait;
                End();
                var worked = request.Status is StatusCode.Success;
                if (!worked) {
                    if (remove != null) removing.ExceptWith(remove);
                    Debug.LogError($"{what} failed: {request.Error.message}");
                }

                done?.Invoke(worked);
            }
        }

        /// <summary>
        /// For an SDK's own way of changing packages, such as AppLovin's, which edits the manifest and resolves: the button pressed spins, with <paramref name="what"/> on hover, until <see cref="End"/>, or until the Package Manager registers the packages again.
        /// </summary>
        public static void Begin(string what) {
            Running = what;
            InternalEditorUtility.RepaintAllViews();
        }

        public static void End() {
            Running = null;
            pressed = null;
            InternalEditorUtility.RepaintAllViews();
        }

        /// <summary>
        /// A button that starts a package change, true when clicked: off while a change runs, the one that started it spinning meanwhile, with its text beside the spinner where the button has room, and what runs on hover.<br/>
        /// <paramref name="key"/> names the button on its page, such as the package and the button's text, so it spins again when the page draws it next.
        /// </summary>
        public static bool Button(Rect rect, GUIContent label, string key, bool enabled = true, GUIStyle style = null) {
            style ??= EditorStyles.miniButton;
            if (Spins(key)) {
                var content = Spinning(rect, label, style);
                EditorGUIUtility.SetIconSize(new Vector2(Section.IconSize, Section.IconSize));
                using (new EditorGUI.DisabledScope(true)) GUI.Button(rect, content, style);
                EditorGUIUtility.SetIconSize(Vector2.zero);
                // Over the disabled button, which shows no tooltip of its own.
                GUI.Label(rect, new GUIContent("", Running));
                return false;
            }

            using (new EditorGUI.DisabledScope(!enabled || IsRunning)) {
                if (!GUI.Button(rect, label, style)) return false;
            }

            pressed = key;
            return true;
        }

        /// <summary>Marks a control other than <see cref="Button"/> as the one starting a change, such as a version picked from a drop-down, so <see cref="Spins"/> says it spins.</summary>
        public static void Press(string key) {
            pressed = key;
        }

        /// <summary>Whether the control of <paramref name="key"/> started the change that runs.</summary>
        public static bool Spins(string key) => IsRunning && pressed == key;

        /// <summary>The spinner, with <paramref name="label"/>'s text where <paramref name="rect"/> has room for both in <paramref name="style"/> at an icon size of <see cref="Section.IconSize"/>, which the caller sets as it draws it.</summary>
        public static GUIContent Spinning(Rect rect, GUIContent label, GUIStyle style) {
            var both = SpinnerUtils.Content(label.text);
            EditorGUIUtility.SetIconSize(new Vector2(Section.IconSize, Section.IconSize));
            var fits = style.CalcSize(both).x <= rect.width;
            EditorGUIUtility.SetIconSize(Vector2.zero);
            return fits ? both : SpinnerUtils.Content("");
        }

        [InitializeOnLoadMethod]
        private static void Watch() {
            Events.registeredPackages += _ => {
                if (IsRunning) End();
            };
        }
    }
}