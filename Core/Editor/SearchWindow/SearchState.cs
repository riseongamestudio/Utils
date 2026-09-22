using System.Collections.Generic;
using UnityEngine;

namespace RiseOn.Utils.Editor.SearchWindow {
    internal class LevelState {
        public float ScrollOffset;
        public int   SelectedIndex = -1;
    }

    internal class SectionState {
        public readonly Stack<SearchNode>                  NavigationStack = new();
        public readonly List<SearchNode>                   CurrentItems    = new();
        public readonly List<SearchNode>                   FilteredItems   = new();
        public readonly List<SearchNode>                   AllFlattened    = new();
        public readonly Dictionary<SearchNode, LevelState> LevelStates     = new();

        public float      ScrollOffset;
        public int        SelectedIndex = -1;
        public string     Label;
        public bool       IsBuilding    = true;
        public float      BuildProgress;
        public int        PickableCount;
        public SearchNode Root;

        // The initial value was revealed here; scroll to it the next time this section is shown.
        public bool RevealPending;
    }

    public class SectionBuildContext {
        private readonly SectionState state;
        private readonly SearchWindow window;

        internal SectionBuildContext(SectionState state, SearchWindow window) {
            this.state  = state;
            this.window = window;
        }

        public void ReportProgress(float progress) {
            state.BuildProgress = Mathf.Clamp01(progress);
            if (window != null) window.OnSectionProgress(state);
        }

        public void Complete(SearchNode root) {
            state.Root       = root;
            state.IsBuilding = false;
            if (window != null) window.OnSectionReady(state);
        }
    }
}
