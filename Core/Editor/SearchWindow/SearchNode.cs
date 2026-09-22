using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using FuzzySearch = UnityEditor.Search.FuzzySearch;

namespace RiseOn.Utils.Editor.SearchWindow {
    public class SearchNode {
        // A term found in the name always outranks one found only elsewhere in the label.
        private const long NameBonus = 1_000_000;

        // The fuzzy score is the same for "Goblin" and "GoblinArcher"; a whole word equal to the term comes first.
        private const long WholeWordBonus = 1_000;

        private List<SearchNode> children;
        private Texture2D        icon;
        private string           searchKey;
        private string           searchName;
        private string           plainLabel;

        /// <summary>Row text. Rich text is allowed.</summary>
        public string Label;

        /// <summary>Row text while searching, when it should say more than <see cref="Label"/>. Rich text is allowed.</summary>
        public string LabelSearch;

        /// <summary>
        /// What search ranks first, usually the object's name and type. A match only in the rest of the label (a path, a
        /// namespace) ranks below it. Plain text; defaults to the label without rich text.
        /// </summary>
        public string SearchName;

        public object Data;

        /// <summary>
        /// Loads <see cref="Icon"/> the first time the row is shown, so a long list does not load every icon up front.
        /// </summary>
        public Func<Texture2D> IconLoader;

        public Texture2D Icon {
            get => icon ??= IconLoader?.Invoke();
            set => icon = value;
        }

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

        /// <summary><see cref="Label"/> without rich text tags, for tooltips.</summary>
        internal string PlainLabel => plainLabel ??= RichText.Strip(Label);

        /// <summary>Length of <see cref="SearchName"/>, the tie-breaker between equal scores: shorter names first.</summary>
        internal int RankLength => searchName?.Length ?? 0;

        /// <summary>
        /// Scores the node with Unity Search's fuzzy matcher: the letters of each term in order, not necessarily
        /// together, with bonuses for consecutive letters, word starts and capitals, so "pb" finds PickableBehaviour.
        /// Every term must match. Folders never match, so a search lists only what can be picked.
        /// </summary>
        internal bool TryScore(string[] terms, out long score) {
            score = 0;
            if (HasChildren) return false;

            searchKey ??= RichText.Strip(Label) + "\n" + RichText.Strip(LabelSearch);
            if (searchKey.Length <= 1) return false;

            searchName ??= string.IsNullOrEmpty(SearchName) ? PlainLabel : SearchName;
            foreach (var term in terms) {
                long termScore = 0;
                if (FuzzySearch.FuzzyMatch(term, searchName, ref termScore, null)) {
                    termScore += NameBonus;
                    if (ContainsWord(searchName, term)) termScore += WholeWordBonus;
                } else if (!FuzzySearch.FuzzyMatch(term, searchKey, ref termScore, null)) {
                    return false;
                }

                score += termScore;
            }

            return true;
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
        /// Drops the formatting tags a label may use and keeps everything else, so <c>&lt;Transform&gt;</c> in a label
        /// stays searchable.
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
