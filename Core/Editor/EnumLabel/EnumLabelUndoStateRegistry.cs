using System.Collections.Generic;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    internal static class EnumLabelUndoStateRegistry {
        private sealed class TargetState {
            public Object Target { get; }
            public EnumLabelUndoState UndoState { get; }

            public TargetState(Object target, EnumLabelUndoState undoState) {
                Target    = target;
                UndoState = undoState;
            }
        }

        private static readonly Dictionary<string, List<TargetState>> TARGET_STATES   = new();
        private static readonly Dictionary<string, EnumLabelUndoState> FALLBACK_STATES = new();

        public static EnumLabelUndoState GetOrCreate(
            string key
          , Object target
          , string enumTypeName) {
            if (!TARGET_STATES.TryGetValue(key, out var targetStates)) {
                targetStates       = new List<TargetState>();
                TARGET_STATES[key] = targetStates;
            }

            foreach (var targetState in targetStates) {
                if (ReferenceEquals(targetState.Target, target)) return targetState.UndoState;
            }

            var undoState = CreateUndoState(enumTypeName);
            targetStates.Add(new TargetState(
                target
              , undoState));

            return undoState;
        }

        public static EnumLabelUndoState GetOrCreateFallback(string key, string enumTypeName) {
            if (FALLBACK_STATES.TryGetValue(key, out var state) && state is not null) return state;

            state                = CreateUndoState(enumTypeName);
            FALLBACK_STATES[key] = state;
            return state;
        }

        private static EnumLabelUndoState CreateUndoState(string enumTypeName) {
            var state = ScriptableObject.CreateInstance<EnumLabelUndoState>();
            state.hideFlags    = HideFlags.HideAndDontSave;
            state.EnumTypeName = enumTypeName;

            return state;
        }
    }
}
