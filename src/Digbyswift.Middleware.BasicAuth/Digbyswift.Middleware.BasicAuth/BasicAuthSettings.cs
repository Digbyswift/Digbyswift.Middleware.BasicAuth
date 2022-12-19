using Microsoft.Extensions.Configuration;

namespace Digbyswift.Middleware.BasicAuth
{
    public sealed class BasicAuthSettings
    {
        public static readonly string SectionName = "BasicAuth";
        public static readonly string AuthenticationSchemeName = "ProtectedSiteScheme";
        public static readonly string DefaultRealm = "Gated site login";

        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Realm { get; set; } = DefaultRealm;

        [ConfigurationKeyName("Enabled")]
        public bool IsEnabled { get; set; }
        public string? BypassKey { get; set; }
    }
}
