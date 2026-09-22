using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace RiseOn.Utils.Renderers {
    [ExecuteAlways]
    [HideMonoScript]
    public abstract class RendererAnchorer<TRenderer> : MonoBehaviourExt where TRenderer : Renderer {
        #region PROPERTIES

        [SerializeField]
        private bool disableOnFirstEnable = true;
        
        private bool executedDisableOnFirstEnable;

        [SerializeField, OnValueChanged(nameof(UpdatePosFromAnchor)), Required]
        private TRenderer target;
        
        public TRenderer Target { get => target; set => SetPropThenUpdate(ref target, value); }

        [SerializeField, OnValueChanged(nameof(UpdatePosFromAnchor))]
        private SpriteAlignment anchor;

        public SpriteAlignment Anchor { get => anchor; set => SetPropThenUpdate(ref anchor, value); }

        [HideLabel, Indent, ShowIf(nameof(Anchor), SpriteAlignment.Custom)]
        [SerializeField, OnValueChanged(nameof(UpdatePosFromAnchor))]
        private Vector2 customAnchor = new(0.5f, 0.5f);
        
        public Vector2 CustomAnchor { get => customAnchor; set => SetPropThenUpdate(ref customAnchor, value); }
        
        private void SetPropThenUpdate<T>(ref T orgVal, T newVal) {
            if (EqualityComparer<T>.Default.Equals(orgVal, newVal)) return;

            RecordForUndo(this);
            orgVal = newVal;
            MarkDirty(this);

            UpdatePosFromAnchor();
        }

        #endregion

        #region RENDERER PROPERTIES

        protected abstract Matrix4x4 GetTargetL2WMatrix();
        protected abstract Vector2 GetTargetSize();

        #endregion

        #region TRACK RENDERER PROPS

        private Matrix4x4 prevTargetL2WMatrix;
        private Vector2 prevTargetSize;

        protected virtual bool IsNeedUpdateRdrTrackingProps() {
            if (Target == null) return false;

            if (TF.hasChanged) return true;
            if (prevTargetL2WMatrix != GetTargetL2WMatrix()) return true;
            if (prevTargetSize != GetTargetSize()) return true;

            return false;
        }

        private void UpdateRdrTrackingProps() {
            prevTargetL2WMatrix = GetTargetL2WMatrix();
            prevTargetSize      = GetTargetSize();
            
            TF.hasChanged = false;
        }

        private void LateUpdate() {
            if (IsNeedUpdateRdrTrackingProps()) {
                UpdatePosFromAnchor();
                UpdateRdrTrackingProps();
            }
        }

        #endregion

        private void OnEnable() {
            if (this.IsPlaying()) {
                UpdatePosFromAnchor();

                if (!executedDisableOnFirstEnable) {
                    executedDisableOnFirstEnable = true;

                    if (disableOnFirstEnable) enabled = false;
                }
            }
        }

        [Button, ShowIf("@" + nameof(Target) + "!=null")]
        public void UpdatePosFromAnchor() {
            if (Target == null) return;

            var newPos = GetTargetL2WMatrix().MultiplyPoint3x4(GetTargetSize() * Anchor switch {
                SpriteAlignment.Center       => new(0.5f, 0.5f)
              , SpriteAlignment.TopLeft      => new(0f, 1f)
              , SpriteAlignment.TopCenter    => new(0.5f, 1f)
              , SpriteAlignment.TopRight     => new(1f, 1f)
              , SpriteAlignment.LeftCenter   => new(0f, 0.5f)
              , SpriteAlignment.RightCenter  => new(1f, 0.5f)
              , SpriteAlignment.BottomLeft   => new(0f, 0f)
              , SpriteAlignment.BottomCenter => new(0.5f, 0f)
              , SpriteAlignment.BottomRight  => new(1f, 0f)
              , SpriteAlignment.Custom       => CustomAnchor

              , _ => throw new ArgumentOutOfRangeException()
            });

            if (TF.position == newPos) return;
            
            RecordForUndo(TF);
            TF.position = newPos;
            MarkDirty(TF);
        }
    }
}