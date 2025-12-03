using Digbyswift.Core.Constants;
using Microsoft.Extensions.Configuration;

namespace Digbyswift.Middleware.BasicAuth;

public sealed class BasicAuthSettings
{
    public const string SectionName = "BasicAuth";
    public const string AuthenticationSchemeName = "ProtectedSiteScheme";
    public const string DefaultRealm = "Default";

    public string? Username { get; set; }
    public string? Password { get; set; }
    public string Realm { get; set; } = DefaultRealm;

    [ConfigurationKeyName("Enabled")]
    public bool IsEnabled { get; set; }
    public string? BypassKey { get; set; }

    private IEnumerable<string>? _excludedPaths;
    public IEnumerable<string> ExcludedPaths
    {
        get => _excludedPaths?.Where(x => x.StartsWith(CharConstants.ForwardSlash)) ?? [];
        set => _excludedPaths = value;
    }

    public IEnumerable<string> WhitelistedIPs { get; set; } = [];
    public IEnumerable<string> WhitelistedReferrers { get; set; } = [];
}
