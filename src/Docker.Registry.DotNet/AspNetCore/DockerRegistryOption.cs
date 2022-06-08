using System;
using System.Collections.Generic;
using System.Text;

namespace Docker.Registry.DotNet.AspNetCore
{
    public class DockerRegistryOption
    {
        /// <summary>
        /// Registry Endpoint
        /// </summary>
        public string RegistryUrl { get; set; }

        /// <summary>
        /// UserName
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Authentication Mode
        /// </summary>
        public AuthMode AuthMode { get; set; } = AuthMode.AnonymousOAuth;

        public int Retry { get; set; } = 3;

        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(300);

        public long ChunkSize { get; set; } = 52428800L;

        public string RepoPrefix { get; set; }

        public int MaxConcurrency { get; set; } = 3;
    }
}
