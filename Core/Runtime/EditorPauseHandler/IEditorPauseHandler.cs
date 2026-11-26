using System;
using System.ComponentModel;
using UnityEngine;

namespace RiseOn.Utils {
    /// <summary>
    /// The game's pause in the Editor where a device stops Unity itself, such as under a full-screen ad or iOS's tracking prompt, which
    /// no SDK stops in the Editor.<br/>
    /// The packs call <see cref="Pause"/> as such a thing shows and <see cref="Resume"/> as it goes, nested as they may be. A game with a class implementing this
    /// interface pauses through it; one without has the time scale set to zero and the audio paused, both back as they were after. The
    /// pack's generator finds the game's class: one per project, neither abstract, generic nor nested, with a constructor without parameters.<br/>
    /// Its two instance members are for this interface alone: the class implements them explicitly, and no other code can call them.
    /// </summary>
    public interface IEditorPauseHandler {
        private static Func<IEditorPauseHandler> make;
        private static IEditorPauseHandler handler;
        private static int count;
        private static float timeScale = 1;
        private static bool audioPaused;
        private static bool warned;

        /// <summary>
        /// One more thing up that would stop the game on a device; the first pauses it.<br/>
        /// Outside the Editor, where the device stops the game itself, nothing but a warning, once per launch.<br/>
        /// Unity's thread only, each with a <see cref="Resume"/>.
        /// </summary>
        public static void Pause() {
            if (!Application.isEditor) {
                WarnOutsideEditor(nameof(Pause));
                return;
            }

            if (count++ > 0) return;

            handler ??= make?.Invoke();
            if (handler != null) {
                handler.OnPause();
                return;
            }

            timeScale           = Time.timeScale;
            audioPaused         = AudioListener.pause;
            Time.timeScale      = 0;
            AudioListener.pause = true;
        }

        /// <summary>One fewer such thing up; the last resumes the game. Outside the Editor, as <see cref="Pause"/>, nothing but the warning.</summary>
        public static void Resume() {
            if (!Application.isEditor) {
                WarnOutsideEditor(nameof(Resume));
                return;
            }

            if (count == 0 || --count > 0) return;

            if (handler != null) {
                handler.OnResume();
                return;
            }

            Time.timeScale      = timeScale;
            AudioListener.pause = audioPaused;
        }

        /// <summary>Pauses the game, as the first of the things that would stop it on a device shows.</summary>
        protected void OnPause();

        /// <summary>Undoes <see cref="OnPause"/>, once the last of them has gone.</summary>
        protected void OnResume();

        /// <summary>For the generator's code, which registers the game's class as the Editor starts playing.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Register(Func<IEditorPauseHandler> registered) {
            make    = registered;
            handler = null;
        }

        // The packs call it only in the Editor, so a call outside it is the game's, counting on a pause the device makes itself.
        private static void WarnOutsideEditor(string method) {
            if (warned) return;

            warned = true;
            Debug.LogWarning($"{nameof(IEditorPauseHandler)}.{method} does nothing outside the Editor, where the device pauses the game itself");
        }

        // Static state outlives a play session when the Editor skips the domain reload; the game's class registers again after this.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() {
            make        = null;
            handler     = null;
            count       = 0;
            timeScale   = 1;
            audioPaused = false;
        }
    }
}