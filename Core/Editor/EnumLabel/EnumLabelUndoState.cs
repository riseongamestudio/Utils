using UnityEngine;

namespace RiseOn.Utils.Editor {
    internal sealed class EnumLabelUndoState : ScriptableObject {
        [SerializeField]
        private string enumTypeName;

        public string EnumTypeName {
            get => enumTypeName;
            set => enumTypeName = value;
        }
    }
}