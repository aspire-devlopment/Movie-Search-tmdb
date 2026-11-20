using MovieSearchCase.Domain.Entities;
using MovieSearchCase.Domain.Interfaces.Clients;
using MovieSearchCase.Domain.Interfaces.Services;
using MovieSearchCase.Infrastructure.Clients;
using Microsoft.Extensions.Caching.Memory;

namespace MovieSearchCase.Infrastructure.Services;

public class MovieService : IMovieService
{
    private readonly ITmdbClient _tmdbClient;
    private readonly IMemoryCache _cache;

    public MovieService(ITmdbClient tmdbClient, IMemoryCache cache)
    {
        _tmdbClient = tmdbClient;
        _cache = cache;
    }

    public Task<IReadOnlyList<Movie>> GetTrendingAsync(CancellationToken cancellationToken) =>
        _tmdbClient.GetTrendingMoviesAsync(cancellationToken);


    // Keep this layer as a thin domain-facing delegation to the TMDB client.
    public Task<PagedResult<Movie>> SearchAsync(
    string query,
    int page,
    CancellationToken cancellationToken)
    {
        var cacheKey = $"movie-search:{query.Trim().ToUpperInvariant()}:{page}";
        if (_cache.TryGetValue<PagedResult<Movie>>(cacheKey, out var cached) && cached is not null)
        {
            return Task.FromResult(cached);
        }

        return SearchAndCacheAsync(cacheKey, query, page, cancellationToken);
    }

    private async Task<PagedResult<Movie>> SearchAndCacheAsync(
        string cacheKey,
        string query,
        int page,
        CancellationToken cancellationToken)
    {
        var result = await _tmdbClient.SearchMoviesAsync(query, page, cancellationToken);
        // Cache only successful responses, and keep the lifetime short to limit stale data.
        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(SearchCacheDefaults.EntryLifetimeMinutes))
            .SetSize(SearchCacheDefaults.EntrySize));
        return result;
    }

    public Task<Movie?> GetMovieAsync(int id, CancellationToken cancellationToken) =>
        _tmdbClient.GetMovieAsync(id, cancellationToken);

    public Task<IReadOnlyList<MovieVideo>> GetMovieVideosAsync(int id, CancellationToken cancellationToken) =>
        _tmdbClient.GetMovieVideosAsync(id, cancellationToken);

    public Task<PagedResult<TvSeries>> SearchTvSeriesAsync(
        string query,
        int page,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"tv-search:{query.Trim().ToUpperInvariant()}:{page}";
        if (_cache.TryGetValue<PagedResult<TvSeries>>(cacheKey, out var cached) && cached is not null)
        {
            return Task.FromResult(cached);
        }

        return SearchTvAndCacheAsync(cacheKey, query, page, cancellationToken);
    }

    private async Task<PagedResult<TvSeries>> SearchTvAndCacheAsync(
        string cacheKey,
        string query,
        int page,
        CancellationToken cancellationToken)
    {
        var result = await _tmdbClient.SearchTvSeriesAsync(query, page, cancellationToken);
        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(SearchCacheDefaults.EntryLifetimeMinutes))
            .SetSize(SearchCacheDefaults.EntrySize));
        return result;
    }



}
