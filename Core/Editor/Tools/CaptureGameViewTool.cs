using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RiseOn.Utils.Editor {
    public static class CaptureGameViewTool {
        #if UNITY_EDITOR_WIN

        [MenuItem("Tools/RiseOn/Capture Game View &#c")]
        public static void CaptureGameView(MenuCommand cmd) {
            string storePath = EditorUtility.SaveFilePanel(
                title: "Save Unity Screenshot"
              , directory: Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                  , "Downloads")
              , defaultName: $"GameView-{Random.Range((int)1e5, (int)1e6)}"
              , extension: "png"
            );

            if (string.IsNullOrEmpty(storePath)) return;

            ScreenCapture.CaptureScreenshot(storePath);
        }

        #endif
    }
}