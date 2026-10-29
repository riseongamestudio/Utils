using LitMotion;
using RiseOn.Utils.Editor;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Utils.LitMotion.Editor {
    /// <summary>
    /// Draws an <see cref="EaseExt"/> on one row after its label, in rects worked out here rather than by the layout, so both fields line up to the pixel.<br/>
    /// The curve shares the row only while the ease is <see cref="Ease.CustomAnimationCurve"/>: the value column split in two halves,<br/>
    /// as a block holding one field splits it.
    /// </summary>
    public sealed class EaseExtDrawer : OdinValueDrawer<EaseExt> {
        private InspectorProperty ease;
        private InspectorProperty curve;

        protected override void Initialize() {
            ease = Property.Children[nameof(EaseExt.ease)];
            curve = Property.Children[nameof(EaseExt.curve)];
        }

        protected override void DrawPropertyLayout(GUIContent label) {
            var row = EditorGUILayout.GetControlRect();
            var value = SirenixEditorGUI.PrefixRect(row, label, out var prefix);
            if (label != null) GUI.Label(prefix, label);

            GUIHelper.PushIndentLevel(0);
            if (ValueEntry.SmartValue.IsCustom) {
                RectFieldUtils.Split(value, out var left, out var right);
                RectFieldUtils.Draw(ease, left);
                RectFieldUtils.Draw(curve, right);
            } else {
                RectFieldUtils.Draw(ease, value);
            }

            GUIHelper.PopIndentLevel();
        }
    }
}