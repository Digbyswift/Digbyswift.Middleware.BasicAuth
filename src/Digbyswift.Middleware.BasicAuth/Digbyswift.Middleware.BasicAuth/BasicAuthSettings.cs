using Digbyswift.Core.Constants;
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

        private IEnumerable<string>? _excludedPaths;
        private IEnumerable<string>? _validExcludedPaths;
        public IEnumerable<string>? ExcludedPaths
        {
            get => _validExcludedPaths ??= _excludedPaths?.Where(x => x.StartsWith(CharConstants.ForwardSlash)) ?? Enumerable.Empty<string>();
            set => _excludedPaths = value;
        }

        public IEnumerable<string>? WhitelistedIPs { get; set; }
        public IEnumerable<string>? WhitelistedReferrers { get; set; }
    }
}
