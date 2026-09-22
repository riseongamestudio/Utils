using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Utils.Renderers {
    public class SpriteMaskExt : RendererExt<SpriteMask> {
        #region RENDERER PROPERTIES

        protected override Sprite GetSprite() => Rdr.sprite;
        protected override void SetSprite(Sprite value) => Rdr.sprite = value;
        protected override bool GetFlipX() => throw new Exception($"RiseOn {GetType().Name}: This function should never be called! (possibly logic error)");
        protected override void SetFlipX(bool value) => throw new Exception($"RiseOn {GetType().Name}: This function should never be called! (possibly logic error)");
        protected override bool GetFlipY() => throw new Exception($"RiseOn {GetType().Name}: This function should never be called! (possibly logic error)");
        protected override void SetFlipY(bool value) => throw new Exception($"RiseOn {GetType().Name}: This function should never be called! (possibly logic error)");

        #endregion

        [SerializeField, OnValueChanged(nameof(UpdateRdrPosFromPivot)), OnValueChanged(nameof(NormalizeRdrLocScale))]
        private bool flipX, flipY;

        public override bool FlipX {
            get => flipX;
            set {
                SetThisPropThenUpdate(ref flipX, value);
                NormalizeRdrLocScale();
            }
        }

        public override bool FlipY {
            get => flipY;
            set {
                SetThisPropThenUpdate(ref flipY, value);
                NormalizeRdrLocScale();
            }
        }
        
        private void NormalizeRdrLocScale() => Rdr.transform.localScale = RdrNomLocScale;

        protected override Vector3 RdrNomLocScale => new(
            FlipX ? -1 : 1
          , FlipY ? -1 : 1
          , 1
        );
    }
}