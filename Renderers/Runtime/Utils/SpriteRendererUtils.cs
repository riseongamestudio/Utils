using UnityEngine;

namespace RiseOn.Utils.Renderers {
    /// <summary>
    /// Sets one channel of <c>SpriteRenderer.color</c>, keeping the others.<br/>
    /// An index counts like Color's indexer: 0 is r, 1 g, 2 b, 3 a.
    /// </summary>
    public static class SpriteRendererUtils {
        public static void SetColor(this SpriteRenderer sprite, int index, float value) => sprite.color = sprite.color.With(index, value);

        public static void SetColorR(this SpriteRenderer sprite, float r) => sprite.color = sprite.color.WithR(r);
        public static void SetColorG(this SpriteRenderer sprite, float g) => sprite.color = sprite.color.WithG(g);
        public static void SetColorB(this SpriteRenderer sprite, float b) => sprite.color = sprite.color.WithB(b);
        public static void SetColorA(this SpriteRenderer sprite, float a) => sprite.color = sprite.color.WithA(a);
    }
}
