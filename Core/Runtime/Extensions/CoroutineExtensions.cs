using System;
using System.Collections;
using UnityEngine;

namespace RiseOn.Utils {
    public static class CoroutineExtensions {
        public static Coroutine DelayedCall_Second(this MonoBehaviour runner, float delay, Action callback) {
            return runner.StartCoroutine(IEDelayedCall_Second(delay, callback));
        }

        private static IEnumerator IEDelayedCall_Second(float delay, Action callback) {
            yield return WaitForSecondCache.Get(delay);

            callback?.Invoke();
        }

        public static Coroutine DelayedCall_Cond(this MonoBehaviour runner, Func<bool> delay, Action callback) {
            return runner.StartCoroutine(IEDelayedCall_Cond(delay, callback));
        }

        private static IEnumerator IEDelayedCall_Cond(Func<bool> delay, Action callback) {
            yield return new WaitUntil(delay);

            callback?.Invoke();
        }

        public static Coroutine DelayedCall_Frame(this MonoBehaviour runner, int delay, Action callback) {
            return runner.StartCoroutine(IEDelayedCall_Frame(delay, callback));
        }

        private static IEnumerator IEDelayedCall_Frame(int delay, Action callback) {
            while (delay-- > 0) yield return null;

            callback?.Invoke();
        }

        public static Coroutine WaitForSeconds(this MonoBehaviour runner, float seconds, Action<float> onProgressChanged) {
            return runner.StartCoroutine(IEWaitForSeconds(seconds, onProgressChanged));
        }

        private static IEnumerator IEWaitForSeconds(float seconds, Action<float> onProgressChanged) {
            float elapsedTime = 0;

            do {
                onProgressChanged?.Invoke(elapsedTime / seconds);
                yield return null;
            } while ((elapsedTime += Time.deltaTime) < seconds);

            onProgressChanged?.Invoke(1);
        }
    }
}
