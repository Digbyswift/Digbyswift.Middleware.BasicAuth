using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Digbyswift.Middleware.BasicAuth.Tests;

[TestFixture]
public class BasicAuthenticationMiddlewareTests
{
    private const string TestRealm = "TestRealm";
    private const string ValidUsername = "testuser";
    private const string ValidPassword = "testpass";
    private const string BypassKey = "bypass-key";
    private const string ExcludedPath = "/excluded";
    private const string WhitelistedReferrer = "https://trusted.com";
    private const string WhitelistedIP = "127.0.0.1";

    private BasicAuthSettings _authSettings = null!;
    private RequestDelegate _nextMock = null!;
    private DefaultHttpContext _httpContext = null!;

    [SetUp]
    public void SetUp()
    {
        _authSettings = new BasicAuthSettings
        {
            Username = ValidUsername,
            Password = ValidPassword,
            BypassKey = BypassKey,
            ExcludedPaths = [ExcludedPath],
            WhitelistedReferrers = [WhitelistedReferrer],
            WhitelistedIPs = [WhitelistedIP],
            Realm = TestRealm
        };

        _nextMock = Substitute.For<RequestDelegate>();
        _httpContext = new DefaultHttpContext();
    }

    [Test]
    public async Task InvokeAsync_ProceedsToNextMiddleware_WhenBypassKeyIsPresent()
    {
        // Arrange
        _httpContext.Request.Headers["X-Basic-Auth-Bypass"] = BypassKey;
        var middleware = new BasicAuthenticationMiddleware(Options.Create(_authSettings));

        // Act
        await middleware.InvokeAsync(_httpContext, _nextMock);

        // Assert
        await _nextMock.Received(1).Invoke(_httpContext);
    }

    [Test]
    public async Task InvokeAsync_ProceedsToNextMiddleware_WhenMatchedExcludedPathIsPresent()
    {
        // Arrange
        _httpContext.Request.Path = ExcludedPath;
        var middleware = new BasicAuthenticationMiddleware(Options.Create(_authSettings));

        // Act
        await middleware.InvokeAsync(_httpContext, _nextMock);

        // Assert
        await _nextMock.Received(1).Invoke(_httpContext);
    }

    [Test]
    public async Task InvokeAsync_ProceedsToNextMiddleware_WhenMatchedWhitelistedReferrerIsPresent()
    {
        // Arrange
        _httpContext.Request.GetTypedHeaders().Referer = new Uri(WhitelistedReferrer, UriKind.Absolute);
        var middleware = new BasicAuthenticationMiddleware(Options.Create(_authSettings));

        // Act
        await middleware.InvokeAsync(_httpContext, _nextMock);

        // Assert
        await _nextMock.Received(1).Invoke(_httpContext);
    }

    [Test]
    public async Task InvokeAsync_ProceedsToNextMiddleware_WhenMatchedWhitelistedIPIsPresent()
    {
        // Arrange
        _httpContext.Connection.RemoteIpAddress = IPAddress.Parse(WhitelistedIP);
        var middleware = new BasicAuthenticationMiddleware(Options.Create(_authSettings));

        // Act
        await middleware.InvokeAsync(_httpContext, _nextMock);

        // Assert
        await _nextMock.Received(1).Invoke(_httpContext);
    }

