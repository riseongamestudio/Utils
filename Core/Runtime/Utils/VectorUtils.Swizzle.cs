using UnityEngine;

namespace RiseOn.Utils {
    /// <summary>
    /// Swizzles: the components of a vector in another order, or a few of them, as a new vector.<br/>
    /// Every order of distinct components is here, so <c>v.ZYX()</c> or <c>v.XZ()</c>; repeats such as <c>XX</c> are not.<br/>
    /// Two components give a Vector2 (Vector2Int), three a Vector3 (Vector3Int), four a Vector4.
    /// </summary>
    public static partial class VectorUtils {
        #region Vector2

        public static Vector2 YX(this Vector2 value) => new(value.y, value.x);

        #endregion

        #region Vector3

        public static Vector2 XY(this Vector3 value) => new(value.x, value.y);
        public static Vector2 XZ(this Vector3 value) => new(value.x, value.z);
        public static Vector2 YX(this Vector3 value) => new(value.y, value.x);
        public static Vector2 YZ(this Vector3 value) => new(value.y, value.z);
        public static Vector2 ZX(this Vector3 value) => new(value.z, value.x);
        public static Vector2 ZY(this Vector3 value) => new(value.z, value.y);
        public static Vector3 XZY(this Vector3 value) => new(value.x, value.z, value.y);
        public static Vector3 YXZ(this Vector3 value) => new(value.y, value.x, value.z);
        public static Vector3 YZX(this Vector3 value) => new(value.y, value.z, value.x);
        public static Vector3 ZXY(this Vector3 value) => new(value.z, value.x, value.y);
        public static Vector3 ZYX(this Vector3 value) => new(value.z, value.y, value.x);

        #endregion

        #region Vector4

