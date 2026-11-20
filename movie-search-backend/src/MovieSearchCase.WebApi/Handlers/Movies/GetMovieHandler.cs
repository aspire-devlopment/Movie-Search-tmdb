using Microsoft.AspNetCore.Mvc;
using MovieSearchCase.Domain.Interfaces.Services;
using MovieSearchCase.Shared;
using MovieSearchCase.WebApi.Mappers;

namespace MovieSearchCase.WebApi.Handlers.Movies;

public class GetMovieHandler : IRequestHandlerAsync
{
    private readonly IMovieService _movieService;
    private readonly int _id;

    public GetMovieHandler(IMovieService movieService, int id)
    {
        _movieService = movieService;
        _id = id;
    }

    public async Task<IActionResult> HandleAsync(HttpRequest request)
    {
        var movie = await _movieService.GetMovieAsync(_id, request.HttpContext.RequestAborted);
        return movie is null
            ? new ObjectResult(new ProblemDetails
            {
                Type = "about:blank",
                Title = "Movie not found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"No movie with id '{_id}' was found.",
            })
            {
                StatusCode = StatusCodes.Status404NotFound,
                ContentTypes = { "application/problem+json" },
            }
            : new OkObjectResult(movie.ToDetailsApiModel());
    }
}
