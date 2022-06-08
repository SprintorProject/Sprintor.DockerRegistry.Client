using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Docker.Registry.DotNet.Helpers
{
    public interface IRequestContent
    {
        HttpContent GetContent();
    }
}
