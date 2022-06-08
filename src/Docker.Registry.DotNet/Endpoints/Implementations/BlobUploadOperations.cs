using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Docker.Registry.DotNet.Helpers;
using Docker.Registry.DotNet.Models;
using Docker.Registry.DotNet.Registry;
using Microsoft.Extensions.Logging;

namespace Docker.Registry.DotNet.Endpoints.Implementations
{
    internal class BlobUploadOperations : IBlobUploadOperations
    {
        private readonly NetworkClient _client;
        private readonly ILogger<BlobUploadOperations> _logger;

        internal BlobUploadOperations(NetworkClient client)
        {
            this._client = client;
            _logger = client.GetLoggerFactory().CreateLogger<BlobUploadOperations>();
        }

        /// <summary>
        ///     Perform a monolithic upload.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="contentLength"></param>
        /// <param name="stream"></param>
        /// <param name="digest"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task UploadBlobAsync(
            string name,
            int contentLength,
            Stream stream,
            string digest,
            CancellationToken cancellationToken = default)
        {
            BinaryRequestContent content = new BinaryRequestContent(stream, "application/octet-stream");
            await UploadBlobAsync(name,digest, content,cancellationToken).ConfigureAwait(false);
        }

        public async Task UploadBlobAsync(
            string name,
            string digest,
            IRequestContent content,
            CancellationToken cancellationToken = default)
        {
            var response = await InitiateBlobUploadAsync(name);

            try
            {
                var queryString = new QueryString();

                if (response.Location.IsAbsoluteUri && !string.IsNullOrEmpty(response.Location.Query))
                {
                    var collection = HttpUtility.ParseQueryString(response.Location.OriginalString);
                    foreach (var item in collection)
                    {
                        queryString.Add(item.Key, item.Value);
                    }
                }

                queryString.Add("digest", digest);

                await _client.MakeRequestAsync(cancellationToken, HttpMethod.Put, $"v2/{_client.RepoPrefix}{name}/blobs/uploads/{response.DockerUploadUuid}",
                    queryString, null, content).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Attempting to cancel the upload...");

                await CancelBlobUploadAsync(name, response.DockerUploadUuid, response.Location);

                throw ex;
            }
        }

        public async Task<ResumableUploadResponse> InitiateBlobUploadAsync(
            string name,
            Stream stream = null,
            CancellationToken cancellationToken = default)
        {
            var path = $"v2/{_client.RepoPrefix}{name}/blobs/uploads/";

            var response = await this._client.MakeRequestAsync(
                               cancellationToken,
                               HttpMethod.Post,
                               path);

            var uuid = response.Headers.GetString("Docker-Upload-UUID");

            _logger.LogInformation($"Initiate Uploading with uuid: {uuid}");

            var location = response.Headers.GetString("Location");

            _logger.LogInformation($"Using Initiate location: {location}");

            return new ResumableUploadResponse { 
            
                DockerUploadUuid=uuid,
                Location=response.Headers.Location
            };
        }

        public async Task<MountResponse> MountBlobAsync(
            string name,
            MountParameters parameters,
            CancellationToken cancellationToken = default)
        {
            parameters = parameters ?? throw new ArgumentNullException("MountParameters");

            var queryParameters = new QueryString();

            queryParameters.Add("mount",parameters.Digest);
            queryParameters.Add("from", parameters.From);

            var path = $"v2/{_client.RepoPrefix}{name}/blobs/uploads/";

            var response = await this._client.MakeRequestAsync(cancellationToken, HttpMethod.Post, path, queryParameters);

            return new MountResponse { 
            
                DockerContentDigest= response.Headers.GetString("Docker-Content-Digest"),
                Location=response.Headers.Location,
                DockerUploadUuid= response.Headers.GetString("Docker-Upload-UUID"),
                Range= response.Headers.GetString("Range"),
                StatusCode=response.StatusCode
            };
        }

        public async Task<BlobUploadStatus> GetBlobUploadStatus(
            string name,
            string uuid,
            CancellationToken cancellationToken = default)
        {
            var path = $"v2/{_client.RepoPrefix}{name}/blobs/uploads/{uuid}";

            var response= await this._client.MakeRequestAsync(cancellationToken, HttpMethod.Get, path);

            var getUuid = response.Headers.GetString("Docker-Upload-UUID");

            _logger.LogInformation($"Blob Uploading status with uuid: {getUuid}");

            var range = response.Headers.GetString("Range");

            _logger.LogInformation($"Blob Uploading status with range: {range}");

            return new BlobUploadStatus
            {
                DockerUploadUuid = getUuid,
                Range = range
            };
        }

