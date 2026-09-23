using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace RiseOn.Utils {
    internal static class SingletonHub {
        public enum RegisterResult {
            Registered
          , AlreadyRegistered
          , Duplicate
        }

        private static readonly Dictionary<Type, Singleton>   insDict  = new();
        private static readonly Dictionary<Singleton, Type[]> typeDict = new();
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() {
            insDict.Clear();
            typeDict.Clear();
        }

        /// <summary>
        /// Blocks cases such as <c>class A : Singleton&lt;B&gt; { }</c>,<br/>
        /// where A is registered as B but is not assignable to B.
        /// </summary>
        private static void ThrowIfInsNotAssignableToInsType(Singleton ins, Type insType) {
            if (!insType.IsInstanceOfType(ins)) {
                var errMes = $"{ins.GetType().FullName} implements {typeof(ISingleton<>).Name[..^2]}<{insType.FullName}> but is not assignable to {insType.FullName}.";
                Debug.LogError(errMes);
                throw new InvalidOperationException(errMes);
            }
        }

        private static bool TryGetCachedIns<T>(out T result) where T : class, ISingleton<T> {
            result = null;
            
            if (!insDict.TryGetValue(typeof(T), out var cachedIns)) {
                return false;
            }

            if (cachedIns == null) {
                Unregister(cachedIns);
                return false;
            }
            
            ThrowIfInsNotAssignableToInsType(cachedIns, typeof(T));

            result = (T)(object)cachedIns;
            
            return true;
        }

        public static T Ins<T>() where T : class, ISingleton<T> {
            if (TryGetCachedIns<T>(out var cachedIns)) return cachedIns;

            foreach (var ins in Object.FindObjectsByType<Singleton>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                if (ins is not T) continue;
                ins.Awake();
                if (TryGetCachedIns<T>(out var newIns)) return newIns;
            }

            return null;
        }

        public static bool HasIns<T>() where T : class, ISingleton<T> {
            return TryGetCachedIns(out T _);
        }

        public static bool ExistIns<T>() where T : class, ISingleton<T> {
            return Ins<T>() != null;
        }

        public static RegisterResult Register(Singleton ins) {
            if (typeDict.ContainsKey(ins)) return RegisterResult.AlreadyRegistered;

            var insTypes = ListPool<Type>.Get();
            try {
                foreach (var itf in ins.GetType().GetInterfaces()) {
                    if (!itf.IsGenericType
                     || itf.GetGenericTypeDefinition() != typeof(ISingleton<>)) {
                        continue;
                    }

                    var insType = itf.GetGenericArguments()[0];

                    ThrowIfInsNotAssignableToInsType(ins, insType);

                    if (insDict.TryGetValue(insType, out var oldIns)) {
                        if (oldIns != null) return RegisterResult.Duplicate;

                        Unregister(oldIns);
                    }

                    insTypes.Add(insType);
                }

                typeDict.Add(ins, insTypes.ToArray());
                foreach (var type in insTypes) insDict.Add(type, ins);

                return RegisterResult.Registered;
            } finally {
                ListPool<Type>.Release(insTypes);
            }
        }

        public static void Unregister(Singleton ins) {
            if (!typeDict.Remove(ins, out var types)) return;
            foreach (var type in types) insDict.Remove(type);
        }
    }
}