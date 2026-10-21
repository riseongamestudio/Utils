using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RiseOn.Utils {
    /// <summary>
    /// The Debug.Log family as extensions on the value to return, so a guard clause fits on one line:<br/>
    /// <c>if (isOpen) return UniTask.CompletedTask.WithLogWarning("Already open.", this);</c><br/>
    /// Each one logs exactly as its Debug counterpart does, assertions included, then hands back the value.<br/>
    /// Hidden from the Console callstack when Strip logging callstack is on, so the log points at the caller.
    /// </summary>
    public static class LogUtils {
        [HideInCallstack]
        public static T WithLog<T>(this T result, object message, Object context = null) {
            Debug.Log(message, context);
            return result;
        }

        [HideInCallstack]
        public static T WithLogWarning<T>(this T result, object message, Object context = null) {
            Debug.LogWarning(message, context);
            return result;
        }

        [HideInCallstack]
        public static T WithLogError<T>(this T result, object message, Object context = null) {
            Debug.LogError(message, context);
            return result;
        }

        [HideInCallstack]
        public static T WithLogAssertion<T>(this T result, object message, Object context = null) {
            Debug.LogAssertion(message, context);
            return result;
        }

        [HideInCallstack]
        public static T WithLogException<T>(this T result, Exception exception, Object context = null) {
            Debug.LogException(exception, context);
            return result;
        }

        [HideInCallstack]
        public static T WithLogFormat<T>(this T result, string format, params object[] args) {
            Debug.LogFormat(format, args);
            return result;
        }

        [HideInCallstack]
        public static T WithLogFormat<T>(this T result, Object context, string format, params object[] args) {
            Debug.LogFormat(context, format, args);
            return result;
        }

        [HideInCallstack]
        public static T WithLogWarningFormat<T>(this T result, string format, params object[] args) {
            Debug.LogWarningFormat(format, args);
            return result;
        }

        [HideInCallstack]
        public static T WithLogWarningFormat<T>(this T result, Object context, string format, params object[] args) {
            Debug.LogWarningFormat(context, format, args);
            return result;
        }

        [HideInCallstack]
        public static T WithLogErrorFormat<T>(this T result, string format, params object[] args) {
            Debug.LogErrorFormat(format, args);
            return result;
        }

        [HideInCallstack]
        public static T WithLogErrorFormat<T>(this T result, Object context, string format, params object[] args) {
            Debug.LogErrorFormat(context, format, args);
            return result;
        }

        [HideInCallstack]
        public static T WithLogAssertionFormat<T>(this T result, string format, params object[] args) {
            Debug.LogAssertionFormat(format, args);
            return result;
        }

        [HideInCallstack]
        public static T WithLogAssertionFormat<T>(this T result, Object context, string format, params object[] args) {
            Debug.LogAssertionFormat(context, format, args);
            return result;
        }
    }
}
