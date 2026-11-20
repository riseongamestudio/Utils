using System;
using System.Collections.Generic;
using System.Linq;
using RiseOn.Utils.Editor.SearchWindow;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// A package a settings page lists, such as an ad network's adapter or one of a pack's adapters: whether it is in the project, at which version, and what installs or removes it; <see cref="Draw"/> draws it as a row's value column.
    /// </summary>
    public sealed class PackageRow {
        private static GUIStyle missingStyle;

        // The widest of a row's buttons, whose width every one takes, so rows' buttons line up whatever they say.
        private static readonly GUIContent widest = new("Uninstall");

        /// <summary>A row button's width: its text's, at least Uninstall's, so a column of rows' buttons lines up.</summary>
        public static float ButtonWidth(GUIContent label) {
            return Mathf.Max(EditorStyles.miniButton.CalcSize(label).x, EditorStyles.miniButton.CalcSize(widest).x);
        }

        // Room in the version picker beside the longest version, for the list's margins and scroll bar, and its least width, for the search field.
        private const float PickerMargin   = 48;
        private const float PickerMinWidth = 120;

        /// <param name="outdated">The source's own verdict on whether <paramref name="latest"/> is newer, when its versions are not plain numbers; null compares the numbers.</param>
        /// <param name="versions">Every version the source offers this project, newest first; null when it offers only the newest.</param>
        /// <param name="installVersion">Installs the version given, one of <paramref name="versions"/>, over any in the project.</param>
        public PackageRow(string name, string installed, string latest, Action install, Action uninstall, string tooltip = null, bool? outdated = null, IReadOnlyList<string> versions = null, Action<string> installVersion = null) {
            this.outdated  = outdated;
            Versions       = versions;
            InstallVersion = installVersion;
            Name      = name;
            Installed = string.IsNullOrEmpty(installed) ? null : installed;
            Latest    = string.IsNullOrEmpty(latest) ? null : latest;
            Install   = install;
            Uninstall = uninstall;
            Tooltip   = tooltip;
        }

        public string Name { get; }

        /// <summary>The version in the project; null when it is not in it.</summary>
        public string Installed { get; }

        /// <summary>The newest version the source offers this project; null when unknown, such as offline.</summary>
        public string Latest { get; }

        /// <summary>Installs <see cref="Latest"/>, or updates to it; null when the source offers no way.</summary>
        public Action Install { get; }

        /// <summary>Removes the package; null when the source offers no way, or the project's manifest does not name it.</summary>
        public Action Uninstall { get; }

        public string Tooltip { get; }

        /// <summary>Every version the source offers this project, newest first; null when it offers only <see cref="Latest"/>.</summary>
        public IReadOnlyList<string> Versions { get; }

        /// <summary>Installs one of <see cref="Versions"/>, older or newer than the one in the project; null when the source offers no choice.</summary>
        public Action<string> InstallVersion { get; }

        private readonly bool? outdated;

        /// <summary>Whether <see cref="Latest"/> is newer than <see cref="Installed"/>: the source's verdict when it gave one, else the numbers compared from the left.</summary>
        public bool IsOutdated => Installed != null && Latest != null && (outdated ?? Compare(Installed, Latest) < 0);

        private static int Compare(string a, string b) {
            var left  = a.Split('.', '-', '+');
            var right = b.Split('.', '-', '+');
            for (var i = 0; i < Math.Max(left.Length, right.Length); i++) {
                long.TryParse(i < left.Length ? left[i] : "0", out var x);
                long.TryParse(i < right.Length ? right[i] : "0", out var y);
                if (x != y) return x.CompareTo(y);
            }

            return 0;
        }

        /// <summary>
        /// A row's value column: the version in the project, an arrow to the newest when that is newer, then Install, Update and Uninstall as they apply, waiting while <see cref="PackageJobs"/> runs a change.
        /// </summary>
        public static void Draw(Rect value, PackageRow row) {
            // Only what can be done: Install while missing, Update while a newer version exists; then Uninstall.
            // Where the source offers a choice, Install asks which version, the newest picked, as the Package Manager window does for a package not in the project.
            var picks   = row.Versions is { Count: > 0 } && row.InstallVersion != null;
            var buttons = new List<(GUIContent Label, Action<Rect> Act)>();
            if (row.Install != null) {
                if (row.Installed == null && picks) buttons.Add((new GUIContent("Install", "Pick the version to install"), button => PickVersion(button, true, row, row.Latest)));
                else if (row.Installed == null) buttons.Add((new GUIContent("Install", $"Install {row.Latest ?? "the newest version"}"), _ => row.Install()));
                else if (row.IsOutdated) buttons.Add((new GUIContent("Update", $"Update to {row.Latest}"), _ => row.Install()));
            }

            if (row.Uninstall != null && row.Installed != null) buttons.Add((new GUIContent("Uninstall", $"Remove {row.Name} from the project"), _ => row.Uninstall()));

            for (var i = buttons.Count - 1; i >= 0; i--) {
                var width  = ButtonWidth(buttons[i].Label);
                var button = new Rect(value.xMax - width, value.y, width, value.height);
                value.xMax = button.x - Section.LabelGap;
                using (new EditorGUI.DisabledScope(PackageJobs.IsRunning)) {
                    if (!GUI.Button(button, buttons[i].Label, EditorStyles.miniButton)) continue;
                }

                buttons[i].Act(button);
                GUIUtility.ExitGUI();
            }

            missingStyle ??= new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleLeft };
            var text  = row.Installed == null ? "Not installed" : row.IsOutdated ? $"{row.Installed} → {row.Latest}" : row.Installed;
            var shown = new GUIContent(text, row.Latest == null ? null : $"Newest: {row.Latest}");
            if (!picks || row.Installed == null) {
                GUI.Label(value, shown, row.Installed == null ? missingStyle : EditorStyles.label);
                return;
            }

            // Once installed, another version, older ones too, picked from a drop-down on the version, as the Package Manager window's version list.
            // The text stays as on a row with no choice, with an arrow after it, so the list does not turn into a column of buttons.
            shown.text    = $"{shown.text}  ▾";
            shown.tooltip = row.Latest == null ? "Pick another version" : $"Newest: {row.Latest}. Pick another version";
            var pick = new Rect(value.x, value.y, Mathf.Min(value.width, EditorStyles.label.CalcSize(shown).x), value.height);
            EditorGUIUtility.AddCursorRect(pick, MouseCursor.Link);
            using (new EditorGUI.DisabledScope(PackageJobs.IsRunning)) {
                if (!EditorGUI.DropdownButton(pick, shown, FocusType.Passive, EditorStyles.label)) return;
            }

            PickVersion(pick, false, row, row.Installed);
        }

        // Every version the source offers, newest first, the one in the project grayed out; picking one installs it.
        // The window takes the width of what opens it, so one under a narrow button is widened, from the side the button keeps, to its versions.
        private static void PickVersion(Rect under, bool fromRight, PackageRow row, string selected) {
            var longest = row.Versions.Max(version => EditorStyles.label.CalcSize(new GUIContent(version)).x);
            var width   = Mathf.Max(under.width, PickerMinWidth, longest + PickerMargin);
            var frame   = new Rect(fromRight ? under.xMax - width : under.x, under.y, width, under.height);
            var choices = row.Versions.Select(version => new SearchChoice<string>(version, version, version == row.Installed ? "In the project" : null));
            ChoiceSearchWindow.Open(frame, "Version", choices, row.InstallVersion, selected: selected);
        }
    }
}