using UnityEngine;
using UnityEngine.UI;

namespace RiseOn.Utils.UI {
    /// <summary>
    /// A graphic that draws nothing but still takes raycasts.<br/>
    /// It blocks clicks without an invisible Image, which would still build a mesh for the canvas.<br/>
    /// The CanvasRenderer gives it the depth that GraphicRaycaster sorts hits by, so it is required even though nothing is drawn.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class RaycastBlocker : Graphic {
        public override void SetMaterialDirty() { }

        public override void SetVerticesDirty() { }

        public override void Rebuild(CanvasUpdate update) { }

        protected override void OnPopulateMesh(VertexHelper vh) { vh.Clear(); }
    }
}