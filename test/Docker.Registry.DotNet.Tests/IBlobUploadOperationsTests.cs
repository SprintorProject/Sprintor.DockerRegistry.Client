using Docker.Registry.DotNet.Helpers;
using Docker.Registry.DotNet.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Docker.Registry.DotNet.Tests
{
    public class IBlobUploadOperationsTests
    {
        private readonly IRegistryClient _client;
        public IBlobUploadOperationsTests()
        {
            var services = new ServiceCollection();

            services.AddLogging(configure=> {

                configure.AddDebug();
            });
            services.AddDockerRegistry(opstion => {

                opstion.RegistryUrl = "http://123.207.62.124";
                opstion.Username = "lingbo";
                opstion.Password = "xxxx";
                opstion.RepoPrefix = "";
                opstion.AuthMode = AspNetCore.AuthMode.PasswordOAuth;
            });

            var serviceProvider = services.BuildServiceProvider();
            _client=serviceProvider.GetRequiredService<IRegistryClient>();
        }

        [Fact]
        public async Task UploadBlobAsync_Test()
        {
            using (var h = SHA256.Create())
            {
                using (var fsLayer = new FileStream(@"D:\lingb\jupyter-singleuser\0d5e9b4a3c9d8fc997f01fa2f3e74ccd2c3404c35d800cff1f96409f06f05ae1\layer.tar", FileMode.Open, FileAccess.Read))
                {
                    var hashResult = h.ComputeHash(fsLayer);
                    var hashHex = BitConverter.ToString(hashResult).Replace("-", "").ToLower();
                    await _client.BlobUploads.UploadBlobAsync("jupyterhub/singleuser1", 0, fsLayer, $"sha256:{hashHex}", CancellationToken.None);
                }
            }
        }

        [Fact]
        public async Task UploadChunkBlobAsync_Test()
        {
            string repoName = "lingbohome/test";

            using (var h = SHA256.Create())
            {
                using (var fsLayer = new FileStream(@"E:\layer.tar", FileMode.Open, FileAccess.Read))
                {
                    var hashResult = h.ComputeHash(fsLayer);
                    var hashHex = BitConverter.ToString(hashResult).Replace("-", "").ToLower();
                    fsLayer.Position = 0;
                    IChunkLayerBuilder chunkLayerBuilder = new DefaultChunkLayerBuilder(fsLayer, 1024*1024);
                    var chunkList = chunkLayerBuilder.Build();

                    if(chunkList==null||chunkList.Count==0)
                    {
                        await _client.BlobUploads.UploadBlobAsync(repoName, 0, fsLayer, $"sha256:{hashHex}", CancellationToken.None);
                    }
                    else
                    {
                       var res= await _client.BlobUploads.InitiateBlobUploadAsync(repoName);

                        string uploadUUid = res.DockerUploadUuid;
                        Uri location = res.Location;
                        foreach (var item in chunkList)
                        {
                            var chunkUploadRes= await _client.BlobUploads.UploadBlobChunkAsync(repoName, uploadUUid, item,fsLayer.Length,location);
                            uploadUUid = chunkUploadRes.DockerUploadUuid;
                            location = chunkUploadRes.Location;
                        }

                        var endUploadRes= await _client.BlobUploads.CompleteBlobUploadAsync(repoName, uploadUUid, $"sha256:{hashHex}", location,fsLayer.Length);
                    }

                }
            }
        }
    }
}
