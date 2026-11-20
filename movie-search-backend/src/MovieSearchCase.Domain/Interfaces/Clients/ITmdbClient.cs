using MovieSearchCase.Domain.Entities;

namespace MovieSearchCase.Domain.Interfaces.Clients;

/// <summary>
/// Talks to The Movie Database (TMDB) REST API. See https://developer.themoviedb.org/reference.
/// </summary>
public interface ITmdbClient
{
    /// <summary>
    /// Returns this week's trending movies (TMDB's /trending/movie/week endpoint).
    /// </summary>
    Task<IReadOnlyList<Movie>> GetTrendingMoviesAsync(CancellationToken cancellationToken);

    // TMDB search returns its page totals alongside the movies.
    Task<PagedResult<Movie>> SearchMoviesAsync(string query, int page, CancellationToken cancellationToken);

    Task<Movie?> GetMovieAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<MovieVideo>> GetMovieVideosAsync(int id, CancellationToken cancellationToken);

    Task<PagedResult<TvSeries>> SearchTvSeriesAsync(string query, int page, CancellationToken cancellationToken);
}
