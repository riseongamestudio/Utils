using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace RiseOn.Utils.Network {
    /// <summary>
    /// Requests for WebGL, where <see cref="HttpClient"/> cannot run: <see cref="UnityWebRequest"/> goes through the browser's own fetch.<br/>
    /// Kept apart from <see cref="NetworkHttp"/> so a WebGL build never creates its <see cref="HttpClient"/>.
    /// </summary>
    internal static class BrowserRequest {
        /// <summary>
        /// Plain GET, returning the body as text.<br/>
        /// Polled once per frame instead of waited on with timers, since WebGL has no threads for <see cref="CancellationTokenSource.CancelAfter(int)"/> or <see cref="Task.Delay(int)"/> to fire on.<br/>
        /// No custom headers: those make the browser send a preflight first, which most public endpoints refuse.<br/>
        /// Throws <see cref="TimeoutException"/> past <paramref name="timeoutSeconds"/> and <see cref="OperationCanceledException"/> when canceled, aborting the request either way.<br/>
        /// Main thread only.
        /// </summary>
        internal static async Task<string> GetText(Uri uri, float timeoutSeconds, CancellationToken cancellationToken) {
            var stopwatch = Stopwatch.StartNew();

            using (var request = UnityWebRequest.Get(uri)) {
                var operation = request.SendWebRequest();

                while (!operation.isDone) {
                    if (cancellationToken.IsCancellationRequested) {
                        request.Abort();
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    if (stopwatch.Elapsed.TotalSeconds >= timeoutSeconds) {
                        request.Abort();
                        throw new TimeoutException($"Request to '{uri}' timed out after {timeoutSeconds * 1000f:0}ms.");
                    }

                    await Task.Yield();
                }

                if (request.result is not UnityWebRequest.Result.Success) {
                    throw new HttpRequestException($"Request to '{uri}' failed: {request.error}");
                }

                return request.downloadHandler.text;
            }
        }
    }
}
