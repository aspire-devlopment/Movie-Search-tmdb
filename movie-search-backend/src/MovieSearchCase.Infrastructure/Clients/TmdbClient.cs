using System.Net.Http.Json;
using MovieSearchCase.Domain.Entities;
using MovieSearchCase.Domain.Exceptions;
using System.Net;
using System.Text.Json;
using MovieSearchCase.Domain.Interfaces.Clients;

namespace MovieSearchCase.Infrastructure.Clients;

public class TmdbClient : ITmdbClient
{
    private const string TrendingMoviesPath = "trending/movie/week";
    private const string MovieSearchPath = "search/movie";
    private const string TvSearchPath = "search/tv";

    private readonly HttpClient _httpClient;

    public TmdbClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<Movie>> GetTrendingMoviesAsync(CancellationToken cancellationToken)
    {
        TmdbPagedResponse<TmdbMovie>? response;

        try
        {
            response = await _httpClient.GetFromJsonAsync<TmdbPagedResponse<TmdbMovie>>(
                TrendingMoviesPath,
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new MovieException(
                MovieException.ExceptionTitle,
                ErrorType.UpstreamServiceUnavailable,
                "TMDB did not return trending movies.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw UpstreamFailure("TMDB trending movies request timed out.");
        }
        catch (JsonException)
        {
            throw UpstreamFailure("TMDB returned an invalid trending movies response.");
        }

        if (response is null)
        {
            throw new MovieException(
                MovieException.ExceptionTitle,
                ErrorType.UpstreamServiceUnavailable,
                "TMDB returned an empty trending movies response.");
        }

        return response.Results.Select(TmdbMovieMapper.ToDomainModel).ToList();
    }

    // This keeps TMDB's response metadata and naming inside Infrastructure.
    public async Task<PagedResult<Movie>> SearchMoviesAsync(
      string query,
      int page,
      CancellationToken cancellationToken)
    {
        TmdbPagedResponse<TmdbMovie>? response;

        try
        {
            response = await _httpClient.GetFromJsonAsync<TmdbPagedResponse<TmdbMovie>>(
                $"{MovieSearchPath}?query={Uri.EscapeDataString(query)}&page={page}",
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new MovieException(
                MovieException.ExceptionTitle,
                ErrorType.UpstreamServiceUnavailable,
                "TMDB did not return movie search results.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new MovieException(
                MovieException.ExceptionTitle,
                ErrorType.UpstreamServiceUnavailable,
                "TMDB movie search request timed out.");
        }
        catch (JsonException)
        {
            throw UpstreamFailure("TMDB returned an invalid movie search response.");
        }

        if (response is null)
        {
            throw new MovieException(
                MovieException.ExceptionTitle,
                ErrorType.UpstreamServiceUnavailable,
                "TMDB returned an empty movie search response.");
        }

        return new PagedResult<Movie>
        {
            Page = response.Page,
            TotalPages = response.TotalPages,
            TotalResults = response.TotalResults,
            Results = response.Results
                .Select(TmdbMovieMapper.ToDomainModel)
                .ToList()
        };
    }

    public async Task<Movie?> GetMovieAsync(int id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<TmdbMovie>(
                $"movie/{id}", cancellationToken);
            return response is null
                ? throw UpstreamFailure("TMDB returned an empty movie details response.")
                : response.ToDomainModel();
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (HttpRequestException)
        {
            throw UpstreamFailure("TMDB did not return movie details.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw UpstreamFailure("TMDB movie details request timed out.");
        }
        catch (JsonException)
        {
            throw UpstreamFailure("TMDB returned invalid movie details.");
        }
    }

    public async Task<IReadOnlyList<MovieVideo>> GetMovieVideosAsync(
        int id,
        CancellationToken cancellationToken)
    {
        TmdbVideoResponse? response;
        try
        {
            response = await _httpClient.GetFromJsonAsync<TmdbVideoResponse>(
                $"movie/{id}/videos", cancellationToken);
        }
        catch (HttpRequestException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            throw new MovieException(
                MovieException.ExceptionTitle,
                ErrorType.EntityNotFound,
                $"No movie with id '{id}' was found.");
        }
        catch (HttpRequestException)
        {
            throw UpstreamFailure("TMDB did not return movie videos.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw UpstreamFailure("TMDB movie videos request timed out.");
        }
        catch (JsonException)
        {
            throw UpstreamFailure("TMDB returned an invalid movie videos response.");
        }

        if (response is null)
        {
            throw UpstreamFailure("TMDB returned an empty movie videos response.");
        }

        return response.Results.Select(TmdbMovieMapper.ToDomainModel).ToArray();
    }

    public async Task<PagedResult<TvSeries>> SearchTvSeriesAsync(
        string query,
        int page,
        CancellationToken cancellationToken)
    {
        TmdbPagedResponse<TmdbTvSeries>? response;
        try
        {
            response = await _httpClient.GetFromJsonAsync<TmdbPagedResponse<TmdbTvSeries>>(
                $"{TvSearchPath}?query={Uri.EscapeDataString(query)}&page={page}", cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw UpstreamFailure("TMDB did not return TV search results.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw UpstreamFailure("TMDB TV search request timed out.");
        }
        catch (JsonException)
        {
            throw UpstreamFailure("TMDB returned an invalid TV search response.");
        }

        if (response is null)
        {
            throw UpstreamFailure("TMDB returned an empty TV search response.");
        }

        return new PagedResult<TvSeries>
        {
            Page = response.Page,
            TotalPages = response.TotalPages,
            TotalResults = response.TotalResults,
            Results = response.Results.Select(TmdbMovieMapper.ToDomainModel).ToArray(),
        };
    }

    private static MovieException UpstreamFailure(string detail) => new(
        MovieException.ExceptionTitle,
        ErrorType.UpstreamServiceUnavailable,
        detail);



}
