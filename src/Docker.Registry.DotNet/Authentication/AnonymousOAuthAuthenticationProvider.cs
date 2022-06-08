using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Docker.Registry.DotNet.OAuth;

using JetBrains.Annotations;

namespace Docker.Registry.DotNet.Authentication
{
    [PublicAPI]
    public class AnonymousOAuthAuthenticationProvider : AuthenticationProvider
    {
        private readonly OAuthClient _client;

        public AnonymousOAuthAuthenticationProvider(IHttpClientFactory clientFactory = null)
        {
            _client = new OAuthClient(clientFactory != null ? clientFactory.CreateClient(ConstConfig.ClientName) : new HttpClient());
        }

        private static string Schema { get; } = "Bearer";

        public override Task AuthenticateAsync(HttpRequestMessage request)
        {
            return Task.CompletedTask;
        }

        public override async Task AuthenticateAsync(
            HttpRequestMessage request,
            HttpResponseMessage response)
        {
            var header = this.TryGetSchemaHeader(response, Schema);

            base.SetHeaders(request, response);

            //Get the bearer bits
            var bearerBits = AuthenticateParser.ParseTyped(header.Parameter);

            //Get the token
            var token = await this._client.GetTokenAsync(
                            bearerBits.Realm,
                            bearerBits.Service,
                            bearerBits.Scope);

            //Set the header
            request.Headers.Authorization = new AuthenticationHeaderValue(Schema, token.Token);
        }
    }
}