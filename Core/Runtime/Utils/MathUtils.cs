using UnityEngine;

namespace RiseOn.Utils {
    public static class MathUtils {
        /// <summary>
        /// Power ease-in: t^power, slow at first and speeding up into 1.<br/>
        /// 1 is linear, 2 is InQuad, 3 InCubic, 4 InQuart, 5 InQuint.
        /// </summary>
        public static float EaseInPower(float t, uint power) {
            return Mathf.Pow(t, power);
        }

        /// <summary>
        /// Power ease-out: 1 - (1 - t)^power, fast at first and slowing into 1.<br/>
        /// 1 is linear, 2 is OutQuad, 3 OutCubic, 4 OutQuart, 5 OutQuint.
        /// </summary>
        public static float EaseOutPower(float t, uint power) {
            return 1 - Mathf.Pow(1 - t, power);
        }

        /// <summary>
        /// Power ease-in-out: slow at both ends and fastest through the middle, reaching 0.5 at t = 0.5.<br/>
        /// The first half is <see cref="EaseInPower"/> squeezed into [0, 0.5], the second <see cref="EaseOutPower"/> into [0.5, 1].<br/>
        /// 2 is InOutQuad, 3 InOutCubic, 4 InOutQuart, 5 InOutQuint.
        /// </summary>
        public static float EaseInOutPower(float t, uint power) {
            return t < 0.5f
                ? Mathf.Pow(2 * t, power) / 2
                : 1 - Mathf.Pow(2 * (1 - t), power) / 2;
        }
    }
}
