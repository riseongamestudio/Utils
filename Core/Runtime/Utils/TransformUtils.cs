using UnityEngine;

namespace RiseOn.Utils {
    public static partial class TransformUtils {
        public static void ResetLocalValues(this Transform target) {
            target.localPosition = Vector3.zero;
            target.localRotation = Quaternion.identity;
            target.localScale    = Vector3.one;
        }
    }
}
