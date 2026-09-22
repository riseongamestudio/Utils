using System;
using System.Collections.Generic;

namespace RiseOn.Utils {
    public static class EnumExtensions {
        public static void SetEnumIfBigger<TEnum>(ref this TEnum target, TEnum value) where TEnum : struct, Enum {
            if (Comparer<TEnum>.Default.Compare(target, value) < 0) {
                target = value;
            }
        }
    }
}
