using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CSharp;
using UnityEditor;
using UnityEditor.PackageManager;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Renames members of a generated type where the project's own scripts name them through the type, as <c>Type.member</c>: the scripts under Assets and in embedded packages, the code a project writes.<br/>
    /// A <c>using static</c> of the type, whose members go unqualified, is not followed.<br/>
    /// For a settings page whose Apply renames an enum's members, so code keeps compiling as the names change.
    /// </summary>
    public static class CodeRename {
        // Puts @ before a C# keyword, as a source generator writes a member named after one.
        private static readonly CSharpCodeProvider csharp = new();

        /// <summary>The scripts that name one of the members to rename, with no change made.</summary>
        public static List<string> Find(string type, IReadOnlyCollection<(string From, string To)> renames) {
            if (renames.Count == 0) return new List<string>();

            var pattern = Pattern(type, renames);
            return Scripts().Where(path => pattern.IsMatch(File.ReadAllText(path))).ToList();
        }

        /// <summary>Renames the members in every script that names them, keeping each file's encoding; returns the scripts changed. Unity imports them at its next refresh.</summary>
        public static List<string> Members(string type, IReadOnlyCollection<(string From, string To)> renames) {
            var changed = new List<string>();
            if (renames.Count == 0) return changed;

            var pattern = Pattern(type, renames);
            var map     = renames.ToDictionary(rename => rename.From, rename => csharp.CreateEscapedIdentifier(rename.To));
            foreach (var path in Scripts()) {
                var bytes   = File.ReadAllBytes(path);
                var bom     = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
                var text    = Encoding.UTF8.GetString(bytes, bom ? 3 : 0, bytes.Length - (bom ? 3 : 0));
                var renamed = pattern.Replace(text, match => match.Groups[1].Value + map[match.Groups[2].Value]);
                if (renamed == text) continue;

                File.WriteAllText(path, renamed, new UTF8Encoding(bom));
                changed.Add(path);
            }

            return changed;
        }

        // The type's name, not part of a longer one, then a dot and one of the members, with or without @.
        private static Regex Pattern(string type, IEnumerable<(string From, string To)> renames) {
            return new($@"(?<![\w@])({Regex.Escape(type)}\s*\.\s*)@?({string.Join("|", renames.Select(rename => Regex.Escape(rename.From)))})\b");
        }

        private static IEnumerable<string> Scripts() {
            var roots = new List<string> { "Assets" };
            roots.AddRange(PackageInfo.GetAllRegisteredPackages().Where(package => package.source is PackageSource.Embedded).Select(package => package.resolvedPath));
            return roots.Where(Directory.Exists).SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories));
        }
    }
}