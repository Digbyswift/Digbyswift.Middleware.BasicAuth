using System.Net;
using Digbyswift.Core.Constants;
using Digbyswift.Core.Extensions;
using Digbyswift.Http.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Digbyswift.Middleware.BasicAuth.Tests;

[TestFixture]
public class RequestValidatorTests
{
    private const string TestBypassKey = "test-bypass-key";
    private const string TestClientIp = "192.168.1.10";
    private const string NonWhitelistedIp = "10.0.0.1";
    private const string AllowedReferrerPrefix = "https://allowed.example.com";
    private const string DisallowedReferrerPrefix = "https://disallowed.example.com";
    private const string MatchingExcludedPath = "/health";
    private const string NonMatchingPath = "/some/other/path";
    private const string ValidUsername = "user1";
    private const string ValidPassword = "pass1";

    private DefaultHttpContext _httpContext = null!;
    private BasicAuthSettings _authSettings = null!;
    private RequestValidator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _httpContext = new DefaultHttpContext();
        _authSettings = new BasicAuthSettings
        {
            Username = ValidUsername,
            Password = ValidPassword
        };
        _sut = new RequestValidator(_authSettings);
    }

    #region HasByPassKey

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    public void HasByPassKey_ReturnsFalse_WhenBypassKeyIsNullOrWhitespace(string? bypassKey)
    {
        // Arrange
        _authSettings.BypassKey = bypassKey;

        var request = _httpContext.Request;
        request.Headers[RequestValidator.AuthBypassHeaderName] = new StringValues(TestBypassKey);

        // Act
        var result = _sut.HasByPassKey(request);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void HasByPassKey_ReturnsFalse_WhenHeaderIsMissing()
    {
        // Arrange
        _authSettings.BypassKey = TestBypassKey;
        var request = _httpContext.Request;

        // Act
        var result = _sut.HasByPassKey(request);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void HasByPassKey_ReturnsTrue_WhenHeaderMatchesBypassKey()
    {
        // Arrange
        _authSettings.BypassKey = TestBypassKey;

        var request = _httpContext.Request;
        request.Headers[RequestValidator.AuthBypassHeaderName] = new StringValues(TestBypassKey);

        // Act
        var result = _sut.HasByPassKey(request);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void HasByPassKey_ReturnsFalse_WhenHeaderDoesNotMatchBypassKey()
    {
        // Arrange
        const string differentKey = "different-key";

        _authSettings.BypassKey = TestBypassKey;

        var request = _httpContext.Request;
        request.Headers[RequestValidator.AuthBypassHeaderName] = new StringValues(differentKey);

        // Act
        var result = _sut.HasByPassKey(request);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region CanAllowPath

    [Test]
    public void CanAllowPath_ReturnsFalse_WhenExcludedPathsIsEmpty()
    {
        var request = _httpContext.Request;
        request.Path = MatchingExcludedPath;

        // Act
        var result = _sut.CanAllowPath(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_authSettings.ExcludedPaths, Is.Empty);
            Assert.That(result, Is.False);
        });
    }

    [Test]
    public void CanAllowPath_ReturnsFalse_WhenPathDoesNotMatchAnyExcludedPath()
    {
        // Arrange
        _authSettings.ExcludedPaths = [MatchingExcludedPath];

        var request = _httpContext.Request;
        request.Path = NonMatchingPath;

        // Act
        var result = _sut.CanAllowPath(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_authSettings.ExcludedPaths, Has.Exactly(1).Items);
            Assert.That(result, Is.False);
        });
    }

    [TestCase(MatchingExcludedPath + " ")]
    [TestCase(MatchingExcludedPath + "-something-else")]
    public void CanAllowPath_ReturnsFalse_WhenPathSegmentsDoNotStartWithExcludedPath(string path)
    {
        // Arrange
        _authSettings.ExcludedPaths = [MatchingExcludedPath];

        var request = _httpContext.Request;
        request.Path = path;

        // Act
        var result = _sut.CanAllowPath(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_authSettings.ExcludedPaths, Has.Exactly(1).Items);
            Assert.That(result, Is.False);
        });
    }

    [Test]
    public void CanAllowPath_ReturnsTrue_WhenPathEqualsExcludedPath()
    {
        // Arrange
        _authSettings.ExcludedPaths = [MatchingExcludedPath];

        var request = _httpContext.Request;
        request.Path = MatchingExcludedPath;

        // Act
        var result = _sut.CanAllowPath(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_authSettings.ExcludedPaths, Has.Exactly(1).Items);
            Assert.That(result, Is.True);
        });
    }

    [TestCase(MatchingExcludedPath + "/")]
    [TestCase(MatchingExcludedPath + "/status")]
    [TestCase(MatchingExcludedPath + "/status/")]
    [TestCase(MatchingExcludedPath + "/%20 ")]
    public void CanAllowPath_ReturnsTrue_WhenPathStartsWithExcludedPath(string path)
    {
        // Arrange
        _authSettings.ExcludedPaths = [MatchingExcludedPath];

        var request = _httpContext.Request;
        request.Path = path;

        // Act
        var result = _sut.CanAllowPath(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_authSettings.ExcludedPaths, Has.Exactly(1).Items);
            Assert.That(result, Is.True);
        });
    }

    #endregion

    #region CanAllowReferrer

    [Test]
    public void CanAllowReferrer_ReturnsFalse_WhenReferrerHeaderIsMissing()
    {
        // Arrange
        _authSettings.WhitelistedReferrers = [AllowedReferrerPrefix];
        var request = _httpContext.Request;

        // Act
        var result = _sut.CanAllowReferrer(request);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void CanAllowReferrer_ReturnsTrue_WhenReferrerStartsWithWhitelistedPrefix()
    {
        // Arrange
        _authSettings.WhitelistedReferrers = [AllowedReferrerPrefix];

        var request = _httpContext.Request;
        request.Headers[HeaderNames.Referer] = $"{AllowedReferrerPrefix}/page";

        // Act
        var result = _sut.CanAllowReferrer(request);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void CanAllowReferrer_ReturnsFalse_WhenReferrerDoesNotStartWithWhitelistedPrefix()
    {
        // Arrange
        _authSettings.WhitelistedReferrers = [AllowedReferrerPrefix];

        var request = _httpContext.Request;
        request.Headers[HeaderNames.Referer] = $"{DisallowedReferrerPrefix}/page";

        // Act
        var result = _sut.CanAllowReferrer(request);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region CanAllowIp

    [Test]
    public void CanAllowIp_ReturnsFalse_WhenClientIpIsNull()
    {
        // Arrange
        _authSettings.WhitelistedIPs = [TestClientIp];

        var request = _httpContext.Request;
        _httpContext.Connection.RemoteIpAddress = null;

        // Act
        var result = _sut.CanAllowIp(request);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void CanAllowIp_ReturnsFalse_WhenClientIpIsWhitespace()
    {
        // Arrange
        _authSettings.WhitelistedIPs = [TestClientIp];

        var request = _httpContext.Request;

        // If GetClientIp() implementation relies on headers, simulate whitespace value
        request.Headers[NonStandardHeaderNames.XForwardedFor] = StringConstants.Space;

        // Act
        var result = _sut.CanAllowIp(request);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void CanAllowIp_ReturnsTrue_WhenClientIpIsWhitelisted()
    {
        // Arrange
        _authSettings.WhitelistedIPs = [TestClientIp];

        var request = _httpContext.Request;
        _httpContext.Connection.RemoteIpAddress = IPAddress.Parse(TestClientIp);

        // Act
        var result = _sut.CanAllowIp(request);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void CanAllowIp_ReturnsFalse_WhenClientIpIsNotWhitelisted()
    {
        // Arrange
        _authSettings.WhitelistedIPs = [TestClientIp];

        var request = _httpContext.Request;
        _httpContext.Connection.RemoteIpAddress = IPAddress.Parse(NonWhitelistedIp);

        // Act
        var result = _sut.CanAllowIp(request);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region TryGetBasicAuthCredentials

    [Test]
    public void TryGetBasicAuthCredentials_ReturnsFalse_WhenAuthorizationHeaderMissing()
    {
        // Arrange
        var request = _httpContext.Request;

        // Act
        var result = _sut.TryGetBasicAuthCredentials(request, out var username, out var password);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(username, Is.Null);
            Assert.That(password, Is.Null);
        });
    }

    [TestCase("")]
    [TestCase("Bearer ")]
    public void TryGetBasicAuthCredentials_ReturnsFalse_WhenHeaderDoesNotStartWithBasicPrefix(string prefix)
    {
        // Arrange
        const string validCredentials = $"{ValidUsername}:{ValidPassword}";
        var encodedToken = validCredentials.Base64Encode();

        var request = _httpContext.Request;
        request.Headers[HeaderNames.Authorization] = $"{prefix} {encodedToken}";

        // Act
        var result = _sut.TryGetBasicAuthCredentials(request, out var username, out var password);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(username, Is.Null);
            Assert.That(password, Is.Null);
        });
    }

    [Test]
    public void TryGetBasicAuthCredentials_ReturnsFalse_WhenDecodedCredentialsContainNoColon()
    {
        // Arrange
        const string invalidCredentials = $"{ValidUsername}{ValidPassword}";
        var encodedToken = invalidCredentials.Base64Encode();

        var request = _httpContext.Request;
        request.Headers[HeaderNames.Authorization] = $"{RequestValidator.AuthHeaderNamePrefix} {encodedToken}";

        // Act
        var result = _sut.TryGetBasicAuthCredentials(request, out var username, out var password);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(username, Is.Null);
            Assert.That(password, Is.Null);
        });
    }

    [TestCase(":")]
    [TestCase(":a")]
    [TestCase("::")]
    [TestCase(":a:a")]
    public void TryGetBasicAuthCredentials_ReturnsFalse_WhenMoreThanTwoPartsAfterSplit(string additionalCredentialPart)
    {
        // Arrange
        var invalidCredentials = $"{ValidUsername}:{ValidPassword}{additionalCredentialPart}";
        var encodedToken = invalidCredentials.Base64Encode();

        var request = _httpContext.Request;
        request.Headers[HeaderNames.Authorization] = $"{RequestValidator.AuthHeaderNamePrefix} {encodedToken}";

        // Act
        var result = _sut.TryGetBasicAuthCredentials(request, out var username, out var password);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(username, Is.Null);
            Assert.That(password, Is.Null);
        });
    }

    [Test]
    public void TryGetBasicAuthCredentials_ReturnsTrue_WhenCredentialsAreValid()
    {
        // Arrange
        const string invalidCredentials = $"{ValidUsername}:{ValidPassword}";
        var encodedToken = invalidCredentials.Base64Encode();

        var request = _httpContext.Request;
        request.Headers[HeaderNames.Authorization] = $"{RequestValidator.AuthHeaderNamePrefix}:{encodedToken}";

        // Act
        var result = _sut.TryGetBasicAuthCredentials(request, out var username, out var password);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(username, Is.EqualTo(ValidUsername));
            Assert.That(password, Is.EqualTo(ValidPassword));
        });
    }

    #endregion

    #region CheckCredentials

    [TestCase(null, ValidPassword)]
    [TestCase("", ValidPassword)]
    [TestCase(" ", ValidPassword)]
    [TestCase(ValidUsername, null)]
    [TestCase(ValidUsername, "")]
    [TestCase(ValidUsername, " ")]
    public void CheckCredentials_ReturnsFalse_WhenUsernameOrPasswordIsNullOrWhitespace(string? username, string? password)
    {
        // Act
        var result = _sut.CheckCredentials(username, password);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void CheckCredentials_ReturnsFalse_WhenUsernameDoesNotMatchIgnoringCase()
    {
        // Arrange
        const string wrongUsername = "wrongUser";

        // Act
        var result = _sut.CheckCredentials(wrongUsername, ValidPassword);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void CheckCredentials_ReturnsFalse_WhenPasswordDoesNotMatch()
    {
        // Arrange
        const string wrongPassword = "incorrect";

        // Act
        var result = _sut.CheckCredentials(ValidUsername, wrongPassword);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void CheckCredentials_ReturnsTrue_WhenUsernameMatchesIgnoringCase_AndPasswordMatches()
    {
        // Arrange
        const string usernameDifferentCase = "USER1";

        // Act
        var result = _sut.CheckCredentials(usernameDifferentCase, ValidPassword);

        // Assert
        Assert.That(result, Is.True);
    }

    #endregion
}
