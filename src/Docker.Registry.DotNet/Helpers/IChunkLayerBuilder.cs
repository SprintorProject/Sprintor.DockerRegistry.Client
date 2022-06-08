using Docker.Registry.DotNet.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Docker.Registry.DotNet.Helpers
{
    public interface IChunkLayerBuilder
    {
        /// <summary>
        /// Build an ordered collection of the ChunkLayer
        /// </summary>
        /// <returns></returns>
        List<ChunkLayer> Build();
    }
}
