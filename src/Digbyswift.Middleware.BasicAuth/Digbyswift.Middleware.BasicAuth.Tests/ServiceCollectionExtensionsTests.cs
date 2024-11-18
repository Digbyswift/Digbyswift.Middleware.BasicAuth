using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Digbyswift.Middleware.BasicAuth.Tests;

public class ServiceCollectionExtensionsTests
{
    [Test]
    public void AddBasicAuthentication_RegistersServices_WhenIsEnabledIsTrue()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { $"{BasicAuthSettings.SectionName}:Enabled", "true" },
                { $"{BasicAuthSettings.SectionName}:Username", "test-user" },
                { $"{BasicAuthSettings.SectionName}:Password", "test-pass" }
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        services.AddBasicAuthentication();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // Verify BasicAuthSettings are configured
        var options = serviceProvider.GetService<IOptions<BasicAuthSettings>>();
        Assert.That(options, Is.Not.Null);
        Assert.That(options?.Value.IsEnabled, Is.True);

        // Verify AuthenticationOptions has a custom scheme
        var authOptions = serviceProvider.GetService<IOptions<AuthenticationOptions>>();
        Assert.That(authOptions, Is.Not.Null);
        Assert.That(authOptions?.Value.SchemeMap, Contains.Key(BasicAuthSettings.AuthenticationSchemeName));

        // Verify BasicAuthenticationMiddleware is registered
        var middleware = serviceProvider.GetService<BasicAuthenticationMiddleware>();
        Assert.That(middleware, Is.Not.Null);
    }

    [Test]
    public void AddBasicAuthentication_DoesNotRegisterServices_WhenIsEnabledIsFalse()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { $"{BasicAuthSettings.SectionName}:Enabled", Boolean.FalseString },
                { $"{BasicAuthSettings.SectionName}:Username", "test-user" },
                { $"{BasicAuthSettings.SectionName}:Password", "test-pass" }
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);

        // Act
        services.AddBasicAuthentication();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // Verify BasicAuthSettings are not configured
        var options = serviceProvider.GetService<IOptions<BasicAuthSettings>>();
        Assert.That(options, Is.Null);

        // Verify AuthenticationOptions is not configured
        var authOptions = serviceProvider.GetService<IOptions<AuthenticationOptions>>();
        Assert.That(authOptions, Is.Null);

        // Verify BasicAuthenticationMiddleware is not registered
        var middleware = serviceProvider.GetService<BasicAuthenticationMiddleware>();
        Assert.That(middleware, Is.Null);
    }
}
