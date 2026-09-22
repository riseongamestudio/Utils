using System.Collections;
using UnityEngine;

namespace RiseOn.Utils.Renderers {
    public static class ParticleSystemExtensions {
        public static void EmitAt(
            this ParticleSystem ps
          , Vector3 pos
          , int amount = 1
          , bool applyShape = false) {
            var emitParams = new ParticleSystem.EmitParams {
                position             = pos
              , applyShapeToPosition = applyShape
            };

            ps.Emit(emitParams, amount);
        }

        public static void BurstAt(
            this ParticleSystem ps
          , MonoBehaviour runner
          , Vector3 pos
          , int amount = 1
          , float interval = 0
          , bool applyShape = false) {
            if (interval > 0) runner.StartCoroutine(IEBurstAt(ps, pos, amount, interval, applyShape));
            else ps.EmitAt(pos, amount, applyShape);
        }

        private static IEnumerator IEBurstAt(
            ParticleSystem ps
          , Vector3 pos
          , int amount
          , float interval
          , bool applyShape) {
            while (amount-- > 0) {
                ps.EmitAt(pos, 1, applyShape);

                if (amount > 0)
                    yield return WaitForSecondCache.Get(interval);
            }
        }

        public static void BurstAllAt(
            this ParticleSystem ps
          , MonoBehaviour runner
          , Vector3 pos
          , bool applyShape = false) {
            var emission = ps.emission;
            var bursts   = new ParticleSystem.Burst[emission.burstCount];
            emission.GetBursts(bursts);

            foreach (var burst in bursts)
                runner.StartCoroutine(IEBurstSingleAt(ps, pos, burst, applyShape, burst.time));
        }

        private static IEnumerator IEBurstSingleAt(
            ParticleSystem ps
          , Vector3 pos
          , ParticleSystem.Burst burst
          , bool applyShape
          , float delay) {
            if (delay > 0) yield return WaitForSecondCache.Get(delay);

            int cycles = Mathf.Max(1, burst.cycleCount);
            while (cycles-- > 0) {
                if (Random.value <= burst.probability)
                    ps.EmitAt(pos, (int)burst.count.Evaluate(Random.value, Random.value), applyShape);

                if (cycles > 0)
                    yield return WaitForSecondCache.Get(burst.repeatInterval);
            }
        }
    }
}
