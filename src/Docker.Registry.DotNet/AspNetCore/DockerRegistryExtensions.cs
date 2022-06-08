using Docker.Registry.DotNet;
using Docker.Registry.DotNet.AspNetCore;
using Docker.Registry.DotNet.Authentication;
using Docker.Registry.DotNet.Registry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DockerRegistryExtensions
    {
        /// <summary>
        /// Add DockerRegistry
        /// </summary>
        /// <param name="services"></param>
        /// <param name="setupAction"></param>
        public static void AddDockerRegistry(this IServiceCollection services, Action<DockerRegistryOption> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException("services");
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException("setupAction");
            }

            services.AddOptions();
            services.Configure(setupAction);
            services.AddHttpClient(ConstConfig.ClientName);

            services.AddTransient(sp => {

                var clientFactory = sp.GetService<IHttpClientFactory>();
                var loggerFactory = sp.GetService<ILoggerFactory>();
                var optionMonitor = sp.GetService<IOptionsMonitor<DockerRegistryOption>>()?? throw new ArgumentNullException("DockerRegistryOption is be null");
                var option = optionMonitor.CurrentValue;
                
                AuthenticationProvider authenticationProvider = null;
                if (option.AuthMode == AuthMode.PasswordOAuth)
                    authenticationProvider = new PasswordOAuthAuthenticationProvider(option.Username, option.Password, clientFactory);
                else if (option.AuthMode == AuthMode.Basic)
                    authenticationProvider = new BasicAuthenticationProvider(option.Username, option.Password);
                else
                    authenticationProvider = new AnonymousOAuthAuthenticationProvider(clientFactory);

                return new RegistryClientConfiguration(option.RegistryUrl, option.Retry, option.ChunkSize,option.MaxConcurrency,option.RepoPrefix, loggerFactory, clientFactory, option.Timeout)
                .CreateClient(authenticationProvider);
            });
        }
    }
}
