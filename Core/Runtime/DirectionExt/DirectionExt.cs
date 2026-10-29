using System;
using UnityEngine;

namespace RiseOn.Utils {
    [Serializable]
    public struct DirectionExt {
        [SerializeField]
        internal Direction direction;

        [Tooltip("Degrees counterclockwise from the right.")]
        [SerializeField]
        internal float customAngle;

        internal bool IsCustom => direction is Direction.Custom;

        public DirectionExt(Direction direction) {
            this.direction = direction;

            // Switching to Custom later starts from the preset that was picked.
            customAngle = direction switch {
                Direction.Up => 90f,
                Direction.Left => 180f,
                Direction.Down => 270f,
                _ => 0f
            };
        }

        /// <summary>
        /// The direction as a vector of length 1.
        /// </summary>
        public Vector2 ToVector2() {
            return direction switch {
                Direction.Left => Vector2.left,
                Direction.Right => Vector2.right,
                Direction.Up => Vector2.up,
                Direction.Down => Vector2.down,
                Direction.Custom => FromAngle(customAngle),
                _ => throw new ArgumentOutOfRangeException()
            };

            static Vector2 FromAngle(float degrees) {
                var radians = degrees * Mathf.Deg2Rad;
                return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            }
        }
    }
}