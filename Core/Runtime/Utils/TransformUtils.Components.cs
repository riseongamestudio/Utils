using UnityEngine;

namespace RiseOn.Utils {
    /// <summary>
    /// Edits of one or two components of a transform's position, scale or Euler angles, keeping the rest.<br/>
    /// An index counts like Vector3's indexer: 0 is x, 1 y, 2 z; any other index throws.<br/>
    /// All three at once is the property itself: <c>transform.position = v</c>, <c>transform.position += v</c>.
    /// </summary>
    public static partial class TransformUtils {
        #region Position

        public static void SetPosition(this Transform target, int index, float value) => target.position = target.position.With(index, value);
        public static void AddPosition(this Transform target, int index, float value) => target.position = target.position.With(index, target.position[index] + value);

        public static void SetPositionX(this Transform target, float x) => target.position = target.position.With(0, x);
        public static void SetPositionY(this Transform target, float y) => target.position = target.position.With(1, y);
        public static void SetPositionZ(this Transform target, float z) => target.position = target.position.With(2, z);
        public static void AddPositionX(this Transform target, float x) => target.position += new Vector3(x, 0, 0);
        public static void AddPositionY(this Transform target, float y) => target.position += new Vector3(0, y, 0);
        public static void AddPositionZ(this Transform target, float z) => target.position += new Vector3(0, 0, z);

        public static void SetPositionXY(this Transform target, Vector2 xy) => target.position = new Vector3(xy.x, xy.y, target.position.z);
        public static void SetPositionXZ(this Transform target, Vector2 xz) => target.position = new Vector3(xz.x, target.position.y, xz.y);
        public static void SetPositionYZ(this Transform target, Vector2 yz) => target.position = new Vector3(target.position.x, yz.x, yz.y);
        public static void AddPositionXY(this Transform target, Vector2 xy) => target.position += new Vector3(xy.x, xy.y, 0);
        public static void AddPositionXZ(this Transform target, Vector2 xz) => target.position += new Vector3(xz.x, 0, xz.y);
        public static void AddPositionYZ(this Transform target, Vector2 yz) => target.position += new Vector3(0, yz.x, yz.y);

        #endregion

        #region LocalPosition

        public static void SetLocalPosition(this Transform target, int index, float value) => target.localPosition = target.localPosition.With(index, value);
        public static void AddLocalPosition(this Transform target, int index, float value) => target.localPosition = target.localPosition.With(index, target.localPosition[index] + value);

        public static void SetLocalPositionX(this Transform target, float x) => target.localPosition = target.localPosition.With(0, x);
        public static void SetLocalPositionY(this Transform target, float y) => target.localPosition = target.localPosition.With(1, y);
        public static void SetLocalPositionZ(this Transform target, float z) => target.localPosition = target.localPosition.With(2, z);
        public static void AddLocalPositionX(this Transform target, float x) => target.localPosition += new Vector3(x, 0, 0);
        public static void AddLocalPositionY(this Transform target, float y) => target.localPosition += new Vector3(0, y, 0);
        public static void AddLocalPositionZ(this Transform target, float z) => target.localPosition += new Vector3(0, 0, z);

        public static void SetLocalPositionXY(this Transform target, Vector2 xy) => target.localPosition = new Vector3(xy.x, xy.y, target.localPosition.z);
        public static void SetLocalPositionXZ(this Transform target, Vector2 xz) => target.localPosition = new Vector3(xz.x, target.localPosition.y, xz.y);
        public static void SetLocalPositionYZ(this Transform target, Vector2 yz) => target.localPosition = new Vector3(target.localPosition.x, yz.x, yz.y);
        public static void AddLocalPositionXY(this Transform target, Vector2 xy) => target.localPosition += new Vector3(xy.x, xy.y, 0);
        public static void AddLocalPositionXZ(this Transform target, Vector2 xz) => target.localPosition += new Vector3(xz.x, 0, xz.y);
        public static void AddLocalPositionYZ(this Transform target, Vector2 yz) => target.localPosition += new Vector3(0, yz.x, yz.y);

        #endregion

        #region LocalScale

        public static void SetLocalScale(this Transform target, int index, float value) => target.localScale = target.localScale.With(index, value);
        public static void AddLocalScale(this Transform target, int index, float value) => target.localScale = target.localScale.With(index, target.localScale[index] + value);

        public static void SetLocalScaleX(this Transform target, float x) => target.localScale = target.localScale.With(0, x);
        public static void SetLocalScaleY(this Transform target, float y) => target.localScale = target.localScale.With(1, y);
        public static void SetLocalScaleZ(this Transform target, float z) => target.localScale = target.localScale.With(2, z);
        public static void AddLocalScaleX(this Transform target, float x) => target.localScale += new Vector3(x, 0, 0);
        public static void AddLocalScaleY(this Transform target, float y) => target.localScale += new Vector3(0, y, 0);
        public static void AddLocalScaleZ(this Transform target, float z) => target.localScale += new Vector3(0, 0, z);

