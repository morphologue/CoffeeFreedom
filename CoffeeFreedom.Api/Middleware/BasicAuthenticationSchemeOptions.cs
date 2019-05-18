using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication;

namespace CoffeeFreedom.Api.Middleware
{
    public class BasicAuthenticationSchemeOptions : AuthenticationSchemeOptions
    {
        public Dictionary<string, string> UserPasswords { get; set; } = new Dictionary<string, string>();
    }
}
