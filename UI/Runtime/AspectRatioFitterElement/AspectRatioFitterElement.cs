using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RiseOn.Utils.UI {
    [RequireComponent(typeof(RectTransform))]
    public class AspectRatioFitterElement : UIBehaviour, ILayoutElement {
        public enum AspectModeId {
            WidthControlsHeight
          , HeightControlsWidth
        }

        private const float MIN_ASPECT_RATIO = 1e-4f;

        [SerializeField] private AspectModeId aspectMode;
        public AspectModeId AspectMode { get => aspectMode; set => SetThisPropThenDirty(ref aspectMode, value); }

        [SerializeField, MinValue(MIN_ASPECT_RATIO)] private float aspectRatio = 1;
        public float AspectRatio { get => Mathf.Max(aspectRatio, MIN_ASPECT_RATIO); set => SetThisPropThenDirty(ref aspectRatio, Mathf.Max(value, MIN_ASPECT_RATIO)); }

        private RectTransform rectTF;
        protected RectTransform RectTF => rectTF != null ? rectTF : rectTF = GetComponent<RectTransform>();

        private float minWidth;
        float ILayoutElement.minWidth => minWidth;

        private float minHeight;
        float ILayoutElement.minHeight => minHeight;

        private DrivenRectTransformTracker tracker;

        float ILayoutElement.preferredWidth => -1;
        float ILayoutElement.flexibleWidth => -1;
        float ILayoutElement.preferredHeight => -1;
        float ILayoutElement.flexibleHeight => -1;
        int ILayoutElement.layoutPriority => 0;

        void ILayoutElement.CalculateLayoutInputHorizontal() {
            minWidth = AspectMode == AspectModeId.WidthControlsHeight ? 0 : RectTF.rect.height * AspectRatio;
        }

        void ILayoutElement.CalculateLayoutInputVertical() {
            minHeight = AspectMode == AspectModeId.HeightControlsWidth ? 0 : RectTF.rect.width / AspectRatio;
        }

        protected void SetThisPropThenDirty<T>(ref T orgVal, T newVal) {
            if (EqualityComparer<T>.Default.Equals(orgVal, newVal)) return;

            this.RecordForUndo();
            orgVal = newVal;
            this.MarkDirty();

            SetDirty();
        }

        protected override void Start() {
            base.Start();

            SetDirtyNextFrame().Forget();
        }

        /// <summary>
        /// The parent layout is not settled on the first frame, so the size is worked out once more on the next one.<br/>
        /// Canceled when the object is destroyed first; a canceled UniTaskVoid logs nothing.
        /// </summary>
        private async UniTaskVoid SetDirtyNextFrame() {
            await UniTask.DelayFrame(1, cancellationToken: destroyCancellationToken);
            SetDirty();
        }

        #if UNITY_EDITOR
        protected override void Reset() {
            base.Reset();

            if (transform.parent != null
             && transform.parent.TryGetComponent(out HorizontalOrVerticalLayoutGroup parGroup))
                AspectMode = parGroup is HorizontalLayoutGroup
                    ? AspectModeId.HeightControlsWidth
                    : AspectModeId.WidthControlsHeight;
        }
        #endif

        #region EVENT FUNCTIONS THAT CLONE FROM LayoutElement

        protected override void OnEnable() {
            base.OnEnable();
            SetDirty();
        }

        protected override void OnTransformParentChanged() {
            SetDirty();
            base.OnTransformParentChanged();
        }

        protected override void OnDisable() {
            SetDirty();
            base.OnDisable();
        }

        protected override void OnDidApplyAnimationProperties() {
            SetDirty();
        }

        protected override void OnBeforeTransformParentChanged() {
            SetDirty();
        }

        protected void SetDirty() {
            tracker.Clear();

            if (!IsActive()) return;

            LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
            tracker.Add(this, RectTF, AspectMode == AspectModeId.WidthControlsHeight
                ? DrivenTransformProperties.SizeDeltaY
                : DrivenTransformProperties.SizeDeltaX);
        }

        #if UNITY_EDITOR
        protected override void OnValidate() {
            SetDirty();
        }
        #endif

        #endregion
    }
}