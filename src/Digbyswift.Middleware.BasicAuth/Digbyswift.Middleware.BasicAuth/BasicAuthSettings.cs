using Digbyswift.Core.Constants;
using Microsoft.Extensions.Configuration;

namespace Digbyswift.Middleware.BasicAuth
{
public sealed class BasicAuthSettings
{
    public const string SectionName = "BasicAuth";
    public const string AuthenticationSchemeName = "ProtectedSiteScheme";
    public const string DefaultRealm = "Default";

    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Realm { get; set; } = DefaultRealm;

    [ConfigurationKeyName("Enabled")]
    public bool IsEnabled { get; set; }
    public string? BypassKey { get; set; }

    public IEnumerable<string> ExcludedPaths
    {
        get => field?.Where(x => x.StartsWith(CharConstants.ForwardSlash)) ?? [];
        set;
    }

    public IEnumerable<string> WhitelistedIPs { get; set; } = [];
    public IEnumerable<string> WhitelistedReferrers { get; set; } = [];
}
}
