using System.Diagnostics;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Utils {
    public class SerializedScriptableObjectExt : SerializedScriptableObject {
        [Conditional("UNITY_EDITOR")] protected void RecordForUndo(Object target = null) => UndoHelper.RecordForUndo(target == null ? this : target);

        [Conditional("UNITY_EDITOR")] protected void RecordForUndo(params Object[] targets) => UndoHelper.RecordForUndo(targets);

        [Conditional("UNITY_EDITOR")] protected void MarkDirty(Object target = null) => UndoHelper.MarkDirty(target == null ? this : target);

        [Conditional("UNITY_EDITOR")] protected void MarkDirty(params Object[] targets) => UndoHelper.MarkDirty(targets);
    }
}
