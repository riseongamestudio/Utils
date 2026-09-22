using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace RiseOn.Utils.Renderers {
    /// <summary>
    /// Replaces Physics2DRaycaster (remove the built-in one when attaching this, or hits get duplicated).
    /// Sorts hits by actual render order via the same core as <see cref="Compare"/>. Where the engine
    /// leaves render order undefined, the comparison returns 0 — the data (SortingOrder) must disambiguate.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class RenderOrder2DRaycaster : BaseRaycaster {
        [SerializeField]
        private LayerMask eventMask = -1;

        private readonly List<Collider2D> hits    = new();
        private readonly List<Entry>      entries = new();

        private Camera cam;

        // All chains live in one shared buffer as (start, count) ranges — no per-call allocations.
        // Main thread only; Raycast builds its ranges up front and never calls the public Compare,
        // so the buffer is never cleared under an active range.
        private static readonly List<SortingGroup> chainBuffer = new();

        public override Camera eventCamera => cam != null ? cam : cam = GetComponent<Camera>();

        /// <summary>
        /// Render-order comparison: negative if a is drawn AFTER b, i.e. a shows on top and should
        /// receive interaction first. Walks the SortingGroup chains from the root and compares keys
        /// at the first diverging level.
        /// </summary>
        public static int Compare(Transform a, Transform b) {
            chainBuffer.Clear();

            var aCount = BuildChain(a);
            var bCount = BuildChain(b);

            return Compare(a, 0, aCount, b, aCount, bCount);
        }

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList) {
            var cam = eventCamera;

            if (cam == null || !cam.pixelRect.Contains(eventData.position)) return;

            var point = (Vector2)cam.ScreenToWorldPoint(eventData.position);

            // Match OverlapPointAll's default behavior: respect Physics2D.queriesHitTriggers.
            var filter = new ContactFilter2D { useTriggers = Physics2D.queriesHitTriggers };
            filter.SetLayerMask(eventMask);

            var hitCount = Physics2D.OverlapPoint(point, filter, hits);
            if (hitCount == 0) return;

            if (hitCount > 1) {
                entries.Clear();
                chainBuffer.Clear();

                // Build each chain once, then sort on the cached ranges.
                for (var i = 0; i < hitCount; ++i) {
                    var tf    = hits[i].transform;
                    var start = chainBuffer.Count;

                    entries.Add(new() {
                        Col   = hits[i]
                      , TF    = tf
                      , Start = start
                      , Count = BuildChain(tf)
                    });
                }

                SortEntries();

                for (var i = 0; i < hitCount; ++i) hits[i] = entries[i].Col;
            }

            var display = cam.targetDisplay;

            for (var i = 0; i < hitCount; ++i) {
                // Unassigned sort fields stay 0: EventSystem's RaycastComparer ties on every earlier
                // criterion and falls through to index, so the order produced here survives.
                resultAppendList.Add(new() {
                    gameObject     = hits[i].gameObject
                  , module         = this
                  , worldPosition  = point
                  , screenPosition = eventData.position
                  , displayIndex   = display
                  , index          = resultAppendList.Count
                });
            }
        }

        // A few hits per query: insertion sort avoids List.Sort's Comparison wrapper allocation.
        private void SortEntries() {
            for (var i = 1; i < entries.Count; ++i) {
                var cur = entries[i];
                var j   = i - 1;

                while (j >= 0 && Compare(entries[j].TF, entries[j].Start, entries[j].Count, cur.TF, cur.Start, cur.Count) > 0) {
                    entries[j + 1] = entries[j];
                    --j;
                }

                entries[j + 1] = cur;
            }
        }

        // Appends target's chain (root first) to chainBuffer, returns its length.
        private static int BuildChain(Transform target) {
            var start = chainBuffer.Count;

            for (var tf = target; tf != null; tf = tf.parent) {
                if (!tf.TryGetComponent<SortingGroup>(out var group) || !group.enabled) continue;

                chainBuffer.Add(group);

                // sortAtRoot makes the group sort against root-level renderers, so ancestors above it don't apply.
                if (group.sortAtRoot) break;
            }

            var count = chainBuffer.Count - start;

            chainBuffer.Reverse(start, count);

            return count;
        }

        private static int Compare(Transform aTF, int aStart, int aCount, Transform bTF, int bStart, int bCount) {
            var i = 0;

            while (true) {
                var ga = i < aCount ? chainBuffer[aStart + i] : null;
                var gb = i < bCount ? chainBuffer[bStart + i] : null;

                if (ga != null && ga == gb) {
                    ++i;
                    continue;
                }

                GetKey(ga, aTF, out var layerA, out var orderA, out var zA);
                GetKey(gb, bTF, out var layerB, out var orderB, out var zB);

                if (layerA != layerB) return layerB.CompareTo(layerA);
                if (orderA != orderB) return orderB.CompareTo(orderA);

                // Exact float compare on purpose: the comparer must stay transitive, Approximately breaks that.
                var byZ = zA.CompareTo(zB);
                if (byZ != 0) return byZ;

                // Containment wins before anything else in the undefined region: a container and the thing
                // held inside it tie on every render key, and interacting with the container must reach it.
                if (aTF != bTF) {
                    if (bTF.IsChildOf(aTF)) return 1;
                    if (aTF.IsChildOf(bTF)) return -1;
                }

                var hasA = aTF.TryGetComponent<SpriteRenderer>(out _);
                var hasB = bTF.TryGetComponent<SpriteRenderer>(out _);

                if (hasA != hasB) return hasA ? -1 : 1;

                // Full tie with no containment: the engine declares this order undefined — don't guess.
                return 0;
            }
        }

        // A GO without a SpriteRenderer counts as having a virtual one (Default, order 0);
        // a real renderer only beats a virtual one when every key ties.
        private static void GetKey(SortingGroup group, Transform target, out int layerValue, out int order, out float z) {
            if (group != null) {
                layerValue = SortingLayer.GetLayerValueFromID(group.sortingLayerID);
                order      = group.sortingOrder;
                z          = group.transform.position.z;
            } else if (target.TryGetComponent<SpriteRenderer>(out var renderer)) {
                layerValue = SortingLayer.GetLayerValueFromID(renderer.sortingLayerID);
                order      = renderer.sortingOrder;
                z          = renderer.transform.position.z;
            } else {
                layerValue = 0;
                order      = 0;
                z          = target.position.z;
            }
        }

        private struct Entry {
            public Collider2D Col;
            public Transform  TF;
            public int        Start;
            public int        Count;
        }
    }
}