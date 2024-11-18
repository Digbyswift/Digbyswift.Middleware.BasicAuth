using Microsoft.Extensions.Configuration;

namespace Digbyswift.Middleware.BasicAuth.Tests;

[TestFixture]
public class BasicAuthSettingsTests
{
    private const string TestUsername = "test-user";
    private const string TestPassword = "test-password";
    private const string TestRealm = "test-realm";
    private const string TestBypassKey = "test-bypass-key";
    private static readonly string[] TestExcludedPaths = ["/path1", "/path2"];
    private static readonly string[] TestWhitelistedIPs = ["192.168.1.1", "192.168.1.2"];
    private const string TestWhitelistedReferrer = "https://example.com";

    [TestCase(true)]
    [TestCase(false)]
    public void BasicAuthSettings_PropertiesBoundCorrectly_WhenConfigurationIsValid(bool isEnabled)
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "BasicAuth:Username", TestUsername },
            { "BasicAuth:Password", TestPassword },
            { "BasicAuth:Realm", TestRealm },
            { "BasicAuth:Enabled", isEnabled.ToString() },
            { "BasicAuth:BypassKey", TestBypassKey },
            { "BasicAuth:ExcludedPaths:0", TestExcludedPaths[0] },
            { "BasicAuth:ExcludedPaths:1", TestExcludedPaths[1] },
            { "BasicAuth:WhitelistedIPs:0", TestWhitelistedIPs[0] },
            { "BasicAuth:WhitelistedIPs:1", TestWhitelistedIPs[1] },
            { "BasicAuth:WhitelistedReferrers:0", TestWhitelistedReferrer }
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
            Assert.That(authSettings.ExcludedPaths, Has.Member(TestExcludedPaths[0]));
            Assert.That(authSettings.ExcludedPaths, Has.Member(TestExcludedPaths[1]));
            Assert.That(authSettings.WhitelistedIPs, Has.Exactly(2).Items);
            Assert.That(authSettings.WhitelistedIPs, Has.Member(TestWhitelistedIPs[0]));
            Assert.That(authSettings.WhitelistedIPs, Has.Member(TestWhitelistedIPs[1]));
            Assert.That(authSettings.WhitelistedReferrers, Has.Exactly(1).Items);
            Assert.That(authSettings.WhitelistedReferrers, Has.Member(TestWhitelistedReferrer));
        });
    }
}
