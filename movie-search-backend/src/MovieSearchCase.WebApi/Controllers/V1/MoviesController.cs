using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using MovieSearchCase.WebApi.Application.Versioning;
using MovieSearchCase.WebApi.Application;
using MovieSearchCase.WebApi.Handlers.Movies;
using Swashbuckle.AspNetCore.Annotations;

namespace MovieSearchCase.WebApi.Controllers.V1;

[ApiVersion(ApiDefaults.Version)]
[VersionedRoute]
public class MoviesController : ApiControllerBase
{
    private readonly IMoviesRequestHandlerFactory _requestHandlerFactory;

    public MoviesController(IMoviesRequestHandlerFactory requestHandlerFactory)
    {
        _requestHandlerFactory = requestHandlerFactory;
    }

    /// <summary>
    /// Returns this week's TMDB-backed trending movies.
    /// </summary>
    [HttpGet("trending")]
    [SwaggerOperation(Summary = "Get trending movies", OperationId = "GetTrendingMovies")]
    [SwaggerResponse(StatusCodes.Status200OK, "This week's trending movies", typeof(IEnumerable<Models.Movies.Movie>))]
    public async Task<IActionResult> GetTrending() =>
        await _requestHandlerFactory.GetTrendingMovies().HandleAsync(Request);

    /// <summary>Searches TMDB movies and returns the requested result page.</summary>
    [HttpGet("search")]
    [SwaggerOperation(Summary = "Search movies", OperationId = "SearchMovies")]
    [SwaggerResponse(StatusCodes.Status200OK, "Paged movie search results")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid query or page")]
    public async Task<IActionResult> Search(
        [FromQuery] string? query,
        [FromQuery] int? page)
    {
        // Validate explicitly so blank strings and nullable query parameters get the
        // same problem response, instead of relying on subtle model-binding defaults.
        if (string.IsNullOrWhiteSpace(query))
        {
            return ProblemResponse(StatusCodes.Status400BadRequest, "Invalid request",
                "The 'query' parameter is required and must not contain only spaces.");
        }

        query = query.Trim();
        if (query.Length > ApiDefaults.MaximumSearchQueryLength || query.Any(char.IsControl))
        {
            return ProblemResponse(StatusCodes.Status400BadRequest, "Invalid request",
                $"The 'query' parameter must be at most {ApiDefaults.MaximumSearchQueryLength} characters and contain no control characters.");
        }

        var requestedPage = page ?? ApiDefaults.DefaultPage;
        if (requestedPage < ApiDefaults.DefaultPage || requestedPage > ApiDefaults.MaximumPage)
        {
            return ProblemResponse(StatusCodes.Status400BadRequest, "Invalid request",
                $"The 'page' parameter must be a whole number from {ApiDefaults.DefaultPage} to {ApiDefaults.MaximumPage}.");
        }

        return await _requestHandlerFactory.SearchMovies(query, requestedPage)
            .HandleAsync(Request);
    }

    /// <summary>Returns the details for one movie.</summary>
    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Get movie details", OperationId = "GetMovie")]
    [SwaggerResponse(StatusCodes.Status200OK, "Movie details", typeof(Models.Movies.MovieDetails))]
    public async Task<IActionResult> GetMovie(int id)
    {
        if (id <= 0)
        {
            return ProblemResponse(StatusCodes.Status400BadRequest, "Invalid request",
                "The movie id must be positive.");
        }

        return await _requestHandlerFactory.GetMovie(id).HandleAsync(Request);
    }

    /// <summary>Returns videos associated with a movie.</summary>
    [HttpGet("{id:int}/videos")]
    [SwaggerOperation(Summary = "Get movie videos", OperationId = "GetMovieVideos")]
    public async Task<IActionResult> GetMovieVideos(int id)
    {
        if (id <= 0)
        {
            return ProblemResponse(StatusCodes.Status400BadRequest, "Invalid request",
                "The movie id must be positive.");
        }

        return await _requestHandlerFactory.GetMovieVideos(id).HandleAsync(Request);
    }
}
