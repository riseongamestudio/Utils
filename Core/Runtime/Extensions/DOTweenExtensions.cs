using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Core.Easing;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace RiseOn.Utils {
    public static class DOTweenExtensions {
        public static Sequence DOShakePositionDynamic(
            this Transform target,
            float duration,
            Vector3[] strengths,
            int vibrato = 10,
            float randomness = 90f,
            bool snapping = false,
            bool fadeOut = false) {
            if (strengths.Length == 0) return null;

            float durationUnit = duration / strengths.Length;

            var seq = DOTween.Sequence(target);

            foreach (var strength in strengths)
                seq.Append(target.DOShakePosition(
                    duration: durationUnit
                  , strength: strength
                  , vibrato: vibrato
                  , randomness: randomness
                  , snapping: snapping
                  , fadeOut: fadeOut));

            return seq;
        }

        /// <summary>
        /// Ignores global rotation and scale.<br/>
        /// See <see cref="DOLocalMovePure"/>
        /// </summary>
        public static Sequence DOLocalJumpPure(
            this Transform target
          , Vector3 endPnt
          , float jumpHeight
          , float duration
          , TweenCallback onReachTop = null
          , Ease moveEase = Ease.Linear) {
            var orgY = target.position.y;
            if (target.parent != null)
                orgY -= target.parent.position.y;
            var topY = Mathf.Max(orgY, endPnt.y) + jumpHeight;

            var upDis = topY - orgY;
            var upTime = Mathf.Approximately(upDis, 0)
                ? 0
                : duration / (Mathf.Sqrt((topY - endPnt.y) / upDis) + 1); // Base on real physic formula
            var downTime = duration - upTime;

            var seq = DOTween.Sequence(target);

            seq.Insert(0, target.DOLocalMovePure(endPnt.x, duration, VecAxis.X).SetEase(moveEase));
            seq.Insert(0, target.DOLocalMovePure(endPnt.z, duration, VecAxis.Z).SetEase(moveEase));
            seq.Insert(0, target.DOLocalMovePure(topY, upTime, VecAxis.Y).SetEase(Ease.OutQuad));
            if (onReachTop != null) seq.InsertCallback(upTime, onReachTop);
            seq.Insert(upTime, target.DOLocalMovePure(endPnt.y, downTime, VecAxis.Y).SetEase(Ease.InQuad));

            return seq;
        }

        /// <summary>
        /// Similar to <see cref="DG.Tweening.ShortcutExtensions.DOLocalMove"/>,
        /// but ignores global rotation and scale.
        /// </summary>
        private static TweenerCore<float, float, FloatOptions> DOLocalMovePure(
            this Transform target
          , float endValue
          , float duration
          , VecAxis axis
          , bool snapping = false) {
            var t = DOTween.To(
                getter: () => target.position.Get(axis) - target.parent.position.Get(axis)
              , setter: vl => target.SetPosition(axis, target.parent.position.Get(axis) + vl)
              , endValue, duration);
            t.SetOptions(snapping).SetTarget(target);
            return t;
        }

        public static Sequence DOJump_BetterHeight(
            this Transform target
          , Vector3 endPnt
          , float jumpHeight
          , float duration
          , TweenCallback onReachTop = null
          , Ease moveEase = Ease.Linear) {
            var orgY = target.position.y;
            var topY = Mathf.Max(orgY, endPnt.y) + jumpHeight;

            var upDis = topY - orgY;
            var upTime = Mathf.Approximately(upDis, 0)
                ? 0
                : duration / (Mathf.Sqrt((topY - endPnt.y) / upDis) + 1); // Base on real physic formula
            var downTime = duration - upTime;

            var seq = DOTween.Sequence(target);

            seq.Insert(0, target.DOMoveX(endPnt.x, duration).SetEase(moveEase));
            seq.Insert(0, target.DOMoveZ(endPnt.z, duration).SetEase(moveEase));
            seq.Insert(0, target.DOMoveY(topY, upTime).SetEase(Ease.OutQuad));
            if (onReachTop != null) seq.InsertCallback(upTime, onReachTop);
            seq.Insert(upTime, target.DOMoveY(endPnt.y, downTime).SetEase(Ease.InQuad));

            return seq;
        }

        public static float Evaluate(this Ease ease, float time, float duration = 1) => EaseManager.Evaluate(
            easeType: ease
          , customEase: null
          , time: time
          , duration: duration
          , overshootOrAmplitude: DOTween.defaultEaseOvershootOrAmplitude
          , period: DOTween.defaultEasePeriod);
    }
}
