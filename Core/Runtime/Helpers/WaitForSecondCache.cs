using System.Collections.Generic;
using UnityEngine;

namespace RiseOn.Utils {
    public static class WaitForSecondCache {
        private static Dictionary<float, WaitForSeconds> cache;

        public static WaitForSeconds Get(float time) {
            cache ??= new();
            if (!cache.ContainsKey(time)) cache[time] = new WaitForSeconds(time);
            return cache[time];
        }
    }
}