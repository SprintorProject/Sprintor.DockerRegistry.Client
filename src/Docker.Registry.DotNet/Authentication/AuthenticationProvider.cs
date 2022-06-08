using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Docker.Registry.DotNet.Helpers;

namespace Docker.Registry.DotNet.Authentication
{
    /// <summary>
    /// Authentication provider.
    /// </summary>
    public abstract class AuthenticationProvider
    {

        protected AuthConfig ChangeUserAuthConfig { get; set; }

        /// <summary>
        /// Called on the initial send
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public abstract Task AuthenticateAsync(HttpRequestMessage request);

        /// <summary>
        /// Called when the send is challenged.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public abstract Task AuthenticateAsync(HttpRequestMessage request, HttpResponseMessage response);

        /// <summary>
        /// Gets the schema header from the http response.
        /// </summary>
        /// <param name="response"></param>
        /// <param name="schema"></param>
        /// <returns></returns>
        protected AuthenticationHeaderValue TryGetSchemaHeader(HttpResponseMessage response, string schema)
        {
            var header = response.GetHeaderBySchema(schema);

            if (header == null)
            {
                throw new InvalidOperationException(
                    $"No WWW-Authenticate challenge was found for schema {schema}");
            }

            return header;
        }

        protected void SetHeaders(HttpRequestMessage request,
            HttpResponseMessage response)
        {
            var xCsrfToken = response.Headers.GetString("X-Harbor-Csrf-Token");
            if (!string.IsNullOrEmpty(xCsrfToken))
            {
                request.Headers.Add("X-Harbor-Csrf-Token", xCsrfToken);
            }
        }

        public void ChangeUserSession(string username,string password)
        {
            ChangeUserAuthConfig = new AuthConfig
            {
                Username = username,
                Password = password
            };
        }
    }
}