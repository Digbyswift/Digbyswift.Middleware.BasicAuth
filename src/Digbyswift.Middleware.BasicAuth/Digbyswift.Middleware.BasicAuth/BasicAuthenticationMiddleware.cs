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
    private readonly BasicAuthSettings _authSettings;
    private readonly RequestValidator _validator;

    public BasicAuthenticationMiddleware(IOptions<BasicAuthSettings> authSettings)
    {
        _authSettings = authSettings.Value;

        _validator = new RequestValidator(authSettings.Value);
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
        if (_validator.HasByPassKey(context.Request))
            return true;

        if (_validator.CanAllowPath(context.Request))
            return true;

        if (_validator.CanAllowReferrer(context.Request))
            return true;

        if (_validator.CanAllowIp(context.Request))
            return true;

        var authenticateResult = await context.AuthenticateAsync(BasicAuthSettings.AuthenticationSchemeName);
        if (authenticateResult.Succeeded)
            return true;

        if (!_validator.TryGetBasicAuthCredentials(context.Request, out var username, out var password))
            return false;

        return _validator.CheckCredentials(username, password);
    }

    private void SetUnauthorizedHeader(HttpContext context)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Response.Headers.Add(HeaderNames.WWWAuthenticate, $"Basic realm=\"{_authSettings.Realm}\"");
    }
}
