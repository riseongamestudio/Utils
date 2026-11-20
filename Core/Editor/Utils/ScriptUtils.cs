using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    /// <summary>
    /// The script file of a C# type, shown as the Inspector shows a component's script.
    /// </summary>
    public static class ScriptUtils {
        private static readonly Dictionary<Type, MonoScript> scripts = new();

        /// <summary>
        /// The script that declares the type: the file named after it, else the first that declares it, as a partial class may span several.<br/>
        /// Null for a type no script in the project declares.
        /// </summary>
        public static MonoScript Find(Type type) {
            if (type == null) return null;
            if (scripts.TryGetValue(type, out var known) && known) return known;

            var found = Search(type);
            scripts[type] = found;
            return found;
        }

        /// <summary>
        /// A read-only field holding the type's script, drawn as an object field without its picker: a click pings the script, a double click opens it.<br/>
        /// Gray as the Inspector's own script field, since nothing here can change it.
        /// </summary>
        public static void Field(Rect rect, GUIContent label, Type type) {
            var script = Find(type);
            Rect field;
            using (new EditorGUI.DisabledScope(true)) {
                field = EditorGUI.PrefixLabel(rect, label);
            }

            var current = Event.current;
            if (current.type is EventType.MouseDown && current.button is 0 && field.Contains(current.mousePosition) && script) {
                if (current.clickCount is 2) AssetDatabase.OpenAsset(script);
                else EditorGUIUtility.PingObject(script);
                current.Use();
            }

            if (current.type is not EventType.Repaint) return;

            var content = script ? new GUIContent(script.name, AssetPreview.GetMiniThumbnail(script)) : new GUIContent("None");
            using (new EditorGUI.DisabledScope(true)) {
                // The icon at the size Unity's object fields give it.
                EditorGUIUtility.SetIconSize(new Vector2(12, 12));
                EditorStyles.objectField.Draw(field, content, false, false, false, false);
                EditorGUIUtility.SetIconSize(Vector2.zero);
            }
        }

        /// <summary>The same field, laid out as a row.</summary>
        public static void Field(GUIContent label, Type type) {
            Field(EditorGUILayout.GetControlRect(), label, type);
        }

        private static MonoScript Search(Type type) {
            MonoScript declaring = null;
            foreach (var guid in AssetDatabase.FindAssets("t:MonoScript")) {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
                if (!script) continue;
                if (script.GetClass() == type) return script;
                if (declaring == null && Declares(script, type)) declaring = script;
            }

            return declaring;
        }

        // A file of a partial class declares it without being named after it, so MonoScript.GetClass misses it.
        private static bool Declares(MonoScript script, Type type) {
            var text = script.text;
            return text.Contains($"class {type.Name}") && (string.IsNullOrEmpty(type.Namespace) || text.Contains(type.Namespace));
        }
    }
}