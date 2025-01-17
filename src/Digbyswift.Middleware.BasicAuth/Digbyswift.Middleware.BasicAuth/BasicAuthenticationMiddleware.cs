using System.Net;
using Digbyswift.Core.Constants;
using Digbyswift.Core.Extensions;
using Digbyswift.Http.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Digbyswift.Middleware.BasicAuth;

public sealed class BasicAuthenticationMiddleware : IMiddleware
{
    private const string AuthBypassHeaderName = "X-Basic-Auth-Bypass";

    private readonly BasicAuthSettings _authSettings;

    public BasicAuthenticationMiddleware(IOptions<BasicAuthSettings> authSettings)
    {
        _authSettings = authSettings.Value;
    }

    /// <inheritdoc />
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!await TryAuthenticateAsync(context))
        {
            SetUnauthorizedHeader(context);
            return;
        }

        await next(context);
    }

    private async Task<bool> TryAuthenticateAsync(HttpContext context)
    {
        if (HasByPassKey(context.Request))
            return true;

        if (CanAllowPath(context.Request))
            return true;

        if (CanAllowReferrer(context.Request))
            return true;

        if (CanAllowIp(context.Request))
            return true;

        var authenticateResult = await context.AuthenticateAsync(BasicAuthSettings.AuthenticationSchemeName);
        if (authenticateResult.Succeeded)
            return true;

        if (!TryGetBasicAuthCredentials(context, out var username, out var password) || String.IsNullOrWhiteSpace(username) || String.IsNullOrWhiteSpace(password))
            return false;

        return username.EqualsIgnoreCase(_authSettings.Username!) && password == _authSettings.Password;
    }

    private bool HasByPassKey(HttpRequest request)
    {
        if (String.IsNullOrWhiteSpace(_authSettings.BypassKey))
            return false;

        if (request.Headers[AuthBypassHeaderName] == StringValues.Empty)
            return false;

        return request.Headers[AuthBypassHeaderName][0] == _authSettings.BypassKey;
    }

    private bool CanAllowPath(HttpRequest request)
    {
        if (_authSettings.ExcludedPaths == null)
            return false;

        var path = request.Path;
        return _authSettings.ExcludedPaths.Any(excludedPath => path.StartsWithSegments(excludedPath));
    }

    private bool CanAllowReferrer(HttpRequest request)
    {
        if (_authSettings.WhitelistedReferrers == null)
            return false;

        if (!request.TryGetReferrer(out var referrer) || referrer == null)
            return false;

        return _authSettings.WhitelistedReferrers.Any(x => referrer.ToString().StartsWith(x));
    }

    private bool CanAllowIp(HttpRequest request)
    {
        if (_authSettings.WhitelistedIPs == null)
            return false;

        var clientIp = request.GetClientIp()?.ToString();
        if (String.IsNullOrWhiteSpace(clientIp))
            return false;

        return _authSettings.WhitelistedIPs.Any(ip => ip.EqualsIgnoreCase(clientIp));
    }

    private static bool TryGetBasicAuthCredentials(HttpContext httpContext, out string? username, out string? password)
    {
        username = null;
        password = null;

        if (!httpContext.Request.Headers.TryGetValue(HeaderNames.Authorization, out var authHeaders))
            return false;

        var authHeader = authHeaders.ToString();
        if (!authHeader.StartsWith("Basic"))
            return false;

        var encodedCredentials = authHeader.Substring(6).Trim();
        var decodedCredentials = encodedCredentials.Base64Decode();
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
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Response.Headers.Add(HeaderNames.WWWAuthenticate, $"Basic realm=\"{_authSettings.Realm}\"");
    }
}
