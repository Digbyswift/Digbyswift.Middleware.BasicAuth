using System.Text;
using Digbyswift.Core.Constants;
using Digbyswift.Extensions;
using Digbyswift.Extensions.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Digbyswift.Middleware.BasicAuth
{
    public sealed class BasicAuthenticationMiddleware : IMiddleware
    {
        private const string AuthBypassHeaderName = "AuthBypass";
        
        private readonly BasicAuthSettings _authSettings;

        public BasicAuthenticationMiddleware(IOptions<BasicAuthSettings> authSettings)
        {
            _authSettings = authSettings.Value;
        }

        /// <inheritdoc />
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var result = await ExecuteAsync(context);
            if (result == DelegateResult.Terminate)
            {
                SetUnauthorizedHeader(context);
                return;
            }

            await next(context);
        }

        public async Task<DelegateResult> ExecuteAsync(HttpContext context)
        {
            if (HasByPassKey(context.Request))
                return DelegateResult.AllowContinue;

            if (CanAllowPath(context.Request.Path))
                return DelegateResult.AllowContinue;
            
            if (CanAllowReferrer(context.Request.Headers[HeaderNames.Referer]))
                return DelegateResult.AllowContinue;

            var clientIp = context.Request.GetClientIp().ToString();
            if (CanAllowIp(clientIp))
                return DelegateResult.AllowContinue;
            
            var authenticateResult = await context.AuthenticateAsync(BasicAuthSettings.AuthenticationSchemeName);
            if (authenticateResult.Succeeded)
                return DelegateResult.AllowContinue;

            if (TryGetBasicAuthCredentials(context, out var username, out var password))
            {
                if (username!.Equals(_authSettings.Username, StringComparison.OrdinalIgnoreCase) &&
                    password == _authSettings.Password)
                    return DelegateResult.AllowContinue;
            }

            return DelegateResult.Terminate;
        }
        
        private bool HasByPassKey(HttpRequest request)
        {
            if (String.IsNullOrWhiteSpace(_authSettings.BypassKey))
                return false;
            
            if (request.Headers[AuthBypassHeaderName] == StringValues.Empty)
                return false;
            
            return request.Headers[AuthBypassHeaderName][0] == _authSettings.BypassKey;
        }

        private bool CanAllowPath(PathString path)
        {
            if (_authSettings.ExcludedPaths == null)
                return false;
            
            return _authSettings.ExcludedPaths.Any(excludedPath => path.StartsWithSegments(excludedPath));
        }

        private bool CanAllowReferrer(StringValues referrer)
        {
            if (String.IsNullOrWhiteSpace(referrer))
                return false;
            
            if (_authSettings.WhitelistedReferrers == null)
                return false;

            var primaryReferrer = referrer[0];
            if (primaryReferrer == null)
                return false;
                
            return _authSettings.WhitelistedReferrers.Any(x => primaryReferrer.StartsWith(x));
        }

        private bool CanAllowIp(string clientIp)
        {
            if (_authSettings.WhitelistedIPs == null)
                return false;
            
            return _authSettings.WhitelistedIPs.Any(ip => ip.EqualsIgnoreCase(clientIp));
        }

        private static bool TryGetBasicAuthCredentials(HttpContext httpContext, out string? username, out string? password)
        {
            username = null;
            password = null;

            if (!httpContext.Request.Headers.TryGetValue(HeaderNames.Authorization, out StringValues authHeaders))
                return false;

            var authHeader = authHeaders.ToString();
            if (!authHeader.StartsWith("Basic"))
                return false;

            var encodedCredentials = authHeader.Substring(6).Trim();
            var decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
            if (!decodedCredentials.Contains(StringConstants.Colon))
                return false;

            var credentialParts = decodedCredentials.Split(CharConstants.Colon);
            if (credentialParts.Length != 2)
                return false;

            username = credentialParts[0];
            password = credentialParts[1];

            return true;
        }

        private void SetUnauthorizedHeader(HttpContext context)
        {
            context.Response.StatusCode = 401;
            context.Response.Headers.Add(HeaderNames.WWWAuthenticate, $"Basic realm=\"{_authSettings.Realm}\"");
        }
    }
}