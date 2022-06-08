using System;
using System.Net;

namespace Docker.Registry.DotNet.Models
{
    public class MountResponse
    {
        /// <summary>
        /// Location
        /// </summary>
        public Uri Location { get; set; }

        /// <summary>
        /// Docker-Content-Digest
        /// </summary>
        public string DockerContentDigest { get; set; }

        /// <summary>
        ///     Range header indicating the progress of the upload. When starting an upload, it will return an empty range, since
        ///     no content has been received.
        /// </summary>
        public string Range { get; set; }

        /// <summary>
        ///     Identifies the docker upload uuid for the current request.
        /// </summary>
        public string DockerUploadUuid { get; set; }

        /// <summary>
        /// StatusCode
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }
    }
}