using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Docker.Registry.DotNet.Helpers;
using Docker.Registry.DotNet.Models;
using Docker.Registry.DotNet.Registry;

using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Docker.Registry.DotNet.Endpoints.Implementations
{
    internal class ManifestOperations : IManifestOperations
    {
        private readonly NetworkClient _client;
        private readonly ILogger<ManifestOperations> _logger;

        public ManifestOperations(NetworkClient client)
        {
            this._client = client;
            _logger = client.GetLoggerFactory().CreateLogger<ManifestOperations>();
        }

        public async Task<GetImageManifestResult> GetManifestAsync(
            string name,
            string reference,
            string mediaType= ManifestMediaTypes.ManifestSchema2,
            CancellationToken cancellationToken = default)
        {
            var headers = new Dictionary<string, string>
                          {
                              {
                                  "Accept",
                                  $"{mediaType}"
                              }
                          };

            var response = await this._client.MakeRequestAsync(
                               cancellationToken,
                               HttpMethod.Get,
                               $"v2/{_client.RepoPrefix}{name}/manifests/{reference}",
                               null,
                               headers).ConfigureAwait(false);

            var contentType = this.GetContentType(response.GetHeader("ContentType"), response.Body);

            switch (contentType)
            {
                case ManifestMediaTypes.ManifestSchema1:
                case ManifestMediaTypes.ManifestSchema1Signed:
                    return new GetImageManifestResult(
                               contentType,
                               this._client.JsonSerializer.DeserializeObject<ImageManifest2_1>(
                                   response.Body),
                               response.Body)
                           {
                               DockerContentDigest = response.GetHeader("Docker-Content-Digest"),
                               Etag = response.GetHeader("Etag")
                           };

                case ManifestMediaTypes.ManifestSchema2:
                    return new GetImageManifestResult(
                               contentType,
                               this._client.JsonSerializer.DeserializeObject<ImageManifest2_2>(
                                   response.Body),
                               response.Body)
                           {
                               DockerContentDigest = response.GetHeader("Docker-Content-Digest")
                           };

                case ManifestMediaTypes.ManifestList:
                    return new GetImageManifestResult(
                        contentType,
                        this._client.JsonSerializer.DeserializeObject<ManifestList>(response.Body),
                        response.Body);

                default:
                    throw new Exception($"Unexpected ContentType '{contentType}'.");
            }
        }

        public async Task PutManifestAsync(string name, string reference, ImageManifest2_2 manifest,
            CancellationToken cancellationToken = default)
        {
            var path = $"v2/{_client.RepoPrefix}{name}/manifests/{reference}";
            var content = new JsonRequestContent<ImageManifest2_2>(manifest, _client.JsonSerializer, "application/vnd.docker.distribution.manifest.v2+json");
            await this._client.MakeRequestAsync(cancellationToken, HttpMethod.Put, path,null,null,content).ConfigureAwait(false);
        }

        public async Task<bool> DoesManifestExistAsync(string name, string reference, CancellationToken cancellation = default)
        {
            try
            {
                var response= await this.GetManifestAsync(name, reference, ManifestMediaTypes.ManifestSchema2, cancellation).ConfigureAwait(false);

                _logger.LogInformation($"images  {name}:{reference}  is exist ,digest:{response?.DockerContentDigest}");
                return string.IsNullOrEmpty(response?.DockerContentDigest)?false:true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,$"images {name}:{reference} is not exist!");
                return false;
            }
        }

        public async Task DeleteManifestAsync(
            string name,
            string reference,
            CancellationToken cancellationToken = default)
        {
            var path = $"v2/{_client.RepoPrefix}{name}/manifests/{reference}";

            await this._client.MakeRequestAsync(cancellationToken, HttpMethod.Delete, path,null ,new Dictionary<string, string> { { "Accept", "application/vnd.docker.distribution.manifest.v2+json" } });
        }

        private string GetContentType(string contentTypeHeader, string manifest)
        {
            if (!string.IsNullOrWhiteSpace(contentTypeHeader))
                return contentTypeHeader;

            var check = JsonConvert.DeserializeObject<SchemaCheck>(manifest);

            if (!string.IsNullOrWhiteSpace(check.MediaType))
                return check.MediaType;

            if (check.SchemaVersion == null)
                return ManifestMediaTypes.ManifestSchema1;

            if (check.SchemaVersion.Value == 2)
                return ManifestMediaTypes.ManifestSchema2;

            throw new Exception(
                $"Unable to determine schema type from version {check.SchemaVersion}");
        }

        [PublicAPI]
        public async Task<string> GetManifestRawAsync(
            string name,
            string reference,
            CancellationToken cancellationToken)
        {
            var headers = new Dictionary<string, string>
                          {
                              {
                                  "Accept",
                                  $"{ManifestMediaTypes.ManifestSchema1}, {ManifestMediaTypes.ManifestSchema2}, {ManifestMediaTypes.ManifestList}, {ManifestMediaTypes.ManifestSchema1Signed}"
                              }
                          };

            var response = await this._client.MakeRequestAsync(
                               cancellationToken,
                               HttpMethod.Get,
                               $"v2/{_client.RepoPrefix}{name}/manifests/{reference}",
                               null,
                               headers).ConfigureAwait(false);

            return response.Body;
        }

        private class SchemaCheck
        {
            /// <summary>
            ///     This field specifies the image manifest schema version as an integer.
            /// </summary>
            [DataMember(Name = "schemaVersion")]
            public int? SchemaVersion { get; set; }

            [DataMember(Name = "mediaType")]
            public string MediaType { get; set; }
        }
    }
}