using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Something a pack's setup lacks: a box at the top of its settings page, with a button that fixes it and one that goes to the field to fix by hand, and, when <see cref="Blocking"/>, a reason the pack's build check stops the build.<br/>
    /// Or, made by <see cref="Info"/>, a note on how the setup runs, which lacks nothing: an info box, with a button for what the note offers, that a build check passes by.
    /// </summary>
    public sealed class SettingsIssue {
        /// <summary>The build target it concerns, or <see cref="BuildTarget.NoTarget"/> for every target.</summary>
        public readonly BuildTarget Target;

        /// <summary>It stops the build; otherwise it only shows on the settings page.</summary>
        public readonly bool Blocking;

        public readonly string Message;
        public readonly string FixLabel;
        public readonly Action Fix;

        /// <summary>The settings asset holding the field to fix by hand, for the box's Edit button; null for none.</summary>
        public readonly Object FieldAsset;

        /// <summary>That field's serialized path, such as "devKey".</summary>
        public readonly string FieldPath;

        /// <summary>A note, made by <see cref="Info"/>: not a lack, so a build check neither stops nor warns for it.</summary>
        public readonly bool IsInfo;

        /// <param name="fixLabel">With <paramref name="fix"/>, a button that fixes it.</param>
        /// <param name="fieldAsset">With <paramref name="fieldPath"/>, the field the box's Edit button takes the page to; see <see cref="SettingsFocus"/>.</param>
        public SettingsIssue(BuildTarget target, bool blocking, string message, string fixLabel = null, Action fix = null, Object fieldAsset = null, string fieldPath = null) {
            Target     = target;
            Blocking   = blocking;
            Message    = message;
            FixLabel   = fixLabel;
            Fix        = fix;
            FieldAsset = fieldAsset;
            FieldPath  = fieldPath;
        }

        private SettingsIssue(string message, string fixLabel, Action fix) : this(BuildTarget.NoTarget, false, message, fixLabel, fix) {
            IsInfo = true;
        }

        /// <summary>A note on how the setup runs, such as a default standing in for what the project has not made, with a button for what it offers.</summary>
        public static SettingsIssue Info(string message, string fixLabel = null, Action fix = null) => new(message, fixLabel, fix);

        public bool Concerns(BuildTarget target) {
            return Target is BuildTarget.NoTarget || Target == target;
        }

        /// <summary>Its box, the target's name first when it concerns one, its buttons at the end: the fix, then Edit.</summary>
        public void Draw() {
            var platform = Target is BuildTarget.NoTarget ? "" : $"{Target}: ";
            var buttons  = new List<(GUIContent, bool)>();
            if (Fix != null) buttons.Add((new GUIContent(FixLabel), true));
            var edit = FieldAsset && FieldPath != null;
            if (edit) buttons.Add((new GUIContent("Edit", "Goes to the field"), true));

            var picked = HelpBoxUtils.Draw(platform + Message, Blocking ? MessageType.Error : IsInfo ? MessageType.Info : MessageType.Warning, buttons.ToArray());
            if (picked < 0) return;

            if (Fix != null && picked == 0) {
                try {
                    Fix();
                } catch (Exception e) when (e is not ExitGUIException) {
                    Debug.LogException(e);
                }
            } else if (edit) {
                SettingsFocus.Request(FieldAsset, FieldPath);
            }
        }

        /// <summary>Each issue's box, in order.</summary>
        public static void Draw(IEnumerable<SettingsIssue> issues) {
            foreach (var issue in issues) issue.Draw();
        }
    }
}