        public static void SetLocalScaleXY(this Transform target, Vector2 xy) => target.localScale = new Vector3(xy.x, xy.y, target.localScale.z);
        public static void SetLocalScaleXZ(this Transform target, Vector2 xz) => target.localScale = new Vector3(xz.x, target.localScale.y, xz.y);
        public static void SetLocalScaleYZ(this Transform target, Vector2 yz) => target.localScale = new Vector3(target.localScale.x, yz.x, yz.y);
        public static void AddLocalScaleXY(this Transform target, Vector2 xy) => target.localScale += new Vector3(xy.x, xy.y, 0);
        public static void AddLocalScaleXZ(this Transform target, Vector2 xz) => target.localScale += new Vector3(xz.x, 0, xz.y);
        public static void AddLocalScaleYZ(this Transform target, Vector2 yz) => target.localScale += new Vector3(0, yz.x, yz.y);

        #endregion

        #region EulerAngles

        public static void SetEulerAngles(this Transform target, int index, float value) => target.eulerAngles = target.eulerAngles.With(index, value);
        public static void AddEulerAngles(this Transform target, int index, float value) => target.eulerAngles = target.eulerAngles.With(index, target.eulerAngles[index] + value);

        public static void SetEulerAnglesX(this Transform target, float x) => target.eulerAngles = target.eulerAngles.With(0, x);
        public static void SetEulerAnglesY(this Transform target, float y) => target.eulerAngles = target.eulerAngles.With(1, y);
        public static void SetEulerAnglesZ(this Transform target, float z) => target.eulerAngles = target.eulerAngles.With(2, z);
        public static void AddEulerAnglesX(this Transform target, float x) => target.eulerAngles += new Vector3(x, 0, 0);
        public static void AddEulerAnglesY(this Transform target, float y) => target.eulerAngles += new Vector3(0, y, 0);
        public static void AddEulerAnglesZ(this Transform target, float z) => target.eulerAngles += new Vector3(0, 0, z);

        public static void SetEulerAnglesXY(this Transform target, Vector2 xy) => target.eulerAngles = new Vector3(xy.x, xy.y, target.eulerAngles.z);
        public static void SetEulerAnglesXZ(this Transform target, Vector2 xz) => target.eulerAngles = new Vector3(xz.x, target.eulerAngles.y, xz.y);
        public static void SetEulerAnglesYZ(this Transform target, Vector2 yz) => target.eulerAngles = new Vector3(target.eulerAngles.x, yz.x, yz.y);
        public static void AddEulerAnglesXY(this Transform target, Vector2 xy) => target.eulerAngles += new Vector3(xy.x, xy.y, 0);
        public static void AddEulerAnglesXZ(this Transform target, Vector2 xz) => target.eulerAngles += new Vector3(xz.x, 0, xz.y);
        public static void AddEulerAnglesYZ(this Transform target, Vector2 yz) => target.eulerAngles += new Vector3(0, yz.x, yz.y);

        #endregion

        #region LocalEulerAngles

        public static void SetLocalEulerAngles(this Transform target, int index, float value) => target.localEulerAngles = target.localEulerAngles.With(index, value);
        public static void AddLocalEulerAngles(this Transform target, int index, float value) => target.localEulerAngles = target.localEulerAngles.With(index, target.localEulerAngles[index] + value);

        public static void SetLocalEulerAnglesX(this Transform target, float x) => target.localEulerAngles = target.localEulerAngles.With(0, x);
        public static void SetLocalEulerAnglesY(this Transform target, float y) => target.localEulerAngles = target.localEulerAngles.With(1, y);
        public static void SetLocalEulerAnglesZ(this Transform target, float z) => target.localEulerAngles = target.localEulerAngles.With(2, z);
        public static void AddLocalEulerAnglesX(this Transform target, float x) => target.localEulerAngles += new Vector3(x, 0, 0);
        public static void AddLocalEulerAnglesY(this Transform target, float y) => target.localEulerAngles += new Vector3(0, y, 0);
        public static void AddLocalEulerAnglesZ(this Transform target, float z) => target.localEulerAngles += new Vector3(0, 0, z);

        public static void SetLocalEulerAnglesXY(this Transform target, Vector2 xy) => target.localEulerAngles = new Vector3(xy.x, xy.y, target.localEulerAngles.z);
        public static void SetLocalEulerAnglesXZ(this Transform target, Vector2 xz) => target.localEulerAngles = new Vector3(xz.x, target.localEulerAngles.y, xz.y);
        public static void SetLocalEulerAnglesYZ(this Transform target, Vector2 yz) => target.localEulerAngles = new Vector3(target.localEulerAngles.x, yz.x, yz.y);
        public static void AddLocalEulerAnglesXY(this Transform target, Vector2 xy) => target.localEulerAngles += new Vector3(xy.x, xy.y, 0);
        public static void AddLocalEulerAnglesXZ(this Transform target, Vector2 xz) => target.localEulerAngles += new Vector3(xz.x, 0, xz.y);
        public static void AddLocalEulerAnglesYZ(this Transform target, Vector2 yz) => target.localEulerAngles += new Vector3(0, yz.x, yz.y);

        #endregion
    }
}