        public async Task<ResumableUploadResponse> UploadBlobChunkAsync(
            string name,
            string uuid,
            ChunkLayer chunkLayer,
            long contentLength,
            Uri location,
            CancellationToken cancellationToken = default)
        {
            try
            {

                var queryString = new QueryString();

                if (location.IsAbsoluteUri && !string.IsNullOrEmpty(location.Query))
                {
                    var collection = HttpUtility.ParseQueryString(location.OriginalString);
                    foreach (var item in collection)
                    {
                        queryString.Add(item.Key, item.Value);
                    }
                }

                BinaryRequestContent content = new BinaryRequestContent(chunkLayer.ChunkBlob, "application/octet-stream", chunkLayer.Begin, chunkLayer.End,contentLength);
                var response = await _client.MakeRequestAsync(cancellationToken, new HttpMethod("PATCH"), $"v2/{_client.RepoPrefix}{name}/blobs/uploads/{uuid}",
                        queryString, null, content).ConfigureAwait(false);

                var getUuid = response.Headers.GetString("Docker-Upload-UUID");

                _logger.LogInformation($"ChunkBlob Uploading status with uuid: {getUuid}");

                var range = response.Headers.GetString("Range");

                _logger.LogInformation($"ChunkBlob Uploading status with range: {range}");

                return new ResumableUploadResponse
                {
                    Range = range,
                    DockerUploadUuid = uuid,
                    Location = response.Headers.Location
                };
            }
            catch(RegistryApiException ex)
            {
                if (ex.StatusCode==HttpStatusCode.RequestedRangeNotSatisfiable)
                {
                    _logger.LogWarning("Invalid Content-Range header format , Out of order chunk: the range of the next chunk must start immediately after the “last valid range” from the previous response.");
                }

                await CancelBlobUploadAsync(name, uuid,location);
                throw ex;

            }
            catch (Exception ex)
            {

                _logger.LogWarning("Attempting to cancel the upload...");

                await CancelBlobUploadAsync(name, uuid, location);

                throw ex;
            }

        }

        public async Task<ResumableUploadResponse> CompleteBlobUploadAsync(
            string name,
            string uuid,
            string digest,
            Uri location,
            long contentLength=0,
            Stream chunk = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var queryString = new QueryString();

                if (location.IsAbsoluteUri && !string.IsNullOrEmpty(location.Query))
                {
                    var collection = HttpUtility.ParseQueryString(location.OriginalString);
                    foreach (var item in collection)
                    {
                        queryString.Add(item.Key, item.Value);
                    }
                }

                queryString.Add("digest", digest);

                BinaryRequestContent content = new BinaryRequestContent(new MemoryStream(), "application/octet-stream",contentLength);

                var response= await _client.MakeRequestAsync(cancellationToken, HttpMethod.Put, $"v2/{_client.RepoPrefix}{name}/blobs/uploads/{uuid}",
                    queryString, null, content);

                var getDigest = response.Headers.GetString("Docker-Content-Digest");

                _logger.LogInformation($"finished Uploading with digest: {getDigest}");

                var getLocation = response.Headers.GetString("Location");

                _logger.LogInformation($"finished to location: {getLocation}");

                return new ResumableUploadResponse
                {

                    DockerUploadUuid = uuid,
                    Location = response.Headers.Location
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Finished Attempting to cancel the upload...");

                await CancelBlobUploadAsync(name, uuid, location);

                throw ex;
            }
        }

        public async Task CancelBlobUploadAsync(
            string name,
            string uuid,
            Uri location,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var path = $"v2/{_client.RepoPrefix}{name}/blobs/uploads/{uuid}";

                var queryString = new QueryString();

                if (location.IsAbsoluteUri && !string.IsNullOrEmpty(location.Query))
                {
                    var collection = HttpUtility.ParseQueryString(location.OriginalString);
                    foreach (var item in collection)
                    {
                        queryString.Add(item.Key, item.Value);
                    }
                }

                await this._client.MakeRequestAsync(cancellationToken, HttpMethod.Delete, path, queryString);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"cancel Upload blob not sucess: {ex.Message}");
            }

        }


        public async Task<BlobHeader> CheckExistingBlobAsync(
            string name,
            string digest,
    CancellationToken cancellationToken = default)
        {
            var path = $"v2/{_client.RepoPrefix}{name}/blobs/{digest}";

            var response = await this._client.MakeRequestAsync(
                               cancellationToken,
                               HttpMethod.Head,
                               path);

            return new BlobHeader(response.Headers.GetString("Docker-Content-Digest"));
        }
    }
}