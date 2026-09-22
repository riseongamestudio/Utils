using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Sirenix.OdinInspector;
using Debug = UnityEngine.Debug;

namespace RiseOn.Utils.Renderers {
    [ExecuteAlways]
    [HideMonoScript]
    public abstract partial class RendererExt<TRenderer> : MonoBehaviour where TRenderer : Renderer {
        #region CREATE & SETUP RENDERER

        protected virtual Vector3 RdrNomLocScale => Vector3.one;

        private void CreateRenderer() {
            Rdr = UndoUtils.CreateGameObjectUndo(nameof(Renderer)).AddComponentUndo<TRenderer>();

            SetParentRdrThenUpdate(resetScale: true);
        }

        private void SetParentRdrThenUpdate(bool resetScale) {
            UndoUtils.RecordForUndo(Rdr.transform);

            Rdr.transform.SetParentUndo(transform);
            if (resetScale) Rdr.transform.localScale = Vector3.one;

            UndoUtils.MarkDirty(Rdr.transform);

            UpdateRdrPosFromPivot();
        }

        private bool IsNeedApplyRdrLocScaleToThis() =>
            Rdr != null
         && Rdr.transform.parent == transform
         && Rdr.transform.localScale != RdrNomLocScale;

        protected void TryApplyRdrLocScaleToThis() {
            if (IsNeedApplyRdrLocScaleToThis()) {
                UndoUtils.RecordForUndo(transform, Rdr.transform);
                
                transform.localScale     = Vector3.Scale(transform.localScale, Rdr.transform.localScale.Div(RdrNomLocScale));
                Rdr.transform.localScale = RdrNomLocScale;
                
                UndoUtils.MarkDirty(transform, Rdr.transform);
            }
        }

        private void Reset() {
            if (null == Rdr
             && null == (Rdr = transform.Find(nameof(Renderer))?.GetComponent<TRenderer>())) {
                CreateRenderer();
            }
        }

        #endregion

        #region PROPERTIES

        [Title("EXTENDED CONFIGS", titleAlignment: TitleAlignments.Centered)]
        [SerializeField, OnValueChanged(nameof(UpdateRdrPosFromPivot)), OnValueChanged(nameof(TryBlockRdrTransEditor))]
        private TRenderer rdr;

        public TRenderer Rdr {
            get => rdr;
            set {
                SetThisPropThenUpdate(ref rdr, value);
                TryBlockRdrTransEditor();
            }
        }

        [SerializeField, OnValueChanged(nameof(UpdateRdrPosFromPivot))]
        private PivotOverrideMode pivot;

        public PivotOverrideMode Pivot { get => pivot; set => SetThisPropThenUpdate(ref pivot, value); }

        [HideLabel, Indent, ShowIf(nameof(Pivot), PivotOverrideMode.Custom)]
        [SerializeField, OnValueChanged(nameof(UpdateRdrPosFromPivot))]
        private Vector2 customPivot = new(0.5f, 0.5f);

        public Vector2 CustomPivot { get => customPivot; set => SetThisPropThenUpdate(ref customPivot, value); }

        protected void SetThisPropThenUpdate<T>(ref T orgVal, T newVal) {
            if (EqualityComparer<T>.Default.Equals(orgVal, newVal)) return;

            UndoUtils.RecordForUndo(this);
            orgVal = newVal;
            UndoUtils.MarkDirty(this);

            UpdateRdrPosFromPivot();
        }

        #endregion

        #region RENDERER PROPERTIES

        protected abstract Sprite GetSprite();
        protected abstract void SetSprite(Sprite value);
        public Sprite Sprite { get => GetSprite(); set => SetRdrPropThenUpdate(GetSprite, SetSprite, value); }

        protected abstract bool GetFlipX();
        protected abstract void SetFlipX(bool value);
        public virtual bool FlipX { get => GetFlipX(); set => SetRdrPropThenUpdate(GetFlipX, SetFlipX, value); }

        protected abstract bool GetFlipY();
        protected abstract void SetFlipY(bool value);
        public virtual bool FlipY { get => GetFlipY(); set => SetRdrPropThenUpdate(GetFlipY, SetFlipY, value); }

