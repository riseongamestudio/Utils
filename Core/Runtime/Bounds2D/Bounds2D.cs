using System;
using System.Globalization;
using UnityEngine;

namespace RiseOn.Utils {
    [Serializable]
    public struct Bounds2D : IEquatable<Bounds2D>, IFormattable {
        [field: SerializeField] public Vector2 center { get; private set; }
        [field: SerializeField] public Vector2 extents { get; private set; }

        public Vector2 size {
            get => extents * 2;
            set => extents = value * .5f;
        }

        public Vector2 min {
            get => center - extents;
            set => SetMinMax(value, max);
        }

        public Vector2 max {
            get => center + extents;
            set => SetMinMax(min, value);
        }

        public float xMin {
            get => min.x;
            set => SetMinMax(new Vector2(value, min.y), max);
        }

        public float yMin {
            get => min.y;
            set => SetMinMax(new Vector2(min.x, value), max);
        }

        public float xMax {
            get => max.x;
            set => SetMinMax(min, new Vector2(value, max.y));
        }

        public float yMax {
            get => max.y;
            set => SetMinMax(min, new Vector2(max.x, value));
        }

        public Bounds2D(Vector2 center, Vector2 size) {
            this.center  = center;
            this.extents = size * 0.5f;
        }

        public Bounds2D(Bounds bounds) {
            this.center  = bounds.center;
            this.extents = bounds.extents;
        }

        public Bounds2D(Bounds2D bounds, Bounds2D encapsulate) {
            bounds.Encapsulate(encapsulate);
            this = bounds;
        }

        public static Bounds2D FromMinMax(Vector2 min, Vector2 max) {
            var result = new Bounds2D();
            result.SetMinMax(min, max);
            return result;
        }

        public void SetMinMax(Vector2 min, Vector2 max) {
            extents = (max - min) * 0.5f;
            center  = min + extents;
        }

        public void Encapsulate(Vector2 point) {
            SetMinMax(Vector2.Min(min, point), Vector2.Max(max, point));
        }

        public void Encapsulate(Bounds2D bounds) {
            Encapsulate(bounds.center - bounds.extents);
            Encapsulate(bounds.center + bounds.extents);
        }

        public void Expand(float amount) {
            amount  *= .5f;
            extents += new Vector2(amount, amount);
        }

        public void Expand(Vector2 amount) {
            extents += amount * .5f;
        }
        
        public bool Intersects_X(Bounds2D bounds) {
            return 
                min.x <= bounds.max.x 
             && max.x >= bounds.min.x;
        }

        public bool Intersects_Y(Bounds2D bounds) {
            return 
                min.y <= bounds.max.y 
             && max.y >= bounds.min.y;
        }

        public bool Intersects(Bounds2D bounds) {
            return
                Intersects_X(bounds)
             && Intersects_Y(bounds);
        }

        public bool Contains(Vector2 point) {
            var min = this.min;
            var max = this.max;

            return
                point.x >= min.x
             && point.x <= max.x
             && point.y >= min.y
             && point.y <= max.y;
        }

        public bool Contains(Bounds2D bounds) {
            var min = this.min;
            var max = this.max;

            return
                bounds.min.x >= min.x
             && bounds.max.x <= max.x
             && bounds.min.y >= min.y
             && bounds.max.y <= max.y;
        }

        public Vector2 ClosestPoint(Vector2 point) {
            var t = point - center;
            return center + new Vector2(
                Mathf.Clamp(t.x, -extents.x, extents.x)
              , Mathf.Clamp(t.y, -extents.y, extents.y));
        }

        public float SqrDistance(Vector2 point) {
            var closest = ClosestPoint(point);
            var diff    = point - closest;
            return diff.sqrMagnitude;
        }

        public float Distance(Vector2 point) {
            var closest = ClosestPoint(point);
            var diff    = point - closest;
            return diff.magnitude;
        }

        public override int GetHashCode() {
            return center.GetHashCode() ^ (extents.GetHashCode() << 2);
        }

        public override bool Equals(object other) {
            return other is Bounds2D bounds && Equals(bounds);
        }

        public bool Equals(Bounds2D other) {
            return center.Equals(other.center) && extents.Equals(other.extents);
        }

        public static bool operator ==(Bounds2D lhs, Bounds2D rhs) {
            return lhs.center == rhs.center && lhs.extents == rhs.extents;
        }

        public static bool operator !=(Bounds2D lhs, Bounds2D rhs) {
            return !(lhs == rhs);
        }

        public override string ToString() {
            return ToString(null, null);
        }

        public string ToString(string format) {
            return ToString(format, null);
        }

        public string ToString(string format, IFormatProvider formatProvider) {
            if (string.IsNullOrEmpty(format)) format   = "F2";
            if (formatProvider == null) formatProvider = CultureInfo.InvariantCulture.NumberFormat;

            return string.Format("Center: {0}, Extents: {1}",
                center.ToString(format, formatProvider),
                extents.ToString(format, formatProvider));
        }

        public static implicit operator Bounds2D(Bounds bounds) {
            return new Bounds2D(bounds);
        }
        
        public static implicit operator Bounds(Bounds2D bounds2D) {
            return new Bounds(bounds2D.center, bounds2D.size);
        }

        public static Bounds2D Lerp(Bounds2D a, Bounds2D b, float t) {
            return new Bounds2D {
                center  = Vector2.Lerp(a.center, b.center, t)
              , extents = Vector2.Lerp(a.extents, b.extents, t)
            };
        }
        
        public static Bounds2D LerpUnclamped(Bounds2D a, Bounds2D b, float t) {
            return new Bounds2D {
                center  = Vector2.LerpUnclamped(a.center, b.center, t)
              , extents = Vector2.LerpUnclamped(a.extents, b.extents, t)
            };
        }
    }
}