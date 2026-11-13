using System;
using System.Linq;
using RiseOn.Utils.Editor.SearchWindow;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// Unity's field for a value of a type drawn on one line, in a rect given rather than one the layout picks, for rows split by hand whose parts must line up to the pixel with the rows around them.
    /// </summary>
    public static class RectFieldUtils {
        // Between the two halves of a split value column: room for a prefab override mark between them, with as much on either side.
        private const float HALF_GAP = 6f;

        /// <summary>
        /// <paramref name="value"/>, a value column, split in two halves a gap apart, so rows split so line up with one another.
        /// </summary>
        public static void Split(Rect value, out Rect left, out Rect right) {
            left = new Rect(value.x, value.y, (value.width - HALF_GAP) / 2f, value.height);
            right = Rect.MinMaxRect(left.xMax + HALF_GAP, value.y, value.xMax, value.yMax);
        }

        /// <summary>
        /// Whether <see cref="Draw"/> draws a value of <paramref name="type"/>.
        /// </summary>
        public static bool CanDraw(Type type) {
            return type == typeof(float) || type == typeof(double) || type == typeof(int) || type == typeof(long) || type == typeof(bool) || type == typeof(string)
                   || type.IsEnum || type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) || type == typeof(Vector2Int) || type == typeof(Vector3Int)
                   || type == typeof(Color) || type == typeof(AnimationCurve) || type == typeof(Gradient) || typeof(Object).IsAssignableFrom(type);
        }

        /// <summary>
        /// Draws <paramref name="property"/>'s value in <paramref name="rect"/>, inside Unity's property scope, which draws its prefab override mark, and sets what is edited back through Odin, which records it for Undo.
        /// </summary>
        public static void Draw(InspectorProperty property, Rect rect) {
            var serialized = property.Tree.UnitySerializedObject?.FindProperty(property.UnityPropertyPath);
            if (serialized != null) EditorGUI.BeginProperty(rect, GUIContent.none, serialized);

            var entry = property.ValueEntry;
            EditorGUI.showMixedValue = entry.ValueState is PropertyValueState.PrimitiveValueConflict or PropertyValueState.ReferenceValueConflict;
            if (entry.WeakSmartValue is Enum) {
                ChoiceFieldUtils.Enum(rect, property);
            } else {
                EditorGUI.BeginChangeCheck();
                var edited = Field(rect, entry.WeakSmartValue, entry.TypeOfValue);
                if (EditorGUI.EndChangeCheck()) entry.WeakSmartValue = edited;
            }

            EditorGUI.showMixedValue = false;

            if (serialized != null) EditorGUI.EndProperty();

            static object Field(Rect rect, object value, Type type) {
                return value switch {
                    float single => EditorGUI.FloatField(rect, single),
                    double number => EditorGUI.DoubleField(rect, number),
                    int whole => EditorGUI.IntField(rect, whole),
                    long wide => EditorGUI.LongField(rect, wide),
                    bool flag => EditorGUI.Toggle(rect, flag),
                    Vector2 vector => EditorGUI.Vector2Field(rect, GUIContent.none, vector),
                    Vector3 vector => EditorGUI.Vector3Field(rect, GUIContent.none, vector),
                    Vector4 vector => EditorGUI.Vector4Field(rect, GUIContent.none, vector),
                    Vector2Int vector => EditorGUI.Vector2IntField(rect, GUIContent.none, vector),
                    Vector3Int vector => EditorGUI.Vector3IntField(rect, GUIContent.none, vector),
                    Color color => EditorGUI.ColorField(rect, color),
                    _ when type == typeof(string) => EditorGUI.TextField(rect, (string)value),
                    // Unity draws the curve a point inside the rect it is given, at the top: grown by as much upward, it is level with the fields beside it.
                    _ when type == typeof(AnimationCurve) => EditorGUI.CurveField(new Rect(rect.x, rect.y - 1f, rect.width, rect.height + 1f), (AnimationCurve)value ?? new AnimationCurve()),
                    _ when type == typeof(Gradient) => EditorGUI.GradientField(rect, (Gradient)value ?? new Gradient()),
                    _ => EditorGUI.ObjectField(rect, (Object)value, type, true)
                };
            }
        }
    }
}