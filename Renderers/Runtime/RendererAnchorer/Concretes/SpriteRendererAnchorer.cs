using UnityEngine;

namespace RiseOn.Utils.Renderers {
    public class SpriteRendererAnchorer : RendererAnchorer<SpriteRenderer> {
        protected override Matrix4x4 GetTargetL2WMatrix() {
            if (Target == null) return Matrix4x4.identity;

            return
                Target.transform.localToWorldMatrix
              * Matrix4x4.Scale(new(
                    Target.flipX ? -1f : 1f,
                    Target.flipY ? -1f : 1f,
                    1f))
              * Matrix4x4.Translate(Target.localBounds.min.With(VecAxis.Z, 0));
        }

        protected override Vector2 GetTargetSize() {
            if (Target == null) return Vector2.zero;

            return Target.localBounds.size;
        }
    }
}