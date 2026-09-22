using UnityEngine;

namespace RiseOn.Utils.Renderers {
    public class SpriteRendererExt : RendererExt<SpriteRenderer> {
        #region RENDERER PROPERTIES

        protected override Sprite GetSprite() => Rdr.sprite;
        protected override void SetSprite(Sprite value) => Rdr.sprite = value;
        protected override bool GetFlipX() => Rdr.flipX;
        protected override void SetFlipX(bool value) => Rdr.flipX = value;
        protected override bool GetFlipY() => Rdr.flipY;
        protected override void SetFlipY(bool value) => Rdr.flipY = value;

        public SpriteDrawMode DrawMode {
            get => Rdr.drawMode;
            set {
                if (Rdr.drawMode == value) return;

                UndoUtils.RecordForUndo(transform, Rdr, Rdr.transform);

                Rdr.drawMode = value;

                TryApplyRdrLocScaleToThis();

                UndoUtils.MarkDirty(transform, Rdr, Rdr.transform);

                UpdateRdrPosFromPivot();
            }
        }

        private Vector2 GetSize() => Rdr.size;
        private void SetSize(Vector2 value) => Rdr.size = value;
        public Vector2 Size { get => GetSize(); set => SetRdrPropThenUpdate(GetSize, SetSize, value); }

        #endregion
    }
}