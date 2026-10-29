using System;
using UnityEngine;

namespace RiseOn.Utils.Editor.SearchWindow {
    /// <summary>
    /// One choice of a <see cref="ChoiceSearchWindow"/>: where it is listed, what picking it gives back, and why it cannot be picked when it cannot.
    /// </summary>
    public readonly struct SearchChoice<T> {
        /// <summary>The folders and the row's name, split by <c>/</c>, such as <c>Transform/localScale</c>.</summary>
        public readonly string Path;

        /// <summary>What picking it gives back; null makes it the None row.</summary>
        public readonly T Value;

        /// <summary>Whether it is the None row, made with <see cref="None"/>, which gives back the default value: a value type has no null to say so.</summary>
        public readonly bool IsNone;

        /// <summary>Why it cannot be picked, or null when it can; it is listed grayed out with the reason beside it.</summary>
        public readonly string DisabledReason;

        /// <summary>Loads the row's icon the first time the row shows; null for none.</summary>
        public readonly Func<Texture2D> Icon;

        /// <summary>
        /// The row's text while searching, when the name alone does not say which one it is, such as the object a component is on;<br/>
        /// null shows the name. Rich text is allowed.
        /// </summary>
        public readonly string LabelSearch;

        /// <summary>
        /// Loads the icon of the folder the choice sits in, such as a GameObject's for the folder of its types;<br/>
        /// the first choice of a folder that gives one sets it, and a folder none gives one to shows a folder.
        /// </summary>
        public readonly Func<Texture2D> FolderIcon;

        public SearchChoice(string path, T value, string disabledReason = null, Func<Texture2D> icon = null, string labelSearch = null, Func<Texture2D> folderIcon = null) {
            Path           = path;
            Value          = value;
            DisabledReason = disabledReason;
            Icon           = icon;
            LabelSearch    = labelSearch;
            FolderIcon     = folderIcon;
            IsNone         = false;
        }

        /// <summary>
        /// The None row, for any <typeparamref name="T"/>: picking it gives back the default value.<br/>
        /// Named Null for a type that can be null, Default for one that cannot, unless <paramref name="path"/> names it.
        /// </summary>
        public static SearchChoice<T> None(string path = null) {
            var canBeNull = !typeof(T).IsValueType || Nullable.GetUnderlyingType(typeof(T)) != null;
            return new SearchChoice<T>(path ?? (canBeNull ? "Null" : "Default"));
        }

        private SearchChoice(string path) : this(path, default) {
            IsNone = true;
        }
    }
}