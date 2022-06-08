using Docker.Registry.DotNet.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Docker.Registry.DotNet.Helpers
{
    public class DefaultChunkLayerBuilder: IChunkLayerBuilder
    {
        private readonly Stream _stream;
        private readonly long _chunkSize;
        public DefaultChunkLayerBuilder(Stream originalLayerStream,long chunkSize)
        {
            if (originalLayerStream == null)
            {
                throw new ArgumentNullException(nameof(originalLayerStream));
            }

            _stream = originalLayerStream;
            _chunkSize = chunkSize;
        }

        public List<ChunkLayer> Build()
        {
            if (_stream.Length == 0 || _stream.Length < _chunkSize) return new List<ChunkLayer>();

            var contentLength = _stream.Length;
            var block = _chunkSize;
            var count = contentLength / block;
            while (count >= int.MaxValue)
            {
                block *= 2;
                count = contentLength / block;
            }
            var mod = contentLength % block;
            var listChunk = Enumerable.Range(0, (int)count)
                .Select(x => (Begin: x * block, Offset: block - 1))
                .Concat(new[] { (Begin: count * block, Offset: mod - 1) })
                .Select(x => new ChunkLayer()
                {
                    Begin = x.Begin,
                    End = x.Begin + x.Offset,
                    ChunkBlob = GetChunkData((int)x.Offset + 1)
                })
                .ToList();

            return listChunk;
        }

        private Stream GetChunkData(int chunkCount)
        {
            byte[] buffer = new byte[chunkCount];
            _stream.Read(buffer, 0, chunkCount);
            return new MemoryStream(buffer);
        }
    }
}