    [Test]
    public async Task InvokeAsync_ReturnsUnauthorized_WhenAuthenticationSchemeNameIsRecognised()
    {
        // Arrange
        var failedSchemeResult = AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(), BasicAuthSettings.AuthenticationSchemeName));
        var authServiceMock = Substitute.For<IAuthenticationService>();
        authServiceMock.AuthenticateAsync(Arg.Any<HttpContext>(), BasicAuthSettings.AuthenticationSchemeName).Returns(Task.FromResult(failedSchemeResult));

        var serviceProviderMock = Substitute.For<IServiceProvider>();
        serviceProviderMock.GetService(typeof(IAuthenticationService)).Returns(authServiceMock);

        _httpContext.RequestServices = serviceProviderMock;
        var middleware = new BasicAuthenticationMiddleware(Options.Create(_authSettings));

        // Act
        await middleware.InvokeAsync(_httpContext, _nextMock);

        // Assert
        await Assert.MultipleAsync(async () =>
        {
            Assert.That(_httpContext.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
            Assert.That(_httpContext.Response.Headers.ContainsKey("WWW-Authenticate"), Is.False);
            await _nextMock.Received(1).Invoke(_httpContext);
        });
    }

    [Test]
    public async Task InvokeAsync_ReturnsUnauthorized_WhenAuthenticationSchemeNameFailsAndCredentialsAreInvalid()
    {
        // Arrange
        var failedSchemeResult = AuthenticateResult.Fail("Test failed");
        var authServiceMock = Substitute.For<IAuthenticationService>();
        authServiceMock.AuthenticateAsync(Arg.Any<HttpContext>(), BasicAuthSettings.AuthenticationSchemeName).Returns(Task.FromResult(failedSchemeResult));

        var serviceProviderMock = Substitute.For<IServiceProvider>();
        serviceProviderMock.GetService(typeof(IAuthenticationService)).Returns(authServiceMock);

        _httpContext.RequestServices = serviceProviderMock;
        _httpContext.Request.Headers["Authorization"] = "Basic " + Convert.ToBase64String("invaliduser:invalidpass"u8);

        var middleware = new BasicAuthenticationMiddleware(Options.Create(_authSettings));

        // Act
        await middleware.InvokeAsync(_httpContext, _nextMock);

        // Assert
        await Assert.MultipleAsync(async () =>
        {
            Assert.That(_httpContext.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
            Assert.That(_httpContext.Response.Headers.ContainsKey("WWW-Authenticate"), Is.True);
            await _nextMock.DidNotReceive().Invoke(_httpContext);
        });
    }

    [Test]
    public async Task InvokeAsync_ReturnsUnauthorized_WhenAuthenticationSchemeNameHasNoInformationAndCredentialsAreInvalid()
    {
        // Arrange
        var succeededSchemeResult = AuthenticateResult.NoResult();
        var authServiceMock = Substitute.For<IAuthenticationService>();
        authServiceMock.AuthenticateAsync(Arg.Any<HttpContext>(), BasicAuthSettings.AuthenticationSchemeName).Returns(Task.FromResult(succeededSchemeResult));

        var serviceProviderMock = Substitute.For<IServiceProvider>();
        serviceProviderMock.GetService(typeof(IAuthenticationService)).Returns(authServiceMock);

        _httpContext.RequestServices = serviceProviderMock;
        _httpContext.Request.Headers["Authorization"] = "Basic " + Convert.ToBase64String("invaliduser:invalidpass"u8);

        var middleware = new BasicAuthenticationMiddleware(Options.Create(_authSettings));

        // Act
        await middleware.InvokeAsync(_httpContext, _nextMock);

        // Assert
        await Assert.MultipleAsync(async () =>
        {
            Assert.That(_httpContext.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
            Assert.That(_httpContext.Response.Headers.ContainsKey("WWW-Authenticate"), Is.True);
            await _nextMock.DidNotReceive().Invoke(_httpContext);
        });
    }

    [Test]
    public async Task InvokeAsync_ProceedsToNextMiddleware_WhenCredentialsAreValid()
    {
        // Arrange
        var succeededSchemeResult = AuthenticateResult.NoResult();
        var authServiceMock = Substitute.For<IAuthenticationService>();
        authServiceMock.AuthenticateAsync(Arg.Any<HttpContext>(), BasicAuthSettings.AuthenticationSchemeName).Returns(Task.FromResult(succeededSchemeResult));

        var serviceProviderMock = Substitute.For<IServiceProvider>();
        serviceProviderMock.GetService(typeof(IAuthenticationService)).Returns(authServiceMock);

        _httpContext.RequestServices = serviceProviderMock;
        _httpContext.Request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ValidUsername}:{ValidPassword}"));
        var middleware = new BasicAuthenticationMiddleware(Options.Create(_authSettings));

        // Act
        await middleware.InvokeAsync(_httpContext, _nextMock);

        await Assert.MultipleAsync(async () =>
        {
            // Assert
            Assert.That(_httpContext.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
            Assert.That(_httpContext.Response.Headers.ContainsKey("WWW-Authenticate"), Is.False);
            await _nextMock.Received(1).Invoke(_httpContext);
        });
    }
}
