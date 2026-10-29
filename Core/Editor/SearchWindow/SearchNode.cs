using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using FuzzySearch = UnityEditor.Search.FuzzySearch;

namespace RiseOn.Utils.Editor.SearchWindow {
    public class SearchNode {
        // A term found in the name always outranks one found only elsewhere in the label.
        private const long NameBonus = 1_000_000;

        // The fuzzy score is the same for "Goblin" and "GoblinArcher"; a whole word equal to the term comes first.
        private const long WholeWordBonus = 1_000;

        private static Texture2D folderIcon;

        private List<SearchNode> children;
        private bool             isFolder;
        private Texture2D        icon;
        private string           searchKey;
        private string           searchName;
        private string           plainLabel;

        /// <summary>Row text. Rich text is allowed.</summary>
        public string Label;

        /// <summary>Row text while searching, when it should say more than <see cref="Label"/>. Rich text is allowed.</summary>
        public string LabelSearch;

        /// <summary>
        /// What search ranks first, usually the object's name and type.<br/>
        /// A match only in the rest of the label (a path, a namespace) ranks below it.<br/>
        /// Plain text; defaults to the label without rich text.
        /// </summary>
        public string SearchName;

        public object Data;

        /// <summary>
        /// Why the row cannot be picked, or null when it can; while set, the row is grayed out with the reason beside it, and picking it does nothing.
        /// </summary>
        public string DisabledReason;

        /// <summary>
        /// Loads <see cref="Icon"/> the first time the row is shown, so a long list does not load every icon up front.
        /// </summary>
        public Func<Texture2D> IconLoader;

        public Texture2D Icon {
            get => icon ??= IconLoader?.Invoke();
            set => icon = value;
        }

        /// <summary>Whether the row has an icon, or one to load, without loading it; a list where no row has one leaves no room for one.</summary>
        public bool HasIcon => icon != null || IconLoader != null;

        public IReadOnlyList<SearchNode> Children => children;

        public bool HasChildren => children != null && children.Count > 0;

        public void AddChild(SearchNode item) {
            children ??= new List<SearchNode>(1);
            children.Add(item);
        }

        public void AddRangeChildren(IEnumerable<SearchNode> items) {
            if (children == null) {
                children = new List<SearchNode>(items);
            } else {
                children.AddRange(items);
            }
        }

        /// <summary>
        /// Adds <paramref name="item"/> inside the folders of <paramref name="folderPath"/>, such as <c>Physics/2D</c>, making the ones missing;<br/>
        /// an empty path adds it here. Folders and items keep the order they are first added in.
        /// </summary>
        /// <returns>The folder it was added in, this node for an empty path.</returns>
        public SearchNode AddInFolder(string folderPath, SearchNode item) {
            var folder = this;
            if (!string.IsNullOrEmpty(folderPath)) {
                foreach (var rawPart in folderPath.Split('/')) {
                    var part = rawPart.Trim();
                    if (part.Length > 0) folder = folder.GetOrAddFolder(part);
                }
            }

            folder.AddChild(item);
            return folder;
        }

        /// <summary>
        /// Puts folders before items and orders each by name, all the way down, as Unity's Add Component lists them.
        /// </summary>
        public void SortFoldersFirst() {
            if (children == null) return;

            children.Sort((a, b) => a.isFolder != b.isFolder
                ? a.isFolder ? -1 : 1
                : string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase));
            foreach (var child in children) child.SortFoldersFirst();
        }

        private SearchNode GetOrAddFolder(string label) {
            if (children != null) {
                foreach (var child in children) {
                    if (child.isFolder && child.Label == label) return child;
                }
            }

            folderIcon ??= EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
            var folder = new SearchNode { Label = label, Icon = folderIcon, isFolder = true };
            AddChild(folder);
            return folder;
        }

        /// <summary><see cref="Label"/> without rich text tags, for tooltips.</summary>
        internal string PlainLabel => plainLabel ??= RichText.Strip(Label);

        /// <summary>The folder holding the node, set when its section is built; null at the top.</summary>
        internal SearchNode Parent;

        /// <summary>The labels of the folders above the node, joined by <c> › </c>, set when its section is built; empty at the top.</summary>
        internal string Path = string.Empty;

        /// <summary>The indices in <see cref="PlainLabel"/> of the letters the last search matched, for highlighting; empty when it matched elsewhere.</summary>
        internal readonly List<int> Matches = new();

        /// <summary>Length of <see cref="SearchName"/>, the tie-breaker between equal scores: shorter names first.</summary>
        internal int RankLength => searchName?.Length ?? 0;

