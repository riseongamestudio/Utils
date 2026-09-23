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

        // A browser only lets the page read a response from a site that allows it (CORS), and the generate_204
        // endpoints do not. These do, so on WebGL any answer at all counts as internet.
        private static readonly Uri[] browserProbeUris = {
            new("https://www.cloudflare.com/cdn-cgi/trace")
          , new("https://1.1.1.1/cdn-cgi/trace")
          , new("https://timeapi.io/api/time/current/zone?timeZone=UTC")
        };

        /// <summary>
        /// Whether the device has a route out: wifi, ethernet or mobile data is connected. Instant, sends nothing.<br/>
        /// Reachable in the sense the OS uses: packets could leave the device, not that anything answers them, so a wifi that has lost its internet still counts.<br/>
        /// <see cref="HasNetworkAsync"/> checks that the internet really answers.<br/>
        /// Main thread only.
        /// </summary>
        public static bool IsNetworkReachable => Application.internetReachability is not NetworkReachability.NotReachable;

        /// <summary>
        /// Whether the internet really answers: probes a few endpoints in parallel and returns true as soon as one does, false when none does within <paramref name="timeoutSeconds"/>.<br/>
        /// Outside WebGL an answer only counts when it is 204, which also catches a captive portal (wifi that wants a login first).<br/>
        /// On WebGL the probes go through the browser to endpoints that allow it, and any answer counts; call it from the main thread there.<br/>
        /// Only a canceled <paramref name="cancellationToken"/> throws.
        /// </summary>
        public static Task<bool> HasNetworkAsync(
            float timeoutSeconds = DefaultTimeoutSeconds
          , CancellationToken cancellationToken = default) {
            if (float.IsNaN(timeoutSeconds) || float.IsInfinity(timeoutSeconds) || timeoutSeconds <= 0f) {
                throw new ArgumentOutOfRangeException(
                    nameof(timeoutSeconds),
                    timeoutSeconds,
                    "Timeout must be a positive finite value.");
            }

            return RaceProbes(Application.platform is RuntimePlatform.WebGLPlayer, timeoutSeconds, cancellationToken);

            static async Task<bool> RaceProbes(bool inBrowser, float timeoutSeconds, CancellationToken cancellationToken) {
                using (var probeCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)) {
                    // WebGL has no threads: there the awaits stay on Unity's context, and since CancelAfter has nothing to
                    // fire on, each browser probe keeps its own time limit instead.
                    if (!inBrowser) probeCancellation.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                    var uris   = inBrowser ? browserProbeUris : probeUris;
                    var probes = new List<Task<bool>>(uris.Length);
                    foreach (var uri in uris) {
                        probes.Add(inBrowser
                            ? ProbeInBrowser(uri, timeoutSeconds, probeCancellation.Token)
                            : Probe(uri, probeCancellation.Token));
                    }

                    while (probes.Count > 0) {
                        var finishedProbe = await Task.WhenAny(probes).ConfigureAwait(inBrowser);
                        probes.Remove(finishedProbe);
                        if (!finishedProbe.Result) continue;

                        probeCancellation.Cancel();
                        return true;
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
                return false;
            }

            static async Task<bool> Probe(Uri uri, CancellationToken cancellationToken) {
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

            static async Task<bool> ProbeInBrowser(Uri uri, float timeoutSeconds, CancellationToken cancellationToken) {
                try {
                    await BrowserRequest.GetText(uri, timeoutSeconds, cancellationToken);
                    return true;
                } catch (Exception) {
                    return false;
                }
            }
        }
    }
}
