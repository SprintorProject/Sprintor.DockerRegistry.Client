using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Docker.Registry.DotNet.Models
{
    public class ChunkLayer:IDisposable
    {
        public long Begin { get; set; }

        public long End { get; set; }

        public long Size => ChunkBlob.Length;

        public Stream ChunkBlob { get; set; }

        public void Dispose()
        {
            ChunkBlob?.Dispose();
        }
    }
}
