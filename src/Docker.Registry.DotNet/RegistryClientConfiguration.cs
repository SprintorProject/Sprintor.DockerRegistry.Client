using System;
using System.Net.Http;
using System.Threading;

using Docker.Registry.DotNet.Authentication;
using Docker.Registry.DotNet.Registry;

using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Docker.Registry.DotNet
{
    public class RegistryClientConfiguration
    {
        /// <summary>
        ///     Creates an instance of the RegistryClientConfiguration.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="clientFactory"></param>
        /// <param name="defaultTimeout"></param>
        public RegistryClientConfiguration(string host, ILoggerFactory loggerFactory=null, IHttpClientFactory clientFactory = null, TimeSpan defaultTimeout = default)
            : this(defaultTimeout)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(host));

            this.Host = host;
            this.ClientFactory = clientFactory;
            LoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        }

        public RegistryClientConfiguration(string host,int retry,long chunkSize, ILoggerFactory loggerFactory=null, IHttpClientFactory clientFactory = null, TimeSpan defaultTimeout = default)
            : this(host, loggerFactory,clientFactory, defaultTimeout)
        {
            Retry = retry;
            ChunkSize = chunkSize;
        }

        public RegistryClientConfiguration(string host, int retry, long chunkSize,int maxConcurrency,string repoPrefix, ILoggerFactory loggerFactory = null, IHttpClientFactory clientFactory = null, TimeSpan defaultTimeout = default)
            : this(host,retry,chunkSize, loggerFactory, clientFactory, defaultTimeout)
        {
            MaxConcurrency = maxConcurrency;
            RepoPrefix = repoPrefix;
        }

        /// <summary>
        ///     Obsolete constructor that allows a uri to be used to specify a registry.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="defaultTimeout"></param>
        [Obsolete("Use the constructor that allows you to specify a host.")]
        public RegistryClientConfiguration(Uri endpoint, TimeSpan defaultTimeout = default)
            : this(defaultTimeout)
        {
            this.EndpointBaseUri = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        private RegistryClientConfiguration(TimeSpan defaultTimeout)
        {
            LoggerFactory = LoggerFactory?? NullLoggerFactory.Instance;
            if (defaultTimeout != TimeSpan.Zero)
            {
                if (defaultTimeout < Timeout.InfiniteTimeSpan)
                    // TODO: Should be a resource for localization.
                    // TODO: Is this a good message?
                    throw new ArgumentException(
                        "Timeout must be greater than Timeout.Infinite",
                        nameof(defaultTimeout));
                this.DefaultTimeout = defaultTimeout;
            }
        }

        public Uri EndpointBaseUri { get; }

        public string Host { get; }

        public int Retry { get; }

        public long ChunkSize { get; internal set; } = 52428800L;

        public IHttpClientFactory ClientFactory { get; }

        public ILoggerFactory LoggerFactory { get; }

        public string RepoPrefix { get; }

        public int MaxConcurrency { get; } = 3;

        public TimeSpan DefaultTimeout { get; internal set; } = TimeSpan.FromSeconds(100);

        [PublicAPI]
        public IRegistryClient CreateClient()
        {
            return new RegistryClient(this, new AnonymousOAuthAuthenticationProvider());
        }

        [PublicAPI]
        public IRegistryClient CreateClient(AuthenticationProvider authenticationProvider)
        {
            return new RegistryClient(this, authenticationProvider);
        }
    }
}