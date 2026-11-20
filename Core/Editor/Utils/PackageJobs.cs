using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditorInternal;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// The package changes settings pages ask for, one at a time, as Unity's Package Manager window takes one: from the press until the Package Manager is done, the section that asked shows what runs under a spinner and every install button waits, since the Package Manager resolves every package again before anything compiles.<br/>
    /// A domain reload, which a change ends with, clears it.
    /// </summary>
    public static class PackageJobs {
        private static string owner;

        /// <summary>What runs, such as "Installing LevelPlay"; null while nothing does.</summary>
        public static string Running { get; private set; }

        public static bool IsRunning => Running != null;

        // Packages asked to go: their classes stay loaded until the domain reloads, which clears this.
        private static readonly HashSet<string> removing = new();

        /// <summary>Whether the package was asked to go, so a page does not take its classes back in the meantime.</summary>
        public static bool Removes(string package) => removing.Contains(package);

        /// <summary>
        /// Adds and removes packages in one request, as Unity's Package Manager window does; <paramref name="done"/> gets whether it worked.<br/>
        /// <paramref name="owner"/> names the section that shows it; a failure goes to the Console as "<paramref name="what"/> failed".
        /// </summary>
        public static void Change(string owner, string what, string[] add, string[] remove, Action<bool> done = null) {
            var request = Client.AddAndRemove(add, remove);
            if (remove != null) removing.UnionWith(remove);
            Begin(owner, what);
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
        /// For an SDK's own way of changing packages, such as AppLovin's, which edits the manifest and resolves: <paramref name="owner"/> shows <paramref name="what"/> until <see cref="End"/>, or until the Package Manager registers the packages again.
        /// </summary>
        public static void Begin(string owner, string what) {
            PackageJobs.owner = owner;
            Running           = what;
            InternalEditorUtility.RepaintAllViews();
        }

        public static void End() {
            owner   = null;
            Running = null;
            InternalEditorUtility.RepaintAllViews();
        }

        /// <summary>A row of the spinner and what runs, as a list's own content, in the section <paramref name="owner"/> names only.</summary>
        public static void DrawRunning(string owner) {
            if (Running == null || owner != PackageJobs.owner) return;

            GUILayout.Space(Section.RowPadding);
            var line = EditorGUILayout.GetControlRect();
            line.xMin += Section.Inset;
            line.xMax -= Section.Inset;
            EditorGUIUtility.SetIconSize(new Vector2(Section.IconSize, Section.IconSize));
            GUI.Label(line, SpinnerUtils.Content(Running));
            EditorGUIUtility.SetIconSize(Vector2.zero);
            GUILayout.Space(Section.RowPadding);
        }

        [InitializeOnLoadMethod]
        private static void Watch() {
            Events.registeredPackages += _ => {
                if (IsRunning) End();
            };
        }
    }
}