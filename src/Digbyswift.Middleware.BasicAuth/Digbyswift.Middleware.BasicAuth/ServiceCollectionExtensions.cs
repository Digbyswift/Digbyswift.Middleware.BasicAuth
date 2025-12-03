using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Digbyswift.Middleware.BasicAuth;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBasicAuthentication(this IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        var config = serviceProvider.GetService<IConfiguration>();

        var basicAuthConfig = config?.GetSection(BasicAuthSettings.SectionName);
        var basicAuthSettings = basicAuthConfig?.Get<BasicAuthSettings>();
        if (basicAuthSettings?.IsEnabled ?? false)
        {
            ArgumentNullException.ThrowIfNull(basicAuthSettings.Username);
            ArgumentNullException.ThrowIfNull(basicAuthSettings.Password);

            services.Configure<BasicAuthSettings>(basicAuthConfig!);
            services.Configure<AuthenticationOptions>(options =>
            {
                options.AddScheme<CookieAuthenticationHandler>(BasicAuthSettings.AuthenticationSchemeName, null);
            });
            services.AddScoped<BasicAuthenticationMiddleware>();
        }

        return services;
    }
}
