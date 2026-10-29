using System;
using LitMotion;
using UnityEngine;

namespace RiseOn.Utils.LitMotion {
    /// <summary>
    /// An <see cref="Ease"/> of LitMotion, or a curve drawn by hand when it is <see cref="Ease.CustomAnimationCurve"/>.
    /// </summary>
    [Serializable]
    public struct EaseExt {
        [SerializeField]
        internal Ease ease;

        [SerializeField]
        internal AnimationCurve curve;

        internal bool IsCustom => ease is Ease.CustomAnimationCurve;

        public EaseExt(Ease ease) {
            this.ease = ease;
            curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }

        public float Evaluate(float t) {
            return IsCustom ? curve.Evaluate(t) : EaseUtility.Evaluate(t, ease);
        }
    }
}