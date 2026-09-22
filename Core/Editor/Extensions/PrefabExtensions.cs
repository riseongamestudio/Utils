using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RiseOn.Utils.Editor {
    public static class PrefabExtensions {
        public static GameObject GetRootPrefab(this Object obj) {
            if (obj is not GameObject go) {
                if (obj is not Component cpn) return null;
                go = cpn.gameObject;
            }

            if (PrefabStageUtility.GetPrefabStage(go) == null
             && !EditorUtility.IsPersistent(go)) {
                return null;
            }

            return go.transform.root.gameObject;
        }

        public static bool TryGetRootPrefab(this Object obj, out GameObject rootPrefab) {
            rootPrefab = obj.GetRootPrefab();
            return rootPrefab != null;
        }
    }
}
