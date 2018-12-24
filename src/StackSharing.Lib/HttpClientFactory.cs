using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace StackSharing.Lib
{
    /// <seealso cref="IHttpClientFactory" />
    internal class HttpClientFactory : IHttpClientFactory
    {
        /// <summary>
        /// Always return the same static HttpClient. See https://aspnetmonsters.com/2016/08/2016-08-27-httpclientwrong/
        /// </summary>
        private readonly HttpClient _client;

        public HttpClientFactory(IConnectionSettings connectionSettings)
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.Default.GetBytes($"{connectionSettings.UserName}:{connectionSettings.GetPassword()}")));
        }

        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <returns></returns>
        public HttpClient GetHttpClient()
        {
            return _client;
        }
    }
}