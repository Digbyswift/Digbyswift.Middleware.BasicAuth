using Digbyswift.Core.Constants;
using Digbyswift.Core.Extensions;
using Digbyswift.Http.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Digbyswift.Middleware.BasicAuth;

public class RequestValidator
{
    internal const string AuthBypassHeaderName = "X-Basic-Auth-Bypass";
    internal const string AuthHeaderNamePrefix = "Basic";

    private readonly BasicAuthSettings _authSettings;

    public RequestValidator(BasicAuthSettings authSettings)
    {
        _authSettings = authSettings;
    }

    public bool HasByPassKey(HttpRequest request)
    {
        if (String.IsNullOrWhiteSpace(_authSettings.BypassKey))
            return false;

        if (request.Headers[AuthBypassHeaderName] == StringValues.Empty)
            return false;

        return request.Headers[AuthBypassHeaderName][0] == _authSettings.BypassKey;
    }

    public bool CanAllowPath(HttpRequest request)
    {
        var path = request.Path;
        return _authSettings.ExcludedPaths.Any(excludedPath => path.StartsWithSegments(excludedPath));
    }

    public bool CanAllowReferrer(HttpRequest request)
    {
        if (!request.TryGetReferrer(out var referrer) || referrer == null)
            return false;

        return _authSettings.WhitelistedReferrers.Any(x => referrer.ToString().StartsWith(x));
    }

    public bool CanAllowIp(HttpRequest request)
    {
        var clientIp = request.GetClientIp()?.ToString();
        if (String.IsNullOrWhiteSpace(clientIp))
            return false;

        return _authSettings.WhitelistedIPs.Any(ip => ip.EqualsIgnoreCase(clientIp));
    }

    public bool TryGetBasicAuthCredentials(HttpRequest request, out string? username, out string? password)
    {
        username = null;
        password = null;

        if (!request.Headers.TryGetValue(HeaderNames.Authorization, out var authHeaders))
            return false;

        var authHeader = authHeaders.ToString();
        if (!authHeader.StartsWith(AuthHeaderNamePrefix))
            return false;

        var encodedCredentials = authHeader[6..].Trim();
        var decodedCredentials = encodedCredentials.Base64Decode();
        if (!decodedCredentials.Contains(CharConstants.Colon))
            return false;

        var credentialParts = decodedCredentials.Split(CharConstants.Colon);
        if (credentialParts.Length != 2)
            return false;

        username = credentialParts[0];
        password = credentialParts[1];

        return true;
    }

    public bool CheckCredentials(string? username, string? password)
    {
        if (String.IsNullOrWhiteSpace(username) || String.IsNullOrWhiteSpace(password))
            return false;

        if (!username.EqualsIgnoreCase(_authSettings.Username!))
            return false;

        return password == _authSettings.Password;
    }
}
