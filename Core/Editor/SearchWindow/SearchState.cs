using System.Collections.Generic;
using UnityEngine;

namespace RiseOn.Utils.Editor.SearchWindow {
    public class LevelState {
        public Vector2 ScrollPosition = Vector2.zero;
        public int SelectedIndex = -1;
    }

    public class SectionState {
        // Read-only collections to prevent external reassignment
        public readonly Stack<SearchNode> NavigationStack = new Stack<SearchNode>();
        public readonly List<SearchNode> CurrentItems = new List<SearchNode>();
        public readonly List<SearchNode> FilteredItems = new List<SearchNode>();
        public readonly List<SearchNode> AllFlattened = new List<SearchNode>();
        public readonly Dictionary<SearchNode, LevelState> LevelStates = new Dictionary<SearchNode, LevelState>();
        
        // Public state fields
        public Vector2 ScrollPosition;
        public int SelectedIndex = -1;
        public string Label;
        public bool IsBuilding = true;
        public float BuildProgress = 0f;
        public SearchNode Root;
    }

    public class SectionBuildContext {
        private readonly SectionState _state;
        private readonly SearchWindow _window;

        internal SectionBuildContext(SectionState state, SearchWindow window) {
            _state  = state;
            _window = window;
        }

        public void ReportProgress(float progress) {
            _state.BuildProgress = Mathf.Clamp01(progress);
            _window.Repaint();
        }

        public void Complete(SearchNode root) {
            _state.Root       = root;
            _state.IsBuilding = false;
            _window.OnSectionReady(_state);
            _window.Repaint();
        }
    }
}