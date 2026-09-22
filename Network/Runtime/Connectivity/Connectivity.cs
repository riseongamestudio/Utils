using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace RiseOn.Utils.Network {
    /// <summary>
    /// Whether the device can actually reach the internet, not just whether it has a network interface.
    /// </summary>
    public static class Connectivity {
        private const float DefaultTimeoutSeconds = 2f;

        // These endpoints answer 204 with an empty body. A captive portal (wifi that wants a login first) answers with
        // its own page instead, so anything but 204 counts as no internet.
        private static readonly Uri[] probeUris = {
            new("https://connectivitycheck.gstatic.com/generate_204")
          , new("https://www.google.com/generate_204")
          , new("https://cp.cloudflare.com/generate_204")
        };

        /// <summary>
        /// Instant hint without any request. False means the device has no network interface up at all; true does not
        /// mean the internet is reachable. Main thread only.
        /// </summary>
        public static bool HasNetwork => Application.internetReachability is not NetworkReachability.NotReachable;

        /// <summary>
        /// Probes a few endpoints in parallel and returns true as soon as one answers 204, false when none does within
        /// <paramref name="timeoutSeconds"/>. Only a cancelled <paramref name="cancellationToken"/> throws.
        /// Not supported on WebGL.
        /// </summary>
        public static Task<bool> HasInternetAsync(
            float timeoutSeconds = DefaultTimeoutSeconds
          , CancellationToken cancellationToken = default) {
            if (float.IsNaN(timeoutSeconds) || float.IsInfinity(timeoutSeconds) || timeoutSeconds <= 0f) {
                throw new ArgumentOutOfRangeException(
                    nameof(timeoutSeconds),
                    timeoutSeconds,
                    "Timeout must be a positive finite value.");
            }
#if UNITY_WEBGL && !UNITY_EDITOR
            return Task.FromException<bool>(new PlatformNotSupportedException(
                $"{nameof(Connectivity)} cannot read cross-origin responses on WebGL."));
#else
            return HasInternetAsyncCore(timeoutSeconds, cancellationToken);
#endif
        }

        private static async Task<bool> HasInternetAsyncCore(float timeoutSeconds, CancellationToken cancellationToken) {
            using (var probeCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)) {
                probeCancellation.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                var probes = new List<Task<bool>>(probeUris.Length);
                foreach (var probeUri in probeUris) probes.Add(Probe(probeUri, probeCancellation.Token));

                while (probes.Count > 0) {
                    var finishedProbe = await Task.WhenAny(probes).ConfigureAwait(false);
                    probes.Remove(finishedProbe);
                    if (!finishedProbe.Result) continue;

                    probeCancellation.Cancel();
                    return true;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            return false;
        }

        private static async Task<bool> Probe(Uri uri, CancellationToken cancellationToken) {
            try {
                using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                using (var response = await NetworkHttp.Client.SendAsync(
                           request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false)) {
                    return response.StatusCode is HttpStatusCode.NoContent;
                }
            } catch (Exception) {
                return false;
            }
        }
    }
}
