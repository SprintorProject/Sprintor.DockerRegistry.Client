using System;
using System.Net;
using System.Net.Http.Headers;

namespace Docker.Registry.DotNet.Registry
{
    public class RegistryApiException : Exception
    {
        internal RegistryApiException(RegistryApiResponse response)
            : base($"Docker Registry API responded with status code={response.StatusCode}, response={response.ResponseBody}")
        {
            this.StatusCode = response.StatusCode;
            this.Headers = response.Headers;
            this.ResponseBody = response.ResponseBody;
        }

        public HttpStatusCode StatusCode { get; }

        public HttpResponseHeaders Headers { get; }

        public string ResponseBody { get; }
    }
}