        public static Vector2 XY(this Vector4 value) => new(value.x, value.y);
        public static Vector2 XZ(this Vector4 value) => new(value.x, value.z);
        public static Vector2 XW(this Vector4 value) => new(value.x, value.w);
        public static Vector2 YX(this Vector4 value) => new(value.y, value.x);
        public static Vector2 YZ(this Vector4 value) => new(value.y, value.z);
        public static Vector2 YW(this Vector4 value) => new(value.y, value.w);
        public static Vector2 ZX(this Vector4 value) => new(value.z, value.x);
        public static Vector2 ZY(this Vector4 value) => new(value.z, value.y);
        public static Vector2 ZW(this Vector4 value) => new(value.z, value.w);
        public static Vector2 WX(this Vector4 value) => new(value.w, value.x);
        public static Vector2 WY(this Vector4 value) => new(value.w, value.y);
        public static Vector2 WZ(this Vector4 value) => new(value.w, value.z);
        public static Vector3 XYZ(this Vector4 value) => new(value.x, value.y, value.z);
        public static Vector3 XYW(this Vector4 value) => new(value.x, value.y, value.w);
        public static Vector3 XZY(this Vector4 value) => new(value.x, value.z, value.y);
        public static Vector3 XZW(this Vector4 value) => new(value.x, value.z, value.w);
        public static Vector3 XWY(this Vector4 value) => new(value.x, value.w, value.y);
        public static Vector3 XWZ(this Vector4 value) => new(value.x, value.w, value.z);
        public static Vector3 YXZ(this Vector4 value) => new(value.y, value.x, value.z);
        public static Vector3 YXW(this Vector4 value) => new(value.y, value.x, value.w);
        public static Vector3 YZX(this Vector4 value) => new(value.y, value.z, value.x);
        public static Vector3 YZW(this Vector4 value) => new(value.y, value.z, value.w);
        public static Vector3 YWX(this Vector4 value) => new(value.y, value.w, value.x);
        public static Vector3 YWZ(this Vector4 value) => new(value.y, value.w, value.z);
        public static Vector3 ZXY(this Vector4 value) => new(value.z, value.x, value.y);
        public static Vector3 ZXW(this Vector4 value) => new(value.z, value.x, value.w);
        public static Vector3 ZYX(this Vector4 value) => new(value.z, value.y, value.x);
        public static Vector3 ZYW(this Vector4 value) => new(value.z, value.y, value.w);
        public static Vector3 ZWX(this Vector4 value) => new(value.z, value.w, value.x);
        public static Vector3 ZWY(this Vector4 value) => new(value.z, value.w, value.y);
        public static Vector3 WXY(this Vector4 value) => new(value.w, value.x, value.y);
        public static Vector3 WXZ(this Vector4 value) => new(value.w, value.x, value.z);
        public static Vector3 WYX(this Vector4 value) => new(value.w, value.y, value.x);
        public static Vector3 WYZ(this Vector4 value) => new(value.w, value.y, value.z);
        public static Vector3 WZX(this Vector4 value) => new(value.w, value.z, value.x);
        public static Vector3 WZY(this Vector4 value) => new(value.w, value.z, value.y);
        public static Vector4 XYWZ(this Vector4 value) => new(value.x, value.y, value.w, value.z);
        public static Vector4 XZYW(this Vector4 value) => new(value.x, value.z, value.y, value.w);
        public static Vector4 XZWY(this Vector4 value) => new(value.x, value.z, value.w, value.y);
        public static Vector4 XWYZ(this Vector4 value) => new(value.x, value.w, value.y, value.z);
        public static Vector4 XWZY(this Vector4 value) => new(value.x, value.w, value.z, value.y);
        public static Vector4 YXZW(this Vector4 value) => new(value.y, value.x, value.z, value.w);
        public static Vector4 YXWZ(this Vector4 value) => new(value.y, value.x, value.w, value.z);
        public static Vector4 YZXW(this Vector4 value) => new(value.y, value.z, value.x, value.w);
        public static Vector4 YZWX(this Vector4 value) => new(value.y, value.z, value.w, value.x);
        public static Vector4 YWXZ(this Vector4 value) => new(value.y, value.w, value.x, value.z);
        public static Vector4 YWZX(this Vector4 value) => new(value.y, value.w, value.z, value.x);
        public static Vector4 ZXYW(this Vector4 value) => new(value.z, value.x, value.y, value.w);
        public static Vector4 ZXWY(this Vector4 value) => new(value.z, value.x, value.w, value.y);
        public static Vector4 ZYXW(this Vector4 value) => new(value.z, value.y, value.x, value.w);
        public static Vector4 ZYWX(this Vector4 value) => new(value.z, value.y, value.w, value.x);
        public static Vector4 ZWXY(this Vector4 value) => new(value.z, value.w, value.x, value.y);
        public static Vector4 ZWYX(this Vector4 value) => new(value.z, value.w, value.y, value.x);
        public static Vector4 WXYZ(this Vector4 value) => new(value.w, value.x, value.y, value.z);
        public static Vector4 WXZY(this Vector4 value) => new(value.w, value.x, value.z, value.y);
        public static Vector4 WYXZ(this Vector4 value) => new(value.w, value.y, value.x, value.z);
        public static Vector4 WYZX(this Vector4 value) => new(value.w, value.y, value.z, value.x);
        public static Vector4 WZXY(this Vector4 value) => new(value.w, value.z, value.x, value.y);
        public static Vector4 WZYX(this Vector4 value) => new(value.w, value.z, value.y, value.x);

        #endregion

        #region Vector2Int

        public static Vector2Int YX(this Vector2Int value) => new(value.y, value.x);

        #endregion

        #region Vector3Int

        public static Vector2Int XY(this Vector3Int value) => new(value.x, value.y);
        public static Vector2Int XZ(this Vector3Int value) => new(value.x, value.z);
        public static Vector2Int YX(this Vector3Int value) => new(value.y, value.x);
        public static Vector2Int YZ(this Vector3Int value) => new(value.y, value.z);
        public static Vector2Int ZX(this Vector3Int value) => new(value.z, value.x);
        public static Vector2Int ZY(this Vector3Int value) => new(value.z, value.y);
        public static Vector3Int XZY(this Vector3Int value) => new(value.x, value.z, value.y);
        public static Vector3Int YXZ(this Vector3Int value) => new(value.y, value.x, value.z);
        public static Vector3Int YZX(this Vector3Int value) => new(value.y, value.z, value.x);
        public static Vector3Int ZXY(this Vector3Int value) => new(value.z, value.x, value.y);
        public static Vector3Int ZYX(this Vector3Int value) => new(value.z, value.y, value.x);

        #endregion
    }
}
