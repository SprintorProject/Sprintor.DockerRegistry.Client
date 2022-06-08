using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace Docker.Registry.DotNet.Authentication
{
    [PublicAPI]
    public class BasicAuthenticationProvider : AuthenticationProvider
    {
        private readonly string _password;

        private readonly string _username;

        public BasicAuthenticationProvider(string username, string password)
        {
            this._username = username;
            this._password = password;
        }

        private static string Schema { get; } = "Basic";

        public override Task AuthenticateAsync(HttpRequestMessage request)
        {
            //Set the header
            request.Headers.Authorization =
                new AuthenticationHeaderValue(Schema, BuildBasicAuthToken());

            return Task.CompletedTask;
        }

        public override Task AuthenticateAsync(
            HttpRequestMessage request,
            HttpResponseMessage response)
        {
            //this.TryGetSchemaHeader(response, Schema);

            base.SetHeaders(request, response);

            //Set the header
            request.Headers.Authorization =
                new AuthenticationHeaderValue(Schema, BuildBasicAuthToken());

            return Task.CompletedTask;
        }

        private string BuildBasicAuthToken()
        {
            
            var authInfo = $"{_username}:{_password}";
            if(ChangeUserAuthConfig!=null&&!string.IsNullOrEmpty(ChangeUserAuthConfig.Username)) authInfo = $"{ChangeUserAuthConfig.Username}:{ChangeUserAuthConfig.Password}";

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(authInfo));
        }
    }
}