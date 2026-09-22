using UnityEngine;

namespace RiseOn.Utils.Renderers {
    public static class SpriteRendererExtensions {
        public static void SetAlpha(this SpriteRenderer sprite, float alpha) {
            var color = sprite.color;
            color.a      = alpha;
            sprite.color = color;
        }
    }
}
