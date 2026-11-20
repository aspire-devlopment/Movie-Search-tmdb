using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace MovieSearchCase.WebApi.Application.Cors;

public static class CorsExtensions
{
    private const string PolicyName = "Default";

    public static IServiceCollection AddCorsDefaultPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var corsOptions = configuration.GetSection("CorsPolicy").Get<CorsOptions>() ?? new CorsOptions();

        if (corsOptions.AllowedOrigins.Any(origin =>
                origin == "*" ||
                !Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
                uri.UserInfo.Length > 0 || uri.AbsolutePath != "/" || uri.Query.Length > 0 || uri.Fragment.Length > 0))
        {
            throw new InvalidOperationException(
                "CorsPolicy:AllowedOrigins must contain exact http(s) origins; wildcard and path entries are not allowed.");
        }

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                policy.WithOrigins(corsOptions.AllowedOrigins)
                    .WithMethods("GET", "OPTIONS")
                    .WithHeaders(HeaderNames.Accept, HeaderNames.ContentType)
                    .WithExposedHeaders(HeaderNames.RetryAfter);
            });
        });

        return services;
    }

    public static IApplicationBuilder UseCorsDefaultPolicy(this IApplicationBuilder app) => app.UseCors(PolicyName);
}
