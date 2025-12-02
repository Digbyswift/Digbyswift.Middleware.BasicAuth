using Microsoft.Extensions.Configuration;

namespace Digbyswift.Middleware.BasicAuth.Tests;

[TestFixture]
public class BasicAuthSettingsTests
{
    private const string TestUsername = "test-user";
    private const string TestPassword = "test-password";
    private const string TestRealm = "test-realm";
    private const string TestBypassKey = "test-bypass-key";

    private static readonly string[] _testExcludedPaths = ["/path1", "/path2"];
    private static readonly string[] _testWhitelistedIPs = ["192.168.1.1", "192.168.1.2"];
    private static readonly string[] _testWhitelistedReferrers = ["https://example.com", "https://www.example.com"];

    [Test]
    public void BasicAuthSettings_PropertiesAreCorrect_WhenConfigurationIsMissing()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build();

        var authSettings = new BasicAuthSettings();

        // Act
        configuration.GetSection(BasicAuthSettings.SectionName).Bind(authSettings);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(authSettings.Username, Is.Null);
            Assert.That(authSettings.Password, Is.Null);
            Assert.That(authSettings.Realm, Is.EqualTo(BasicAuthSettings.DefaultRealm));
            Assert.That(authSettings.IsEnabled, Is.False);
            Assert.That(authSettings.BypassKey, Is.Null);
            Assert.That(authSettings.ExcludedPaths, Is.Empty);
            Assert.That(authSettings.WhitelistedIPs, Is.Empty);
            Assert.That(authSettings.WhitelistedReferrers, Is.Empty);
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public void BasicAuthSettings_PropertiesBoundCorrectly_WhenConfigurationIsValid(bool isEnabled)
    {
        // Arrange - Colon format is used here to ensure the
        // configuration is parsed correctly. Double underscore
        // will not work with an in-memory collection.
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "BasicAuth:Username", TestUsername },
            { "BasicAuth:Password", TestPassword },
            { "BasicAuth:Realm", TestRealm },
            { "BasicAuth:Enabled", isEnabled.ToString() },
            { "BasicAuth:BypassKey", TestBypassKey },
            { "BasicAuth:ExcludedPaths:0", _testExcludedPaths[0] },
            { "BasicAuth:ExcludedPaths:1", _testExcludedPaths[1] },
            { "BasicAuth:WhitelistedIPs:0", _testWhitelistedIPs[0] },
            { "BasicAuth:WhitelistedIPs:1", _testWhitelistedIPs[1] },
            { "BasicAuth:WhitelistedReferrers:0", _testWhitelistedReferrers[0] },
            { "BasicAuth:WhitelistedReferrers:1", _testWhitelistedReferrers[1] }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var authSettings = new BasicAuthSettings();

        // Act
        configuration.GetSection(BasicAuthSettings.SectionName).Bind(authSettings);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(authSettings.Username, Is.EqualTo(TestUsername));
            Assert.That(authSettings.Password, Is.EqualTo(TestPassword));
            Assert.That(authSettings.Realm, Is.EqualTo(TestRealm));
            Assert.That(authSettings.IsEnabled, Is.EqualTo(isEnabled));
            Assert.That(authSettings.BypassKey, Is.EqualTo(TestBypassKey));
            Assert.That(authSettings.ExcludedPaths, Has.Exactly(2).Items);
            Assert.That(authSettings.ExcludedPaths, Has.Member(_testExcludedPaths[0]));
            Assert.That(authSettings.ExcludedPaths, Has.Member(_testExcludedPaths[1]));
            Assert.That(authSettings.WhitelistedIPs, Has.Exactly(2).Items);
            Assert.That(authSettings.WhitelistedIPs, Has.Member(_testWhitelistedIPs[0]));
            Assert.That(authSettings.WhitelistedIPs, Has.Member(_testWhitelistedIPs[1]));
            Assert.That(authSettings.WhitelistedReferrers, Has.Exactly(2).Items);
            Assert.That(authSettings.WhitelistedReferrers, Has.Member(_testWhitelistedReferrers[0]));
            Assert.That(authSettings.WhitelistedReferrers, Has.Member(_testWhitelistedReferrers[1]));
        });
    }
}
