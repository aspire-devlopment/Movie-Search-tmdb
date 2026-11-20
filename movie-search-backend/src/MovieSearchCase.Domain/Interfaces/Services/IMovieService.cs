using MovieSearchCase.Domain.Entities;

namespace MovieSearchCase.Domain.Interfaces.Services;

/// <summary>
/// TMDB-backed movie lookups used by the API.
/// </summary>
public interface IMovieService
{
    Task<IReadOnlyList<Movie>> GetTrendingAsync(CancellationToken cancellationToken);

    // Search results carry paging metadata so the API can return the frontend contract.
    Task<PagedResult<Movie>> SearchAsync(
    string query,
    int page,
    CancellationToken cancellationToken);

    Task<Movie?> GetMovieAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<MovieVideo>> GetMovieVideosAsync(int id, CancellationToken cancellationToken);

    Task<PagedResult<TvSeries>> SearchTvSeriesAsync(string query, int page, CancellationToken cancellationToken);

}
