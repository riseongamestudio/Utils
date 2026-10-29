using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Draws a <see cref="DirectionExt"/> on one row after its label: the direction, then, for a custom one, its angle.<br/>
    /// While the direction is <see cref="Direction.Custom"/>, an arrow sets the angle by dragging around it, next to a field for exact degrees.
    /// </summary>
    public sealed class DirectionExtDrawer : OdinValueDrawer<DirectionExt> {
        // Each field keeps a 3px margin on its own side, so the space ahead of the arrow makes up for it.
        private const float ARROW_GAP = 6f;
        private const float FIELD_MARGIN = 3f;

        private InspectorProperty direction;
        private InspectorProperty customAngle;

        protected override void Initialize() {
            direction = Property.Children[nameof(DirectionExt.direction)];
            customAngle = Property.Children[nameof(DirectionExt.customAngle)];
        }

        protected override void DrawPropertyLayout(GUIContent label) {
            SirenixEditorGUI.BeginHorizontalPropertyLayout(label);
            GUIHelper.PushIndentLevel(0);

            direction.Draw(null);
            if (ValueEntry.SmartValue.IsCustom) {
                // Sized by a constant, since the layout pass reserves the space before any rect is known.
                var size = EditorGUIUtility.singleLineHeight;
                var field = GUILayoutUtility.GetLastRect();

                // A space takes no part in the row's margins, so the row keeps the spacing of its fields.
                GUILayout.Space(ARROW_GAP + size + ARROW_GAP - FIELD_MARGIN);
                var space = GUILayoutUtility.GetLastRect();

                // Centered between the fields on both sides, and level with them.
                var left = (field.xMax + space.xMax + FIELD_MARGIN - size) * .5f;
                DrawArrow(customAngle, new Rect(left, field.center.y - size * .5f, size, size));

                customAngle.Draw(null);
            }

            GUIHelper.PopIndentLevel();
            SirenixEditorGUI.EndHorizontalPropertyLayout();

            // An arrow across a disc the height of a field; pressing or dragging on it points the arrow at the mouse.
            static void DrawArrow(InspectorProperty customAngle, Rect rect) {
                // Snapped to whole pixels as tall as the field; the arrow takes the parity of that height so it sits on the exact middle.
                var ppp = EditorGUIUtility.pixelsPerPoint;
                var top = Mathf.Round(rect.y * ppp);
                var height = Mathf.Round(rect.height * ppp);
                var thickness = (height % 2f is 0f ? 2f : 3f) / ppp;
                var left = Mathf.Round(rect.x * ppp);
                rect = new Rect(left / ppp, top / ppp, height / ppp, height / ppp);

                var id = GUIUtility.GetControlID(FocusType.Passive, rect);
                var center = rect.center;
                var radius = rect.height * .5f;
                var current = Event.current;

                switch (current.GetTypeForControl(id)) {
                    case EventType.MouseDown when current.button is 0 && rect.Contains(current.mousePosition):
                        GUIUtility.hotControl = id;
                        customAngle.ValueEntry.WeakSmartValue = AngleTo(center, current.mousePosition);
                        current.Use();
                        break;
                    case EventType.MouseDrag when GUIUtility.hotControl == id:
                        customAngle.ValueEntry.WeakSmartValue = AngleTo(center, current.mousePosition);
                        current.Use();
                        break;
                    case EventType.MouseUp when GUIUtility.hotControl == id:
                        GUIUtility.hotControl = 0;
                        current.Use();
                        break;
                    case EventType.Repaint:
                        var radians = (float)customAngle.ValueEntry.WeakSmartValue * Mathf.Deg2Rad;
                        var pointing = new Vector2(Mathf.Cos(radians), -Mathf.Sin(radians));
                        var arrow = radius - 2f;
                        var tip = center + pointing * arrow;
                        var back = tip - pointing * arrow * .5f;
                        var side = new Vector2(-pointing.y, pointing.x) * arrow * .4f;

                        // Drawn like a number field through Unity's rounded rect: the field shade, a 1px border, darker along the top.
                        var pixel = 1f / EditorGUIUtility.pixelsPerPoint;
                        var pro = EditorGUIUtility.isProSkin;
                        var corners = Vector4.one * radius;
                        var white = Texture2D.whiteTexture;
                        GUI.DrawTexture(rect, white, ScaleMode.StretchToFill, true, 0f, pro ? new Color(.165f, .165f, .165f) : new Color(.94f, .94f, .94f), Vector4.zero, corners);
                        GUI.DrawTexture(rect, white, ScaleMode.StretchToFill, true, 0f, pro ? new Color(.13f, .13f, .13f) : new Color(.6f, .6f, .6f), Vector4.one * pixel, corners);
                        GUI.DrawTexture(rect, white, ScaleMode.StretchToFill, true, 0f, pro ? new Color(.05f, .05f, .05f) : new Color(.45f, .45f, .45f), new Vector4(0f, pixel, 0f, 0f), corners);

                        Handles.color = GUI.enabled ? Color.white : Color.gray;
                        Handles.DrawAAPolyLine(thickness, center - pointing * arrow, tip);
                        Handles.DrawAAPolyLine(thickness, back + side, tip, back - side);
                        break;
                }
            }

            // GUI space grows downward, so y is flipped to keep angles counterclockwise.
            static float AngleTo(Vector2 center, Vector2 mouse) {
                var degrees = Mathf.Atan2(center.y - mouse.y, mouse.x - center.x) * Mathf.Rad2Deg;
                return Mathf.Round(degrees < 0f ? degrees + 360f : degrees);
            }
        }
    }
}