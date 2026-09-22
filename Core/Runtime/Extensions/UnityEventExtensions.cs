using System.Diagnostics;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace RiseOn.Utils {
    /// <summary>
    /// Persistent listener edits (the ones saved with the asset), wrapped in Undo and dirty marking.<br/>
    /// <c>holder</c> is the object that owns <c>unityEvent</c>, the one to record and mark dirty.
    /// </summary>
    public static class UnityEventExtensions {
        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.AddPersistentListener(UnityEventBase)"/>
        [Conditional("UNITY_EDITOR")]
        public static void AddEvent(this UnityEventBase unityEvent, Object holder) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(unityEvent);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.AddPersistentListener(UnityEvent, UnityAction)"/>
        /// <param name="unique">Remove event before add</param>
        [Conditional("UNITY_EDITOR")]
        public static void AddEvent(this UnityEvent unityEvent, Object holder, UnityAction call, bool unique = true) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            if (unique) unityEvent.RemoveEvent(holder, call);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(unityEvent, call);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.AddPersistentListener{T0}(UnityEvent{T0}, UnityAction{T0})"/>
        /// <param name="unique">Remove event before add</param>
        [Conditional("UNITY_EDITOR")]
        public static void AddEvent<T0>(this UnityEvent<T0> unityEvent, Object holder, UnityAction<T0> call, bool unique = true) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            if (unique) unityEvent.RemoveEvent(holder, call);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(unityEvent, call);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.AddPersistentListener{T0,T1}(UnityEvent{T0,T1}, UnityAction{T0,T1})"/>
        /// <param name="unique">Remove event before add</param>
        [Conditional("UNITY_EDITOR")]
        public static void AddEvent<T0, T1>(this UnityEvent<T0, T1> unityEvent, Object holder, UnityAction<T0, T1> call, bool unique = true) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            if (unique) unityEvent.RemoveEvent(holder, call);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(unityEvent, call);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.AddPersistentListener{T0,T1,T2}(UnityEvent{T0,T1,T2}, UnityAction{T0,T1,T2})"/>
        /// <param name="unique">Remove event before add</param>
        [Conditional("UNITY_EDITOR")]
        public static void AddEvent<T0, T1, T2>(this UnityEvent<T0, T1, T2> unityEvent, Object holder, UnityAction<T0, T1, T2> call, bool unique = true) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            if (unique) unityEvent.RemoveEvent(holder, call);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(unityEvent, call);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.AddPersistentListener{T0,T1,T2,T3}(UnityEvent{T0,T1,T2,T3}, UnityAction{T0,T1,T2,T3})"/>
        /// <param name="unique">Remove event before add</param>
        [Conditional("UNITY_EDITOR")]
        public static void AddEvent<T0, T1, T2, T3>(this UnityEvent<T0, T1, T2, T3> unityEvent, Object holder, UnityAction<T0, T1, T2, T3> call, bool unique = true) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            if (unique) unityEvent.RemoveEvent(holder, call);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(unityEvent, call);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        // ── Remove ────────────────────────────────────────────────────────────

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.RemovePersistentListener(UnityEventBase, int)"/>
        [Conditional("UNITY_EDITOR")]
        public static void RemoveEvent(this UnityEventBase unityEvent, Object holder, int index) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(unityEvent, index);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.RemovePersistentListener(UnityEventBase, UnityAction)"/>
        [Conditional("UNITY_EDITOR")]
        public static void RemoveEvent(this UnityEventBase unityEvent, Object holder, UnityAction call) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(unityEvent, call);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.RemovePersistentListener{T0}(UnityEventBase, UnityAction{T0})"/>
        [Conditional("UNITY_EDITOR")]
        public static void RemoveEvent<T0>(this UnityEventBase unityEvent, Object holder, UnityAction<T0> call) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(unityEvent, call);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.RemovePersistentListener{T0,T1}(UnityEventBase, UnityAction{T0,T1})"/>
        [Conditional("UNITY_EDITOR")]
        public static void RemoveEvent<T0, T1>(this UnityEventBase unityEvent, Object holder, UnityAction<T0, T1> call) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(unityEvent, call);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.RemovePersistentListener{T0,T1,T2}(UnityEventBase, UnityAction{T0,T1,T2})"/>
        [Conditional("UNITY_EDITOR")]
        public static void RemoveEvent<T0, T1, T2>(this UnityEventBase unityEvent, Object holder, UnityAction<T0, T1, T2> call) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(unityEvent, call);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.RemovePersistentListener{T0,T1,T2,T3}(UnityEventBase, UnityAction{T0,T1,T2,T3})"/>
        [Conditional("UNITY_EDITOR")]
        public static void RemoveEvent<T0, T1, T2, T3>(this UnityEventBase unityEvent, Object holder, UnityAction<T0, T1, T2, T3> call) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(unityEvent, call);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        // ── Register ──────────────────────────────────────────────────────────

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.RegisterPersistentListener(UnityEvent, int, UnityAction)"/>
        [Conditional("UNITY_EDITOR")]
        public static void RegisterEvent(this UnityEvent unityEvent, Object holder, int index, UnityAction call) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            UnityEditor.Events.UnityEventTools.RegisterPersistentListener(unityEvent, index, call);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.RegisterPersistentListener{T0}(UnityEvent{T0}, int, UnityAction{T0})"/>
        [Conditional("UNITY_EDITOR")]
        public static void RegisterEvent<T0>(this UnityEvent<T0> unityEvent, Object holder, int index, UnityAction<T0> call) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            UnityEditor.Events.UnityEventTools.RegisterPersistentListener(unityEvent, index, call);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.RegisterPersistentListener{T0,T1}(UnityEvent{T0,T1}, int, UnityAction{T0,T1})"/>
        [Conditional("UNITY_EDITOR")]
        public static void RegisterEvent<T0, T1>(this UnityEvent<T0, T1> unityEvent, Object holder, int index, UnityAction<T0, T1> call) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            UnityEditor.Events.UnityEventTools.RegisterPersistentListener(unityEvent, index, call);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.RegisterPersistentListener{T0,T1,T2}(UnityEvent{T0,T1,T2}, int, UnityAction{T0,T1,T2})"/>
        [Conditional("UNITY_EDITOR")]
        public static void RegisterEvent<T0, T1, T2>(this UnityEvent<T0, T1, T2> unityEvent, Object holder, int index, UnityAction<T0, T1, T2> call) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            UnityEditor.Events.UnityEventTools.RegisterPersistentListener(unityEvent, index, call);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.RegisterPersistentListener{T0,T1,T2,T3}(UnityEvent{T0,T1,T2,T3}, int, UnityAction{T0,T1,T2,T3})"/>
        [Conditional("UNITY_EDITOR")]
        public static void RegisterEvent<T0, T1, T2, T3>(this UnityEvent<T0, T1, T2, T3> unityEvent, Object holder, int index, UnityAction<T0, T1, T2, T3> call) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            UnityEditor.Events.UnityEventTools.RegisterPersistentListener(unityEvent, index, call);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        // ── Unregister ────────────────────────────────────────────────────────

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.UnregisterPersistentListener(UnityEventBase, int)"/>
        [Conditional("UNITY_EDITOR")]
        public static void UnregisterEvent(this UnityEventBase unityEvent, Object holder, int index) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            UnityEditor.Events.UnityEventTools.UnregisterPersistentListener(unityEvent, index);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        // ── Void (param) ───────────────────────────────────────────────

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(UnityEventBase, UnityAction)"/>
        /// <param name="unique">Remove event before add</param>
        [Conditional("UNITY_EDITOR")]
        public static void AddVoidEvent(this UnityEventBase unityEvent, Object holder, UnityAction call, bool unique = true) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            if (unique) unityEvent.RemoveEvent(holder, call);
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(unityEvent, call);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.RegisterVoidPersistentListener(UnityEventBase, int, UnityAction)"/>
        [Conditional("UNITY_EDITOR")]
        public static void RegisterVoidEvent(this UnityEventBase unityEvent, Object holder, int index, UnityAction call) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            UnityEditor.Events.UnityEventTools.RegisterVoidPersistentListener(unityEvent, index, call);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        // ── Int (param) ────────────────────────────────────────────────

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.AddIntPersistentListener(UnityEventBase, UnityAction{int}, int)"/>
        /// <param name="unique">Remove event before add</param>
        [Conditional("UNITY_EDITOR")]
        public static void AddIntEvent(this UnityEventBase unityEvent, Object holder, UnityAction<int> call, int argument, bool unique = true) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            if (unique) unityEvent.RemoveEvent(holder, call);
            UnityEditor.Events.UnityEventTools.AddIntPersistentListener(unityEvent, call, argument);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.RegisterIntPersistentListener(UnityEventBase, int, UnityAction{int}, int)"/>
        [Conditional("UNITY_EDITOR")]
        public static void RegisterIntEvent(this UnityEventBase unityEvent, Object holder, int index, UnityAction<int> call, int argument) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            UnityEditor.Events.UnityEventTools.RegisterIntPersistentListener(unityEvent, index, call, argument);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        // ── Float (param) ──────────────────────────────────────────────

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.AddFloatPersistentListener(UnityEventBase, UnityAction{float}, float)"/>
        /// <param name="unique">Remove event before add</param>
        [Conditional("UNITY_EDITOR")]
        public static void AddFloatEvent(this UnityEventBase unityEvent, Object holder, UnityAction<float> call, float argument, bool unique = true) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            if (unique) unityEvent.RemoveEvent(holder, call);
            UnityEditor.Events.UnityEventTools.AddFloatPersistentListener(unityEvent, call, argument);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.RegisterFloatPersistentListener(UnityEventBase, int, UnityAction{float}, float)"/>
        [Conditional("UNITY_EDITOR")]
        public static void RegisterFloatEvent(this UnityEventBase unityEvent, Object holder, int index, UnityAction<float> call, float argument) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            UnityEditor.Events.UnityEventTools.RegisterFloatPersistentListener(unityEvent, index, call, argument);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        // ── Bool (param) ───────────────────────────────────────────────

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(UnityEventBase, UnityAction{bool}, bool)"/>
        /// <param name="unique">Remove event before add</param>
        [Conditional("UNITY_EDITOR")]
        public static void AddBoolEvent(this UnityEventBase unityEvent, Object holder, UnityAction<bool> call, bool argument, bool unique = true) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            if (unique) unityEvent.RemoveEvent(holder, call);
            UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(unityEvent, call, argument);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.RegisterBoolPersistentListener(UnityEventBase, int, UnityAction{bool}, bool)"/>
        [Conditional("UNITY_EDITOR")]
        public static void RegisterBoolEvent(this UnityEventBase unityEvent, Object holder, int index, UnityAction<bool> call, bool argument) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            UnityEditor.Events.UnityEventTools.RegisterBoolPersistentListener(unityEvent, index, call, argument);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        // ── String (param) ─────────────────────────────────────────────

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.AddStringPersistentListener(UnityEventBase, UnityAction{string}, string)"/>
        /// <param name="unique">Remove event before add</param>
        [Conditional("UNITY_EDITOR")]
        public static void AddStringEvent(this UnityEventBase unityEvent, Object holder, UnityAction<string> call, string argument, bool unique = true) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            if (unique) unityEvent.RemoveEvent(holder, call);
            UnityEditor.Events.UnityEventTools.AddStringPersistentListener(unityEvent, call, argument);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.RegisterStringPersistentListener(UnityEventBase, int, UnityAction{string}, string)"/>
        [Conditional("UNITY_EDITOR")]
        public static void RegisterStringEvent(this UnityEventBase unityEvent, Object holder, int index, UnityAction<string> call, string argument) {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            UnityEditor.Events.UnityEventTools.RegisterStringPersistentListener(unityEvent, index, call, argument);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        // ── Object (param) ─────────────────────────────────────────────

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.AddObjectPersistentListener{T}(UnityEventBase, UnityAction{T}, T)"/>
        /// <param name="unique">Remove event before add</param>
        [Conditional("UNITY_EDITOR")]
        public static void AddObjectEvent<T>(this UnityEventBase unityEvent, Object holder, UnityAction<T> call, T argument, bool unique = true) where T : Object {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            if (unique) unityEvent.RemoveEvent(holder, call);
            UnityEditor.Events.UnityEventTools.AddObjectPersistentListener(unityEvent, call, argument);
            UndoUtils.MarkDirty(holder);
            #endif
        }

        /// <inheritdoc cref="UnityEditor.Events.UnityEventTools.RegisterObjectPersistentListener{T}(UnityEventBase, int, UnityAction{T}, T)"/>
        [Conditional("UNITY_EDITOR")]
        public static void RegisterObjectEvent<T>(this UnityEventBase unityEvent, Object holder, int index, UnityAction<T> call, T argument) where T : Object {
            #if UNITY_EDITOR
            UndoUtils.RecordForUndo(holder);
            UnityEditor.Events.UnityEventTools.RegisterObjectPersistentListener(unityEvent, index, call, argument);
            UndoUtils.MarkDirty(holder);
            #endif
        }
    }
}
