using System.IO;
using System.Net;
using System.Net.Http.Headers;

namespace Docker.Registry.DotNet.Registry
{
    internal abstract class RegistryApiResponse
    {
        protected RegistryApiResponse(HttpStatusCode statusCode, HttpResponseHeaders headers)
        {
            this.StatusCode = statusCode;
            this.Headers = headers;
        }

        public HttpStatusCode StatusCode { get; }

        public HttpResponseHeaders Headers { get; }

        public abstract string ResponseBody { get; }
    }

    internal class RegistryApiResponse<TBody> : RegistryApiResponse
    {
        internal RegistryApiResponse(
            HttpStatusCode statusCode,
            TBody body,
            HttpResponseHeaders headers)
            : base(statusCode, headers)
        {
            this.Body = body;
        }

        public TBody Body { get; }

        public override string ResponseBody
        {
            get
            {
                if (Body is string body)
                    return body;
                else
                    return string.Empty;
            }
        }
    }
}