using UnityEngine.UI;

namespace RiseOn.Utils.UI {
    /// <summary>
    /// A graphic that draws nothing but still takes raycasts.<br/>
    /// It blocks clicks without an invisible Image, which would still build a mesh for the canvas.
    /// </summary>
    public class RaycastBlocker : Graphic {
        public override void SetMaterialDirty() { }

        public override void SetVerticesDirty() { }

        public override void Rebuild(CanvasUpdate update) { }

        protected override void OnPopulateMesh(VertexHelper vh) {
            vh.Clear();
        }
    }
}
