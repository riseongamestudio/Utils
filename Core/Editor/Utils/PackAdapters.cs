using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditorInternal;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace RiseOn.Utils.Editor {
    /// <summary>One of a pack's adapters: a package of its own, in a folder beside the pack's core in the pack's repo.</summary>
    public readonly struct PackAdapter {
        /// <summary>What the page shows, such as "MAX".</summary>
        public readonly string Name;

        /// <summary>Its package name, such as "com.riseon.ads.max".</summary>
        public readonly string Package;

        /// <summary>Its folder beside the core's in the pack's repo, such as "Max".</summary>
        public readonly string Folder;

        public PackAdapter(string name, string package, string folder) {
            Name    = name;
            Package = package;
            Folder  = folder;
        }
    }

    /// <summary>
    /// A pack's adapters on its settings page, as the Package Manager window has packages: each one's version in the project, Install at a version picked from the core's registry, Update, Uninstall, and Update All; while the core is a local folder, an adapter installs from the folder beside it.<br/>
    /// The versions are read once a session, from the first <see cref="Load"/>, and again from <see cref="Reload"/>.
    /// </summary>
    public sealed class PackAdapters {
        private readonly Type core;

        // Each adapter's versions in the core's registry, newest first; none while the core is a local folder.
        private readonly Dictionary<string, string[]> versions = new();

        /// <param name="core">A type of the pack's core, which finds the core's package.</param>
        public PackAdapters(Type core, params PackAdapter[] all) {
            this.core = core;
            All       = all;
        }

        public IReadOnlyList<PackAdapter> All { get; }

        /// <summary>Whether the versions are being read.</summary>
        public bool IsLoading { get; private set; }

        /// <summary>Starts reading the adapters' versions, unless read or the core is not from a registry.</summary>
        public void Load() {
            if (IsLoading || versions.Count > 0 || Core() is not { source: PackageSource.Registry }) return;

            IsLoading = true;
            var requests = All.Select(adapter => (adapter.Package, Request: Client.Search(adapter.Package))).ToList();
            EditorApplication.update += Wait;

            void Wait() {
                if (requests.Any(entry => !entry.Request.IsCompleted)) return;

                EditorApplication.update -= Wait;
                IsLoading = false;
                foreach (var (package, request) in requests) {
                    if (request.Status is not StatusCode.Success || request.Result is not { Length: > 0 } found) continue;

                    versions[package] = found[0].versions.compatible.Reverse().ToArray();
                }

                InternalEditorUtility.RepaintAllViews();
            }
        }

        /// <summary>Reads the versions again.</summary>
        public void Reload() {
            if (IsLoading) return;

            versions.Clear();
            Load();
        }

        /// <summary>The adapter <paramref name="package"/> is, or null for a package not the pack's.</summary>
        public PackAdapter? Of(PackageInfo package) => package == null ? null : All.Where(adapter => adapter.Package == package.name).Select(adapter => (PackAdapter?)adapter).FirstOrDefault();

        /// <summary>The adapter's row, in the project or not.</summary>
        public PackageRow Row(PackAdapter adapter) => PackageInfo.FindForPackageName(adapter.Package) is { } package ? Installed(adapter, package) : Missing(adapter);

        /// <summary>
        /// An adapter in the project: its version, the newest, Update, and Uninstall; Uninstall only while the manifest names it, as the Package Manager removes only what the manifest names.
        /// </summary>
        /// <param name="uninstall">What Uninstall does, such as asking first; null removes the package.</param>
        public PackageRow Installed(PackAdapter adapter, PackageInfo package, Action uninstall = null) {
            var offered = versions.TryGetValue(package.name, out var found) ? found : null;
            var latest  = offered?.FirstOrDefault();
            return new PackageRow(adapter.Name, package.version, latest
                                , install: latest == null ? null : () => Install(adapter, latest, "Updating")
                                , uninstall: package.isDirectDependency ? uninstall ?? (() => PackageJobs.Change($"Uninstalling {adapter.Name}", null, new[] { adapter.Package })) : null
                                , tooltip: package.source is PackageSource.Local ? $"From {package.resolvedPath}" : null
                                , versions: offered
                                , installVersion: version => Install(adapter, version, "Installing"));
        }

        /// <summary>An adapter not in the project: Install, at a version picked from the core's registry, or from the folder beside the core's while it is a local folder.</summary>
        public PackageRow Missing(PackAdapter adapter) {
            var root    = Core();
            var local   = root is { source: PackageSource.Local } ? Path.Combine(Path.GetDirectoryName(root.resolvedPath)!, adapter.Folder).Replace('\\', '/') : null;
            var offered = versions.TryGetValue(adapter.Package, out var found) ? found : null;
            Action install = local != null && Directory.Exists(local) ? () => PackageJobs.Change($"Installing {adapter.Name}", new[] { "file:" + local }, null)
                           : local != null ? null
                           : () => Install(adapter, offered?[0], "Installing");
            return new PackageRow(adapter.Name, null, offered?.FirstOrDefault(), install, null
                                , tooltip: install == null ? $"The core is a local folder, and {local} is not there" : null
                                , versions: offered
                                , installVersion: version => Install(adapter, version, "Installing"));
        }

        private PackageInfo Core() {
            return PackageInfo.FindForAssembly(core.Assembly);
        }

        private void Install(PackAdapter adapter, string version, string verb) {
            PackageJobs.Change($"{verb} {adapter.Name}", new[] { version == null ? adapter.Package : $"{adapter.Package}@{version}" }, null);
        }

        /// <summary>At the bar's end, as the Package Manager window's Update All: every adapter behind its newest version in one change, grayed out while none is.</summary>
        public void DrawUpdateAll(Rect value, IEnumerable<(PackAdapter Adapter, PackageRow Row)> shown) {
            var outdated = shown.Where(entry => entry.Row is { IsOutdated: true, Install: not null }).ToList();
            var names    = string.Join(", ", outdated.Select(entry => entry.Adapter.Name));
            var label    = new GUIContent("Update All", outdated.Count == 0 ? "Every adapter in the project is on its newest version" : $"Update {names}");
            var width    = EditorStyles.miniButton.CalcSize(label).x;
            if (!PackageJobs.Button(new Rect(value.xMax - width, value.y, width, value.height), label, $"{core.FullName}/Update All", outdated.Count > 0)) return;

            PackageJobs.Change($"Updating {names}", outdated.Select(entry => $"{entry.Adapter.Package}@{entry.Row.Latest}").ToArray(), null);
            GUIUtility.ExitGUI();
        }
    }
}