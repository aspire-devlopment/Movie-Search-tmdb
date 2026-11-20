
using Microsoft.AspNetCore.Mvc;
using MovieSearchCase.Domain.Interfaces.Services;
using MovieSearchCase.Shared;
using MovieSearchCase.WebApi.Mappers;

namespace MovieSearchCase.WebApi.Handlers.Movies;

public class SearchMoviesHandler : IRequestHandlerAsync
{
    private readonly IMovieService _movieService;
    private readonly string _query;
    private readonly int _page;

    public SearchMoviesHandler(
        IMovieService movieService,
        string query,
        int page)
    {
        _movieService = movieService;
        _query = query;
        _page = page;
    }

    public async Task<IActionResult> HandleAsync(HttpRequest request)
    {
        var result = await _movieService.SearchAsync(
            _query,
            _page,
            request.HttpContext.RequestAborted);

        return new OkObjectResult(new
        {
            Page = result.Page,
            TotalPages = result.TotalPages,
            TotalResults = result.TotalResults,
            Results = result.Results.Select(movie => movie.ToApiModel())
        });
    }
}
