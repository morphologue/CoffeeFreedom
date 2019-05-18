using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using CoffeeFreedom.Api.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoffeeFreedom.Api.Middleware
{
    public class BasicAuthenticationScheme : AuthenticationHandler<BasicAuthenticationSchemeOptions>
    {
        private readonly IHttpContextAccessor _accessor;

        public BasicAuthenticationScheme(IOptionsMonitor<BasicAuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IHttpContextAccessor accessor) : base(options, logger, encoder, clock)
        {
            _accessor = accessor;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!_accessor.HttpContext.Request.TryExtractBasicCredentials(out string username, out string password)
                || !Options.UserPasswords.ContainsKey(username) || Options.UserPasswords[username] != password)
            {
                return Task.FromResult(AuthenticateResult.Fail("Basic authentication failed"));
            }

            Claim[] claims = {new Claim(ClaimTypes.Name, username)};
            ClaimsIdentity identity = new ClaimsIdentity(claims, Scheme.Name);
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
        }
    }
}
