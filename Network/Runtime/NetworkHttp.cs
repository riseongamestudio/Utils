using System.Net.Http;

namespace RiseOn.Utils.Network {
    internal static class NetworkHttp {
        /// <summary>
        /// One client for the whole assembly: <see cref="HttpClient"/> is meant to be reused, not created per request.
        /// </summary>
        internal static HttpClient Client { get; } = new();
    }
}
