using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RiseOn.Utils {
    public abstract class Singleton<T> : Singleton, ISingleton<T> where T : class, ISingleton<T> {
        public static T Ins => SingletonHub.Ins<T>();
        public static bool HasIns => SingletonHub.HasIns<T>();
        public static bool ExistIns => SingletonHub.ExistIns<T>();
    }

    public abstract class Singleton : MonoBehaviourExt {
        [InfoBox(
            "DontDestroyOnLoad only works on root GameObjects."
          , InfoMessageType.Warning,
            VisibleIf = "@isPersistent && transform.parent != null")]
        [SerializeField, FoldoutGroup("Singleton")]
        private bool isPersistent = true;

        [SerializeField, FoldoutGroup("Singleton")]
        private SingletonDestroyDuplicateTarget destroyDuplicateTarget = SingletonDestroyDuplicateTarget.GameObject;

        private protected Singleton() { }

        /// <summary>
        /// This method is not virtual. Override <see cref="OnAwake"/> instead.
        /// </summary>
        protected internal void Awake() {
            switch (SingletonHub.Register(this)) {
                case SingletonRegisterResult.Duplicate:
                    Destroy(destroyDuplicateTarget switch {
                        SingletonDestroyDuplicateTarget.GameObject => gameObject
                      , SingletonDestroyDuplicateTarget.Component  => this

                      , _ => throw new ArgumentOutOfRangeException()
                    });
                    break;

                case SingletonRegisterResult.Success:
                    if (isPersistent && Application.isPlaying) {
                        DontDestroyOnLoad(gameObject);
                    }

                    OnAwake();
                    break;
            }
        }

        protected virtual void OnAwake() { }

        protected virtual void OnDestroy() {
            SingletonHub.Unregister(this);
        }
    }
}