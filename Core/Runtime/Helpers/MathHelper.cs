using System.Collections.Generic;
using UnityEngine;

namespace RiseOn.Utils {
    public static class MathHelper {
        public static float Pow(float a, uint x) {
            float result = 1f;

            while (x > 0) {
                if ((x & 1) != 0) {
                    result *= a;
                }

                a *=  a;
                x >>= 1;
            }

            return result;
        }

        /// <summary>
        /// Format: 1 + (-1)^(<see cref="power"/> + 1) * (<see cref="time"/> - 1)^<see cref="power"/> <br/>
        /// ______/ <br/>
        /// _____/  <br/>
        /// ____/   <br/>
        /// ___/    <br/>
        /// __/     <br/>
        /// _/      <br/>
        /// 0----->1<br/>
        /// </summary>
        public static float Evaluate(float time, uint power) {
            return 1 + Pow(-1, power + 1) * Pow(time - 1, power);
        }

        public static void Swap<T>(ref T left, ref T right) => (left, right) = (right, left);

        public static IEnumerable<Vector2Int> IEIndex2D(int xStart, int xLen, int yStart, int yLen) {
            for (int x = xStart; x < xStart + xLen; ++x)
            for (int y = yStart; y < yStart + yLen; ++y)
                yield return new(x, y);
        }
    }
}
