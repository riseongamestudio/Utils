using System;
using System.Collections.Generic;
using System.Linq;
using RiseOn.Utils.Editor.SearchWindow;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.ValueResolvers;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RiseOn.Utils.Editor {
    /// <summary>Draws <see cref="SectionListAttribute"/>: a <see cref="Section"/> with the items as <see cref="SectionRows"/> rows.</summary>
    public sealed class SectionListAttributeDrawer<TList, TElement> : OdinAttributeDrawer<SectionListAttribute, TList> where TList : IList<TElement> {
        private readonly SectionRows rows = new();
        private IOrderedCollectionResolver resolver;
        private bool set;
        private string item;
        private ValueResolver<TElement> newItem;
        private ValueResolver<TElement> dropped;

        // The item a dropped object makes for the object edited at the index, default when the list turns it away; null for a list that takes no drop.
        private Func<Object, int, TElement> take;

        // What + picks: the type, by Pick or the list's own, and what the picker gave after the window drew, added at the next draw:
        // an object, or None for an empty item.
        private ValueResolver<Type> pickType;
        private Type                ownPickType;
        private readonly List<(Object Object, bool IsNone)> pickedObjects = new();

        // Objects that take the list's drop are over its bar: the section is lit, and they go to its end once let go.
        private bool isBarLit;

        // As Unity's object fields: an asset holds no scene object, and an object in a prefab only what that prefab holds.
        private bool       allowSceneObjects;
        private GameObject rootPrefab;

        // Values the drop-down picked after the window drew, added at the next draw, where Odin records them for undo.
        private readonly List<TElement> picked = new();

        protected override void Initialize() {
            resolver = (IOrderedCollectionResolver) Property.ChildResolver;
            set      = typeof(TElement).IsEnum;
            item     = Attribute.Item ?? ItemName(typeof(TElement));
            if (Attribute.New != null) newItem = ValueResolver.Get<TElement>(Property, Attribute.New);
            if (Attribute.Drop != null) {
                dropped = ValueResolver.Get<TElement>(Property, Attribute.Drop, new NamedValue("obj", typeof(Object)));
                take = (obj, target) => {
                    dropped.Context.NamedValues.Set("obj", obj);
                    return dropped.GetValue(target);
                };
            } else if (!set && typeof(Object).IsAssignableFrom(typeof(TElement))) take = (obj, _) => TakeObject(obj);

            if (Attribute.Pick != null) pickType = ValueResolver.Get<Type>(Property, Attribute.Pick);
            else if (Attribute.Drop == null && take != null) ownPickType = typeof(TElement);

            var target = Property.Tree.WeakTargets[0] as Object;
            allowSceneObjects = !(target && EditorUtility.IsPersistent(target));
            rootPrefab        = target ? target.GetRootPrefab() : null;
            if (Attribute.Foldable) Section.Restore(Property, !Attribute.Closed);
        }

        protected override void DrawPropertyLayout(GUIContent label) {
            label ??= Property.Label;
            if (newItem != null) ValueResolver.DrawErrors(newItem);
            if (dropped != null) ValueResolver.DrawErrors(dropped);
            if (pickType != null) ValueResolver.DrawErrors(pickType);
            foreach (var value in picked) Add(() => value);
            picked.Clear();
            if (pickedObjects.Count > 0) {
                var index = ValueEntry.SmartValue?.Count ?? 0;
                foreach (var (obj, isNone) in pickedObjects) {
                    if (isNone) resolver.QueueInsertAt(index++, Enumerable.Range(0, ValueEntry.ValueCount).Select(_ => (object) (newItem == null ? New() : newItem.GetValue())).ToArray());
                    else if (Takes(obj)) resolver.QueueInsertAt(index++, Enumerable.Range(0, ValueEntry.ValueCount).Select(target => (object) take(obj, target)).ToArray());
                }

                pickedObjects.Clear();
                GUIHelper.RequestRepaint();
            }

            var type = Event.current.type;
            Rect bar;
            using (var section = Attribute.Foldable
                       ? Section.Foldout(Property, label.text, label.tooltip, buttons: 1, padded: false)
                       : new Section(label.text, label.tooltip, buttons: 1, padded: false)) {
                bar = section.Bar;
                DrawSection(section);
            }

            HandleBarDrop(type, bar);
        }

        private void DrawSection(Section section) {
            var list = ValueEntry.SmartValue;
            var choices = set
                ? ((TElement[]) Enum.GetValues(typeof(TElement))).Where(value => list == null || !list.Contains(value)).Select(value => new SearchChoice<TElement>(ChoiceFieldUtils.NameOf((Enum) (object) value), value)).ToList()
                : null;
            var pickAs = PickType();
            SectionRows.AddButton(section, 0, item, choices, value => {
                if (pickAs != null) Pick(section.ButtonRect(0), pickAs);
                else if (set) picked.Add(value);
                else Add(newItem == null ? New : () => newItem.GetValue());
                if (Attribute.Foldable && pickAs == null) Property.State.Expanded = true;
            });
            if (!section.Open) return;

            var count = Property.Children.Count;
            if (set) rows.Draw(count, i => EditorGUILayout.LabelField(ChoiceFieldUtils.NameOf((Enum) (object) list[i])), "None", remove: resolver.QueueRemoveAt, removeTooltip: $"Remove the {item}");
            else if (take == null || dropped is { HasError: true }) rows.Draw(count, i => Property.Children[i].Draw(null), "None", Move, resolver.QueueRemoveAt, $"Remove the {item}");
            else rows.Draw(count, i => Property.Children[i].Draw(null), "None", Move, resolver.QueueRemoveAt, $"Remove the {item}", takes: Takes, drop: Drop);
        }

        // As Odin's and Unity's lists take objects let go on their title: at the end, the whole list lit meanwhile.
        // After the section, so a drop over the rows is theirs and the light goes over the content.
        private void HandleBarDrop(EventType type, Rect bar) {
            if (take == null || dropped is { HasError: true }) return;

            var current = Event.current;
            var wasLit  = isBarLit;
            if (type is EventType.DragExited || SectionRows.HasDragEnded(type)) isBarLit = false;
            else if (type is EventType.DragUpdated or EventType.DragPerform) {
                var taken = current.type is EventType.Used || !bar.Contains(current.mousePosition) ? null : DragAndDrop.objectReferences.Where(Takes).ToList();
                isBarLit = taken is { Count: > 0 };
                if (isBarLit) {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (type is EventType.DragPerform) {
                        DragAndDrop.AcceptDrag();
                        isBarLit = false;
                        Drop(ValueEntry.SmartValue?.Count ?? 0, taken);
                    }

                    current.Use();
                }
            }

            if (isBarLit) Section.DrawDropLight(GUILayoutUtility.GetLastRect());
            if (isBarLit != wasLit) GUIHelper.RequestRepaint();
        }

        private bool Takes(Object obj) => obj && (allowSceneObjects || EditorUtility.IsPersistent(obj)) && !IsNone(take(obj, 0));

        private Type PickType() => take == null || dropped is { HasError: true } ? null : pickType is { HasError: false } ? pickType.GetValue() : ownPickType;

        // The object picker a SerObject field opens; the pick is added at the next draw, where Odin records it for undo.
        private void Pick(Rect button, Type type) {
            var window = GUIHelper.CurrentWindow;
            ObjectSearchWindow.Open(button, ObjectNames.NicifyVariableName(type.Name), node => {
                if (node.Data is Object obj) {
                    if (!obj) return;

                    pickedObjects.Add((obj, false));
                } else pickedObjects.Add((null, true));

                if (Attribute.Foldable) Property.State.Expanded = true;
                if (window) window.Repaint();
            }, type, rootPrefab, allowSceneObjects);
        }

        // Each object one item, in the order dragged, from where they landed; one item each object edited, as + adds.
        private void Drop(int index, IReadOnlyList<Object> objects) {
            for (var i = 0; i < objects.Count; i++) {
                var obj = objects[i];
                resolver.QueueInsertAt(index + i, Enumerable.Range(0, ValueEntry.ValueCount).Select(target => (object) take(obj, target)).ToArray());
            }

            GUIHelper.RequestRepaint();
        }

        private static bool IsNone(TElement value) => EqualityComparer<TElement>.Default.Equals(value, default);

        // As Unity's own list takes an object: one of the type, or a game object's first component of it.
        private static TElement TakeObject(Object obj) {
            var type = typeof(TElement);
            if (type.IsInstanceOfType(obj)) return (TElement) (object) obj;

            return obj is GameObject gameObject && typeof(Component).IsAssignableFrom(type) && gameObject.TryGetComponent(type, out var component) ? (TElement) (object) component : default;
        }

        // A set's value goes where the enum orders it; any other item at the end.
        private void Add(Func<TElement> make) {
            var list  = ValueEntry.SmartValue;
            var index = list?.Count ?? 0;
            if (set) {
                var value = make();
                if (list != null && list.Contains(value)) return;

                index = list?.Count(other => Comparer<TElement>.Default.Compare(other, value) < 0) ?? 0;
            }

            // One each object edited, so no two share a new instance.
            resolver.QueueInsertAt(index, Enumerable.Range(0, ValueEntry.ValueCount).Select(_ => (object) make()).ToArray());
            GUIHelper.RequestRepaint();
        }

        // Odin's resolver has no move, so the item leaves its place and comes back at the other, in every object edited.
        private void Move(int from, int to) {
            var values = Enumerable.Range(0, ValueEntry.ValueCount).Select(target => (object) ValueEntry.Values[target][from]).ToArray();
            resolver.QueueRemoveAt(from);
            resolver.QueueInsertAt(to, values);
        }

        // A wrapper of one type, such as SerRef<IWipeTarget>, by what it wraps; an interface without its I: "wipe target".
        private static string ItemName(Type type) {
            if (type.IsGenericType && type.GetGenericArguments().Length == 1) type = type.GetGenericArguments()[0];
            var name = type.IsGenericType ? type.Name[..type.Name.IndexOf('`')] : type.Name;
            if (type.IsInterface && name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1])) name = name[1..];
            return ObjectNames.NicifyVariableName(name).ToLowerInvariant();
        }

        // What Odin's own list adds: an empty string, a new instance of a class that has a parameterless constructor, else the default.
        private static TElement New() {
            var type = typeof(TElement);
            if (type == typeof(string)) return (TElement) (object) string.Empty;
            if (type.IsValueType || typeof(UnityEngine.Object).IsAssignableFrom(type) || type.IsAbstract || type.GetConstructor(Type.EmptyTypes) == null) return default;

            return (TElement) Activator.CreateInstance(type);
        }
    }
}