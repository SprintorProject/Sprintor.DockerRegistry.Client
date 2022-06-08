using System;
using System.Collections.Generic;
using System.Text;

namespace Docker.Registry.DotNet.AspNetCore
{
    public enum AuthMode
    {
        AnonymousOAuth=0,

        PasswordOAuth=1,

        Basic=2
    }
}
