using System.Net.Http;

namespace StackSharing.Lib
{
    /// <summary>
    /// Factory to create a HttpClient.
    /// </summary>
    internal interface IHttpClientFactory
    {
        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <returns></returns>
        HttpClient GetHttpClient();
    }
}