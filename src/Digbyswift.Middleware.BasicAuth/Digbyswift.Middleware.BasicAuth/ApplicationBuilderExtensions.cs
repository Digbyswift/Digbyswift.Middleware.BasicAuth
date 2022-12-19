using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Digbyswift.Middleware.BasicAuth
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseBasicHttpAuthentication(this IApplicationBuilder builder)
        {
            return builder.ApplicationServices.GetService<IOptions<BasicAuthSettings>>()?.Value.IsEnabled ?? false
                ? builder.UseMiddleware<BasicAuthenticationMiddleware>()
                : builder;
        }
    }
}
