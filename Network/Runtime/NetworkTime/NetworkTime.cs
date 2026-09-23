using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RiseOn.Utils.Network {
    /// <summary>
    /// Current time from the network, independent of the device clock.<br/>
    /// Asks NTP servers first, then falls back to the Date header of a few HTTPS endpoints, and retries until the time budget runs out.
    /// </summary>
    public static class NetworkTime {
        private const float DefaultTimeoutSeconds          = 2f;
        private const int   NtpPort                        = 123;
        private const int   NtpPacketSize                  = 48;
        private const int   NtpAttemptTimeoutMilliseconds  = 350;
        private const int   HttpAttemptTimeoutMilliseconds = 600;
        private const int   RetryDelayMilliseconds         = 50;
        private const long  NtpUnixEpochOffsetSeconds      = 2208988800L;

        private static readonly Dictionary<string, IPEndPoint> cachedNtpEndPoints = new();
        private static readonly object                         ntpEndPointLock    = new();

        private static readonly string[] ntpServers = {
            "time.google.com"
          , "time.cloudflare.com"
          , "pool.ntp.org"
          , "time.windows.com"
        };

        private static readonly Uri[] httpTimeUris = {
            new("https://www.google.com/generate_204")
          , new("https://www.cloudflare.com/cdn-cgi/trace")
        };

        /// <summary>
        /// Current time from the network, as <see cref="DateTimeKind.Utc"/>.<br/>
        /// Throws <see cref="TimeoutException"/>, listing every failed attempt, when nothing answers within <paramref name="timeoutSeconds"/>.<br/>
        /// Not supported on WebGL.
        /// </summary>
        public static Task<DateTime> NowAsync(
            float timeoutSeconds = DefaultTimeoutSeconds
          , CancellationToken cancellationToken = default) {
            ValidateTimeout(timeoutSeconds);
#if UNITY_WEBGL && !UNITY_EDITOR
            return Task.FromException<DateTime>(new PlatformNotSupportedException(
                $"{nameof(NetworkTime)} needs UDP sockets and cross-origin response headers, which WebGL does not allow."));
#else
            return NowAsyncCore(timeoutSeconds, cancellationToken);
#endif
        }

        private static async Task<DateTime> NowAsyncCore(float timeoutSeconds, CancellationToken cancellationToken) {
            var stopwatch = Stopwatch.StartNew();
            var failures  = new List<Exception>();
            var attempt   = 0;

            while (stopwatch.Elapsed.TotalSeconds < timeoutSeconds) {
                foreach (var ntpServer in ntpServers) {
                    cancellationToken.ThrowIfCancellationRequested();
                    var timeoutMilliseconds = GetRemainingTimeoutMilliseconds(stopwatch, timeoutSeconds, NtpAttemptTimeoutMilliseconds);
                    if (timeoutMilliseconds <= 0) break;

                    ++attempt;
                    try {
                        return await GetNtpTime(ntpServer, stopwatch, timeoutSeconds).ConfigureAwait(false);
                    } catch (Exception ex) {
                        failures.Add(new Exception($"Attempt {attempt} via NTP '{ntpServer}' failed: {ex.Message}", ex));
                    }
                }

                foreach (var httpTimeUri in httpTimeUris) {
                    cancellationToken.ThrowIfCancellationRequested();
                    var timeoutMilliseconds = GetRemainingTimeoutMilliseconds(stopwatch, timeoutSeconds, HttpAttemptTimeoutMilliseconds);
                    if (timeoutMilliseconds <= 0) break;

                    ++attempt;
                    try {
                        return await GetHttpDateTime(httpTimeUri, timeoutMilliseconds, cancellationToken).ConfigureAwait(false);
                    } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                        throw;
                    } catch (Exception ex) {
                        failures.Add(new Exception($"Attempt {attempt} via HTTP Date '{httpTimeUri}' failed: {ex.Message}", ex));
                    }
                }

                var retryDelayMilliseconds = GetRemainingTimeoutMilliseconds(stopwatch, timeoutSeconds, RetryDelayMilliseconds);
                if (retryDelayMilliseconds > 0) await Task.Delay(retryDelayMilliseconds, cancellationToken).ConfigureAwait(false);
            }

            throw new TimeoutException(
                $"{nameof(NetworkTime)} failed to get network time after {timeoutSeconds:0.###} seconds "
              + $"and {attempt} attempts. Failures: {BuildFailureSummary(failures)}",
                new AggregateException(failures));
        }

        private static async Task<DateTime> GetNtpTime(
            string ntpServer
          , Stopwatch stopwatch
          , float timeoutSeconds) {
            var attemptStopwatch = Stopwatch.StartNew();
            var ntpData          = new byte[NtpPacketSize];
            ntpData[0] = 0x23; // LI 0, version 4, mode 3 (client)

            var ipEndPoint = await GetNtpEndPoint(ntpServer, stopwatch, attemptStopwatch, timeoutSeconds).ConfigureAwait(false);

            using (var socket = new Socket(ipEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp)) {
                var timeoutMilliseconds = GetRemainingAttemptTimeoutMilliseconds(
                    stopwatch, attemptStopwatch, timeoutSeconds, NtpAttemptTimeoutMilliseconds);
                await WithTimeout(
                    socket.ConnectAsync(ipEndPoint),
                    timeoutMilliseconds,
                    $"Connection to NTP server '{ntpServer}' timed out after {timeoutMilliseconds}ms.").ConfigureAwait(false);

                timeoutMilliseconds = GetRemainingAttemptTimeoutMilliseconds(
                    stopwatch, attemptStopwatch, timeoutSeconds, NtpAttemptTimeoutMilliseconds);
                await WithTimeout(
                    socket.SendAsync(new ArraySegment<byte>(ntpData), SocketFlags.None),
                    timeoutMilliseconds,
                    $"Sending request to NTP server '{ntpServer}' timed out after {timeoutMilliseconds}ms.").ConfigureAwait(false);

                timeoutMilliseconds = GetRemainingAttemptTimeoutMilliseconds(
                    stopwatch, attemptStopwatch, timeoutSeconds, NtpAttemptTimeoutMilliseconds);
                var receivedBytes = await WithTimeout(
                    socket.ReceiveAsync(new ArraySegment<byte>(ntpData), SocketFlags.None),
                    timeoutMilliseconds,
                    $"Receiving response from NTP server '{ntpServer}' timed out after {timeoutMilliseconds}ms.").ConfigureAwait(false);

                if (receivedBytes < NtpPacketSize) {
                    throw new InvalidOperationException(
                        $"NTP server '{ntpServer}' returned incomplete response. Expected {NtpPacketSize} bytes, got {receivedBytes} bytes.");
                }
            }

            var mode          = ntpData[0] & 0x07;
            var leapIndicator = ntpData[0] >> 6;
            var stratum       = ntpData[1];

            if (mode != 4) {
                throw new InvalidOperationException($"NTP server '{ntpServer}' returned invalid mode: {mode}.");
            }

            if (leapIndicator == 3) {
                throw new InvalidOperationException($"NTP server '{ntpServer}' clock is unsynchronized.");
            }

            if (stratum == 0) {
                throw new InvalidOperationException($"NTP server '{ntpServer}' returned a kiss-of-death response.");
            }

            const int transmitTimestampOffset = 40;
            long ntpSeconds  = ReadUInt32BigEndian(ntpData, transmitTimestampOffset);
            long ntpFraction = ReadUInt32BigEndian(ntpData, transmitTimestampOffset + 4);

            if (ntpSeconds == 0) {
                throw new InvalidOperationException($"NTP server '{ntpServer}' returned an empty transmit timestamp.");
            }

            // The 32-bit NTP seconds wrap on 2036-02-07. A cleared top bit means the count restarted in the next era,
            // because no server answers with a time before 1968.
            if ((ntpSeconds & 0x80000000L) == 0) ntpSeconds += 1L << 32;

            var unixMilliseconds = (ntpSeconds - NtpUnixEpochOffsetSeconds) * 1000L + (ntpFraction * 1000L >> 32);
            return DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds).UtcDateTime;
        }

        private static uint ReadUInt32BigEndian(byte[] data, int offset) {
            return ((uint)data[offset] << 24)
                 | ((uint)data[offset + 1] << 16)
                 | ((uint)data[offset + 2] << 8)
                 | data[offset + 3];
        }

        private static async Task<IPEndPoint> GetNtpEndPoint(
            string ntpServer
          , Stopwatch stopwatch
          , Stopwatch attemptStopwatch
          , float timeoutSeconds) {
            lock (ntpEndPointLock) {
                if (cachedNtpEndPoints.TryGetValue(ntpServer, out var cachedNtpEndPoint)) return cachedNtpEndPoint;
            }

            var timeoutMilliseconds = GetRemainingAttemptTimeoutMilliseconds(
                stopwatch, attemptStopwatch, timeoutSeconds, NtpAttemptTimeoutMilliseconds);
            var addresses = await WithTimeout(
                Dns.GetHostAddressesAsync(ntpServer),
                timeoutMilliseconds,
                $"DNS resolve for NTP server '{ntpServer}' timed out after {timeoutMilliseconds}ms.").ConfigureAwait(false);

            foreach (var address in addresses) {
                if (address.AddressFamily is not (AddressFamily.InterNetwork or AddressFamily.InterNetworkV6)) continue;

                var ipEndPoint = new IPEndPoint(address, NtpPort);
                lock (ntpEndPointLock) cachedNtpEndPoints[ntpServer] = ipEndPoint;
                return ipEndPoint;
            }

            throw new InvalidOperationException($"Failed to resolve DNS for NTP server '{ntpServer}'.");
        }

        private static async Task<DateTime> GetHttpDateTime(
            Uri uri
          , int timeoutMilliseconds
          , CancellationToken cancellationToken) {
            // Cancel the request itself on timeout, so it does not keep running in the background.
            using (var attemptCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)) {
                attemptCancellation.CancelAfter(timeoutMilliseconds);

                try {
                    using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                    using (var response = await NetworkHttp.Client.SendAsync(
                               request, HttpCompletionOption.ResponseHeadersRead, attemptCancellation.Token).ConfigureAwait(false)) {
                        if (!response.Headers.Date.HasValue) {
                            throw new InvalidOperationException($"HTTP response from '{uri}' does not contain Date header.");
                        }

                        return response.Headers.Date.Value.UtcDateTime;
                    }
                } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
                    throw new TimeoutException($"HTTP Date request to '{uri}' timed out after {timeoutMilliseconds}ms.");
                }
            }
        }

        private static int GetRemainingTimeoutMilliseconds(
            Stopwatch stopwatch
          , float timeoutSeconds
          , int maxTimeoutMilliseconds) {
            var remainingMilliseconds = (int)Math.Ceiling(timeoutSeconds * 1000d - stopwatch.Elapsed.TotalMilliseconds);

            return Math.Max(0, Math.Min(maxTimeoutMilliseconds, remainingMilliseconds));
        }

        private static int GetRemainingAttemptTimeoutMilliseconds(
            Stopwatch stopwatch
          , Stopwatch attemptStopwatch
          , float timeoutSeconds
          , int maxAttemptTimeoutMilliseconds) {
            var totalRemainingMilliseconds = GetRemainingTimeoutMilliseconds(
                stopwatch, timeoutSeconds, maxAttemptTimeoutMilliseconds);
            var attemptRemainingMilliseconds = (int)Math.Ceiling(
                maxAttemptTimeoutMilliseconds - attemptStopwatch.Elapsed.TotalMilliseconds);

            return Math.Max(0, Math.Min(totalRemainingMilliseconds, attemptRemainingMilliseconds));
        }

        private static void ValidateTimeout(float timeoutSeconds) {
            if (float.IsNaN(timeoutSeconds) || float.IsInfinity(timeoutSeconds) || timeoutSeconds <= 0f) {
                throw new ArgumentOutOfRangeException(
                    nameof(timeoutSeconds),
                    timeoutSeconds,
                    "Timeout must be a positive finite value.");
            }
        }

        private static async Task WithTimeout(Task task, int timeoutMilliseconds, string timeoutMessage) {
            if (timeoutMilliseconds <= 0) throw new TimeoutException(timeoutMessage);

            var completedTask = await Task.WhenAny(task, Task.Delay(timeoutMilliseconds)).ConfigureAwait(false);
            if (completedTask != task) {
                ObserveLateFault(task);
                throw new TimeoutException(timeoutMessage);
            }

            await task.ConfigureAwait(false);
        }

        private static async Task<T> WithTimeout<T>(Task<T> task, int timeoutMilliseconds, string timeoutMessage) {
            if (timeoutMilliseconds <= 0) throw new TimeoutException(timeoutMessage);

            var completedTask = await Task.WhenAny(task, Task.Delay(timeoutMilliseconds)).ConfigureAwait(false);
            if (completedTask != task) {
                ObserveLateFault(task);
                throw new TimeoutException(timeoutMessage);
            }

            return await task.ConfigureAwait(false);
        }

        /// <summary>
        /// A socket call left behind by a timeout fails once the socket is disposed.<br/>
        /// Reading its exception keeps it from being reported as an unobserved task exception.
        /// </summary>
        private static void ObserveLateFault(Task task) {
            task.ContinueWith(
                abandonedTask => { _ = abandonedTask.Exception; },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        private static string BuildFailureSummary(IReadOnlyList<Exception> failures) {
            if (failures.Count == 0) return "No network-time attempt was started before the time budget expired.";

            var messages = new string[failures.Count];
            for (var i = 0; i < failures.Count; ++i) {
                messages[i] = $"{i + 1}. {failures[i].Message}";
            }

            return string.Join(" | ", messages);
        }
    }
}