        protected void SetRdrPropThenUpdate<T>(Func<T> getter, Action<T> setter, T newVal) {
            if (EqualityComparer<T>.Default.Equals(getter(), newVal)) return;

            UndoUtils.RecordForUndo(Rdr);
            setter(newVal);
            UndoUtils.MarkDirty(Rdr);

            UpdateRdrPosFromPivot();
        }

        #endregion

        #region TRACK RENDERER PROPS IN EDITOR

        #if UNITY_EDITOR

        private Vector2 prevPivot_Editor;
        private float prevPPU_Editor;

        private bool IsNeedUpdateRdrTrackingProps() {
            if (Rdr == null) return false;

            if (Rdr.transform.hasChanged) return true;
            if (Rdr.transform.localRotation != Quaternion.identity) return true;
            if (IsNeedApplyRdrLocScaleToThis()) return true;

            if (Sprite == null) return false;

            if (Sprite.pivot != prevPivot_Editor) return true;
            if (!Mathf.Approximately(Sprite.pixelsPerUnit, prevPPU_Editor)) return true;

            return false;
        }

        private void UpdateRdrTrackingProps() {
            if (Sprite != null) {
                prevPivot_Editor = Sprite.pivot;
                prevPPU_Editor   = Sprite.pixelsPerUnit;
            }
            
            Rdr.transform.hasChanged = false;
        }

        private void LateUpdate() {
            if (IsNeedUpdateRdrTrackingProps()) {
                UpdateRdrPosFromPivot();
                UpdateRdrTrackingProps();
            }
        }

        #endif

        #endregion

        private void OnEnable() {
            if (this.IsPlaying()) UpdateRdrPosFromPivot();
            else TryBlockRdrTransEditor();
        }

        [Button]
        public void UpdateRdrPosFromPivot() {
            if (Rdr == null) return;
            
            if (Rdr.transform.localRotation != Quaternion.identity) {
                UndoUtils.RecordForUndo(Rdr.transform);
                Rdr.transform.localRotation = Quaternion.identity;
                UndoUtils.MarkDirty(Rdr.transform);
            }

            TryApplyRdrLocScaleToThis();

            if (Sprite == null) return;

            if (Pivot == PivotOverrideMode.Unchanged) {
                SetRdrLocPos(Vector3.zero);
                return;
            }

            var locPos = Rdr.localBounds.size * (Sprite.pivot / Sprite.bounds.size / Sprite.pixelsPerUnit - Pivot switch {
                PivotOverrideMode.Center      => new(0.5f, 0.5f)
              , PivotOverrideMode.TopLeft     => new(0f, 1f)
              , PivotOverrideMode.Top         => new(0.5f, 1f)
              , PivotOverrideMode.TopRight    => new(1f, 1f)
              , PivotOverrideMode.Left        => new(0f, 0.5f)
              , PivotOverrideMode.Right       => new(1f, 0.5f)
              , PivotOverrideMode.BottomLeft  => new(0f, 0f)
              , PivotOverrideMode.Bottom      => new(0.5f, 0f)
              , PivotOverrideMode.BottomRight => new(1f, 0f)
              , PivotOverrideMode.Custom      => CustomPivot

              , _ => throw new ArgumentOutOfRangeException()
            });
            
            if (FlipX) locPos.x = -locPos.x;
            if (FlipY) locPos.y = -locPos.y;

            SetRdrLocPos(locPos);

            void SetRdrLocPos(Vector3 rdrLocPos) {
                if (Rdr.transform.localPosition == rdrLocPos) return;

                UndoUtils.RecordForUndo(Rdr.transform);
                Rdr.transform.localPosition = rdrLocPos;
                UndoUtils.MarkDirty(Rdr.transform);
            }
        }

        [Conditional("UNITY_EDITOR")]
        private void TryBlockRdrTransEditor() {
            #if UNITY_EDITOR

            if (Rdr != null) {
                Rdr.transform.hideFlags |= HideFlags.NotEditable;
                Rdr.hideFlags           |= HideFlags.HideInInspector;
            }

            #endif
        }
    }
}