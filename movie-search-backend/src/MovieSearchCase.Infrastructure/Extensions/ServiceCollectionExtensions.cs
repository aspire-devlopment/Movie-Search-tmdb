using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MovieSearchCase.Domain.Interfaces.Clients;
using MovieSearchCase.Domain.Interfaces.Services;
using MovieSearchCase.Infrastructure.Clients;
using MovieSearchCase.Infrastructure.Services;
using MovieSearchCase.Shared.Extensions;

namespace MovieSearchCase.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        // Search pages are stable enough for a short in-memory cache and avoid
        // repeating identical TMDB calls while users paginate or revisit a query.
        services.AddMemoryCache(options => options.SizeLimit = SearchCacheDefaults.MaximumEntries);
        services.AddScoped<IMovieService, MovieService>();

        return services;
    }

    public static IServiceCollection AddClients(this IServiceCollection services, IConfiguration configuration)
    {
        var tmdbOptions = configuration.GetSection(nameof(TmdbOptions)).Get<TmdbOptions>().ValidateOptions();
        if (string.IsNullOrWhiteSpace(tmdbOptions.AccessToken))
        {
            throw new System.ComponentModel.DataAnnotations.ValidationException(
                "TmdbOptions:AccessToken must contain a TMDB API read access token.");
        }

        if (!Uri.TryCreate(tmdbOptions.BaseUrl, UriKind.Absolute, out var tmdbBaseUri) ||
            tmdbBaseUri.Scheme != Uri.UriSchemeHttps ||
            !tmdbBaseUri.Host.Equals(TmdbOptions.TrustedHost, StringComparison.OrdinalIgnoreCase) ||
            tmdbBaseUri.UserInfo.Length > 0 ||
            tmdbBaseUri.Query.Length > 0 ||
            tmdbBaseUri.Fragment.Length > 0)
        {
            throw new System.ComponentModel.DataAnnotations.ValidationException(
                $"TmdbOptions:BaseUrl must be an HTTPS URL on {TmdbOptions.TrustedHost}.");
        }

        services.AddHttpClient<ITmdbClient, TmdbClient>(client =>
        {
            client.BaseAddress = tmdbBaseUri;
            client.Timeout = TimeSpan.FromSeconds(TmdbOptions.RequestTimeoutSeconds);
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tmdbOptions.AccessToken);
        });

        return services;
    }
}
