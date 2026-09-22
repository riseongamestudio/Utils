using System.Collections.Generic;
using UnityEngine;

namespace RiseOn.Utils.Editor.SearchWindow {
    public class SearchNode {
        private List<SearchNode> _children;

        public string Label;
        public string LabelSearch;
        public Texture2D Icon;
        public object Data;
        
        public IReadOnlyList<SearchNode> Children => _children;
        
        public bool HasChildren => _children != null && _children.Count > 0;

        public void AddChild(SearchNode item) {
            if (_children == null) {
                _children = new List<SearchNode>(1);
            }
            
            _children.Add(item);
        }

        public void AddRangeChildren(IEnumerable<SearchNode> items) {
            if (_children == null) {
                _children = new List<SearchNode>(items);
            } else {
                _children.AddRange(items);
            }
        }
    }
}