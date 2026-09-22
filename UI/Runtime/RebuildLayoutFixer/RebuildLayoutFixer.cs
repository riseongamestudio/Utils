using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace RiseOn.Utils.UI {
    [RequireComponent(typeof(RectTransform))]
    public class RebuildLayoutFixer : MonoBehaviour {
        private void Update() {
            Fix();
            enabled = false;
        }

        [Button]
        public void Fix() {
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }
    }
}