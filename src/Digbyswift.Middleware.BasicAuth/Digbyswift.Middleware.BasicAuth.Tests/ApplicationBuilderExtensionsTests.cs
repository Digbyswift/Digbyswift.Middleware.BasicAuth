using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Digbyswift.Middleware.BasicAuth.Tests;

[TestFixture]
public class ApplicationBuilderExtensionsTests
{
    private ServiceCollection _services;
    private ServiceProvider _provider;
    private IApplicationBuilder _app;

    [SetUp]
    public void SetUp()
    {
        _services = [];
        _provider = _services.BuildServiceProvider();
        _app = Substitute.For<IApplicationBuilder>();
        _app.ApplicationServices.Returns(_provider);
    }

    [TearDown]
    public void TearDown()
    {
        _provider.Dispose();
    }

    #region UseBasicHttpAuthentication

    [Test]
    public void UseBasicHttpAuthentication_ReturnsBuilder_WhenAuthIsDisabled()
    {
        // Arrange
        _services.AddOptions<BasicAuthSettings>().Configure(o => o.IsEnabled = false);
        _provider = _services.BuildServiceProvider();
        _app.ApplicationServices.Returns(_provider);

        // Act
        var result = _app.UseBasicHttpAuthentication();

        // Assert
        Assert.That(result, Is.EqualTo(_app));

        // Unable to verify UseMiddleware since it is an extension method
        _app.Received(0).Use(Arg.Any<Func<RequestDelegate, RequestDelegate>>());
    }

    [Test]
    public void UseBasicHttpAuthentication_UsesMiddleware_WhenAuthIsEnabled()
    {
        // Arrange
        _services.AddOptions<BasicAuthSettings>().Configure(o => o.IsEnabled = true);
        _provider = _services.BuildServiceProvider();
        _app.ApplicationServices.Returns(_provider);

        // Act
        _app.UseBasicHttpAuthentication();

        // Assert
        // Unable to verify UseMiddleware since it is an extension method
        _app.Received(1).Use(Arg.Any<Func<RequestDelegate, RequestDelegate>>());
    }

    [Test]
    public void UseBasicHttpAuthentication_DoesNotUseMiddleware_WhenOptionsNotRegistered()
    {
        // Arrange
        _provider = _services.BuildServiceProvider();
        _app.ApplicationServices.Returns(_provider);

        // Act
        var result = _app.UseBasicHttpAuthentication();

        // Assert
        Assert.That(result, Is.EqualTo(_app));

        // Unable to verify UseMiddleware since it is an extension method
        _app.Received(0).Use(Arg.Any<Func<RequestDelegate, RequestDelegate>>());
    }

    #endregion
}
