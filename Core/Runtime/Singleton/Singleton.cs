using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace RiseOn.Utils {
    public abstract class Singleton<T> : Singleton, ISingleton<T> where T : class, ISingleton<T> {
        public static T Ins => SingletonHub.Ins<T>();
        public static bool HasIns => SingletonHub.HasIns<T>();
        public static bool ExistIns => SingletonHub.ExistIns<T>();
    }

    public abstract class Singleton : MonoBehaviour {
        /// <summary>What a second instance of the same singleton destroys when it wakes up.</summary>
        private enum DuplicateAction {
            DestroyGameObject
          , DestroyComponent
        }

        [InfoBox(
            "DontDestroyOnLoad only works on root GameObjects."
          , InfoMessageType.Warning,
            VisibleIf = "@isPersistent && transform.parent != null")]
        [SerializeField, FoldoutGroup("Singleton")]
        private bool isPersistent = true;

        [SerializeField, FoldoutGroup("Singleton"), FormerlySerializedAs("destroyDuplicateTarget")]
        private DuplicateAction onDuplicate = DuplicateAction.DestroyGameObject;

        private protected Singleton() { }

        /// <summary>
        /// This method is not virtual. Override <see cref="OnAwake"/> instead.
        /// </summary>
        protected internal void Awake() {
            switch (SingletonHub.Register(this)) {
                case SingletonHub.RegisterResult.Duplicate:
                    Destroy(onDuplicate switch {
                        DuplicateAction.DestroyGameObject => gameObject
                      , DuplicateAction.DestroyComponent  => this

                      , _ => throw new ArgumentOutOfRangeException()
                    });
                    break;

                case SingletonHub.RegisterResult.Registered:
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