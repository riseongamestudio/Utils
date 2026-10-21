using UnityEditor;
using UnityEditor.UI;

namespace RiseOn.Utils.UI.Editor {
    /// <summary>
    /// Shows only the raycast settings of a RaycastBlocker.<br/>
    /// Color and material are hidden, since the blocker draws nothing.
    /// </summary>
    [CustomEditor(typeof(RaycastBlocker)), CanEditMultipleObjects]
    public class RaycastBlockerEditor : GraphicEditor {
        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_Script);
            RaycastControlsGUI();
            serializedObject.ApplyModifiedProperties();
        }
    }
}