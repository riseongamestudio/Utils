using UnityEngine.UI;

namespace RiseOn.Utils.UI {
    /// <summary>
    /// Sets one channel of <c>Image.color</c>, keeping the others.<br/>
    /// An index counts like Color's indexer: 0 is r, 1 g, 2 b, 3 a.
    /// </summary>
    public static class ImageUtils {
        public static void SetColor(this Image image, int index, float value) => image.color = image.color.With(index, value);

        public static void SetColorR(this Image image, float r) => image.color = image.color.WithR(r);
        public static void SetColorG(this Image image, float g) => image.color = image.color.WithG(g);
        public static void SetColorB(this Image image, float b) => image.color = image.color.WithB(b);
        public static void SetColorA(this Image image, float a) => image.color = image.color.WithA(a);
    }
}
