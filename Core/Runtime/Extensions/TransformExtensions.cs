using UnityEngine;

namespace RiseOn.Utils {
    public static class TransformExtensions {
        public static void SetPosition(this Transform target, VecAxis axis, float newValue) {
            var newPos = target.position;
            newPos.Set(axis, newValue);
            target.position = newPos;
        }

        public static void SetPositionXY(this Transform target, Vector2 newValue) {
            var newPos = (Vector3)newValue;
            newPos.z        = target.position.z;
            target.position = newPos;
        }

        public static void SetLocalPositionXY(this Transform target, Vector2 newValue) {
            var newPos = (Vector3)newValue;
            newPos.z             = target.localPosition.z;
            target.localPosition = newPos;
        }

        public static void AddPosition(this Transform target, VecAxis axis, float newValue) {
            var newPos = target.position;
            newPos.Set(axis, target.position.Get(axis) + newValue);
            target.position = newPos;
        }

        public static void AddPositionXY(this Transform target, Vector2 value) {
            var newPos = target.position;
            newPos          += (Vector3)value;
            target.position =  newPos;
        }

        public static void AddPositionXYUndo(this Transform target, Vector2 value) {
            UndoHelper.RecordForUndo(target);
            target.AddPositionXY(value);
            UndoHelper.MarkDirty(target);
        }

        public static void ResetLocalValues(this Transform target) {
            target.localPosition = Vector3.zero;
            target.localRotation = Quaternion.identity;
            target.localScale    = Vector3.one;
        }
    }
}
