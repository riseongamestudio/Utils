using System.Diagnostics;
using UnityEngine;

namespace RiseOn.Utils {
    public class MonoBehaviourExt : MonoBehaviour {
        private Transform tf;
        public Transform TF => tf != null ? tf : tf = transform;

        private RectTransform rectTF;
        public RectTransform RectTF => rectTF != null ? rectTF : rectTF = GetComponent<RectTransform>();

        [Conditional("UNITY_EDITOR")] protected void RecordForUndo(Object target = null) => UndoHelper.RecordForUndo(target == null ? this : target);

        [Conditional("UNITY_EDITOR")] protected void RecordForUndo(params Object[] targets) => UndoHelper.RecordForUndo(targets);

        [Conditional("UNITY_EDITOR")] protected void MarkDirty(Object target = null) => UndoHelper.MarkDirty(target == null ? this : target);

        [Conditional("UNITY_EDITOR")] protected void MarkDirty(params Object[] targets) => UndoHelper.MarkDirty(targets);
    }
}
