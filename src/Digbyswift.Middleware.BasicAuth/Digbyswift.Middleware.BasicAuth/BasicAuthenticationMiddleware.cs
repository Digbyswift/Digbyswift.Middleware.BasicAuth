using System.Text;
using Digbyswift.Core.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Digbyswift.Middleware.BasicAuth
{
    public sealed class BasicAuthenticationMiddleware : IMiddleware
    {
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
            if (context.Request.Headers.ContainsKey("AuthBypass") &&
                context.Request.Headers["AuthBypass"].SingleOrDefault() == _authSettings.BypassKey)
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

        private static bool TryGetBasicAuthCredentials(HttpContext httpContext, out string? username,
            out string? password)
        {
            username = null;
            password = null;

            if (!httpContext.Request.Headers.TryGetValue("Authorization", out StringValues authHeaders))
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
            context.Response.Headers.Add("WWW-Authenticate", $"Basic realm=\"{_authSettings.Realm}\"");
        }
    }
}