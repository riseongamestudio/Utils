using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RiseOn.Utils {
    public static class CollectionUtils {
        #region Push | Pop | Peek

        public static void PushBack<T>(this IList<T> list, T item) {
            list.Add(item);
        }

        /// <summary>Shifts every item one place, so it costs O(n) on a List.</summary>
        public static void PushFront<T>(this IList<T> list, T item) {
            list.Insert(0, item);
        }

        /// <summary>Removes and returns the last item; throws when the list is empty.</summary>
        public static T PopBack<T>(this IList<T> list) {
            var last = list[^1];
            list.RemoveAt(list.Count - 1);
            return last;
        }

        /// <summary>
        /// Removes and returns the first item; throws when the list is empty.<br/>
        /// Shifts every other item one place, so it costs O(n) on a List.
        /// </summary>
        public static T PopFront<T>(this IList<T> list) {
            var first = list[0];
            list.RemoveAt(0);
            return first;
        }

        /// <summary>
        /// The last item, left in place; throws when there is none.<br/>
        /// Read-only, so it takes any sequence: a list reads it directly, a plain sequence is walked to its end.
        /// </summary>
        public static T PeekBack<T>(this IEnumerable<T> source) {
            return source.TryPeekBack(out var item) ? item : throw new InvalidOperationException("The sequence is empty.");
        }

        /// <summary>
        /// The first item, left in place; throws when there is none.<br/>
        /// Read-only, so it takes any sequence: a list reads it directly, a plain sequence stops at its first item.
        /// </summary>
        public static T PeekFront<T>(this IEnumerable<T> source) {
            return source.TryPeekFront(out var item) ? item : throw new InvalidOperationException("The sequence is empty.");
        }

        public static bool TryPopBack<T>(this IList<T> list, out T item) {
            if (list.Count == 0) {
                item = default;
                return false;
            }

            item = list.PopBack();
            return true;
        }

        public static bool TryPopFront<T>(this IList<T> list, out T item) {
            if (list.Count == 0) {
                item = default;
                return false;
            }

            item = list.PopFront();
            return true;
        }

        /// <summary>
        /// One overload for IList and IReadOnlyList alike, since neither derives from the other and separate overloads would be ambiguous on List and arrays.<br/>
        /// A list is read in O(1); a plain sequence is walked to its end.
        /// </summary>
        public static bool TryPeekBack<T>(this IEnumerable<T> source, out T item) {
            switch (source) {
                case IList<T> list when list.Count > 0:
                    item = list[^1];
                    return true;
                case IReadOnlyList<T> list when list.Count > 0:
                    item = list[^1];
                    return true;
                case IList<T>:
                case IReadOnlyList<T>:
                    item = default;
                    return false;
            }

            using var enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext()) {
                item = default;
                return false;
            }

            do item = enumerator.Current;
            while (enumerator.MoveNext());

            return true;
        }

        /// <summary>
        /// One overload for IList and IReadOnlyList alike, as <see cref="TryPeekBack{T}"/>.<br/>
        /// A list is read in O(1); a plain sequence stops at its first item.
        /// </summary>
        public static bool TryPeekFront<T>(this IEnumerable<T> source, out T item) {
            switch (source) {
                case IList<T> list when list.Count > 0:
                    item = list[0];
                    return true;
                case IReadOnlyList<T> list when list.Count > 0:
                    item = list[0];
                    return true;
                case IList<T>:
                case IReadOnlyList<T>:
                    item = default;
                    return false;
            }

            using var enumerator = source.GetEnumerator();
            var found = enumerator.MoveNext();
            item = found ? enumerator.Current : default;
            return found;
        }

        #endregion

        public static ref T ElementAt<T>(this T[,] list, Vector2Int id) {
            return ref list[id.x, id.y];
        }
        
        /// <summary>
        /// True when there are no items.<br/>
        /// One overload for every kind of collection, since ICollection&lt;T&gt; does not derive from IReadOnlyCollection&lt;T&gt; and an overload for each would be ambiguous on List.<br/>
        /// Reads Count whenever there is one; only a plain sequence gets enumerated, and only up to its first item.
        /// </summary>
        public static bool IsEmpty<T>(this IEnumerable<T> source) {
            switch (source) {
                case ICollection<T> collection:         return collection.Count == 0;
                case IReadOnlyCollection<T> collection: return collection.Count == 0;
                case ICollection collection:            return collection.Count == 0;
            }

            using var enumerator = source.GetEnumerator();
            return !enumerator.MoveNext();
        }

        /// <summary>Every index (x, y) of the array, in the order of <see cref="IEIndex2D(int, int, int, int)"/>.</summary>
        public static Index2DEnumerable IEIndex2D<T>(this T[,] list) {
            return IEIndex2D(
                0, list.GetLength(0)
              , 0, list.GetLength(1));
        }

        /// <summary>
        /// Every index (x, y) of the rectangle: all of y for the first x, then the next x.<br/>
        /// A struct, so a foreach over it allocates nothing; used as an IEnumerable it gets boxed.
        /// </summary>
        public static Index2DEnumerable IEIndex2D(int xStart, int xLen, int yStart, int yLen) {
            return new Index2DEnumerable(xStart, xLen, yStart, yLen);
        }

        public readonly struct Index2DEnumerable : IEnumerable<Vector2Int> {
            private readonly int xStart;
            private readonly int xEnd;
            private readonly int yStart;
            private readonly int yEnd;

            public Index2DEnumerable(int xStart, int xLen, int yStart, int yLen) {
                this.xStart = xStart;
                this.yStart = yStart;
                xEnd        = xStart + xLen;
                yEnd        = yStart + yLen;
            }

            public Enumerator GetEnumerator() => new(xStart, xEnd, yStart, yEnd);

            IEnumerator<Vector2Int> IEnumerable<Vector2Int>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public struct Enumerator : IEnumerator<Vector2Int> {
                private readonly int xStart;
                private readonly int xEnd;
                private readonly int yStart;
                private readonly int yEnd;
                private          int x;
                private          int y;

                internal Enumerator(int xStart, int xEnd, int yStart, int yEnd) {
                    this.xStart = xStart;
                    this.xEnd   = xEnd;
                    this.yStart = yStart;
                    this.yEnd   = yEnd;
                    x           = xStart;
                    y           = yStart - 1;
                }

                public Vector2Int Current => new(x, y);

                object IEnumerator.Current => Current;

                public bool MoveNext() {
                    // With no y at all, stepping on would still walk through x.
                    if (yEnd <= yStart) return false;

                    if (++y >= yEnd) {
                        y = yStart;
                        x++;
                    }

                    return x < xEnd;
                }

                public void Reset() {
                    x = xStart;
                    y = yStart - 1;
                }

                public void Dispose() { }
            }
        }
    }
}