        /// <summary>
        /// Scores the node with Unity Search's fuzzy matcher: the letters of each term in order, not necessarily together, with bonuses for consecutive letters, word starts and capitals, so "pb" finds PickableBehaviour.<br/>
        /// Every term must match, in the name or in the path of folders above it, so "counter canvas" finds CanvasGroup in the folder Counter;<br/>
        /// a match in the name ranks first. A folder matches by its name alone, to be opened from the results.
        /// </summary>
        internal bool TryScore(string[] terms, out long score) {
            score = 0;
            Matches.Clear();

            searchName ??= string.IsNullOrEmpty(SearchName) ? PlainLabel : SearchName;
            searchKey  ??= Path + "\n" + PlainLabel + "\n" + RichText.Strip(LabelSearch);
            if (searchName.Length == 0) return false;

            // Letters are highlighted only while the name searched is the label shown.
            var matches = searchName == PlainLabel ? new List<int>() : null;
            foreach (var term in terms) {
                long termScore = 0;
                matches?.Clear();
                if (FuzzySearch.FuzzyMatch(term, searchName, ref termScore, matches)) {
                    termScore += NameBonus;
                    if (ContainsWord(searchName, term)) termScore += WholeWordBonus;
                    if (matches != null) Matches.AddRange(matches);
                } else if (HasChildren || !FuzzySearch.FuzzyMatch(term, searchKey, ref termScore, null)) {
                    Matches.Clear();
                    return false;
                }

                score += termScore;
            }

            return true;
        }

        /// <summary>
        /// The row's text in the results: <see cref="LabelSearch"/> when given, or else the path of folders above it grayed out,<br/>
        /// then the label with the letters the search matched in bold.
        /// </summary>
        internal string GetSearchLabel() {
            if (!string.IsNullOrEmpty(LabelSearch)) return LabelSearch;

            var label = new StringBuilder();
            if (Path.Length > 0) label.Append("<color=#888888>").Append(RichText.Literal(Path)).Append(" › </color>");
            if (Matches.Count == 0 || PlainLabel != Label) return label.Append(Label).ToString();

            for (var i = 0; i < PlainLabel.Length; i++) {
                var letter = RichText.Literal(PlainLabel[i].ToString());
                if (Matches.Contains(i)) label.Append("<b>").Append(letter).Append("</b>");
                else label.Append(letter);
            }

            return label.ToString();
        }

        /// <summary>Whether a word of <paramref name="text"/>, split at anything but letters and digits, equals the term.</summary>
        private static bool ContainsWord(string text, string term) {
            var start = 0;
            for (var i = 0; i <= text.Length; i++) {
                if (i < text.Length && char.IsLetterOrDigit(text[i])) continue;

                if (i - start == term.Length && string.Compare(text, start, term, 0, term.Length, StringComparison.OrdinalIgnoreCase) == 0) {
                    return true;
                }

                start = i + 1;
            }

            return false;
        }
    }

    internal static class RichText {
        private static readonly string[] tagNames = { "b", "i", "u", "s", "color", "size", "alpha", "mark", "noparse" };

        /// <summary>
        /// Drops the formatting tags a label may use and keeps everything else, so <c>&lt;Transform&gt;</c> in a label stays searchable.
        /// </summary>
        internal static string Strip(string text) {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (text.IndexOf('<') < 0) return text;

            var result = new StringBuilder(text.Length);
            var index  = 0;
            while (index < text.Length) {
                var close = text[index] == '<' ? text.IndexOf('>', index + 1) : -1;
                if (close > index && IsFormattingTag(text, index + 1, close)) {
                    index = close + 1;
                    continue;
                }

                result.Append(text[index]);
                index++;
            }

            return result.ToString();
        }

        /// <summary>Shows <paramref name="text"/> as it is, even when part of it looks like a tag.</summary>
        internal static string Literal(string text) {
            if (string.IsNullOrEmpty(text) || text.IndexOf('<') < 0) return text;

            return "<noparse>" + text + "</noparse>";
        }

        /// <summary>Whether <c>text[start..end)</c> is a tag like <c>b</c>, <c>/b</c> or <c>color=#888888</c>.</summary>
        private static bool IsFormattingTag(string text, int start, int end) {
            if (start < end && text[start] == '/') start++;

            foreach (var name in tagNames) {
                if (end - start < name.Length) continue;
                if (string.CompareOrdinal(text, start, name, 0, name.Length) != 0) continue;

                var next = start + name.Length;
                if (next == end || text[next] == '=') return true;
            }

            return false;
        }
    }
}