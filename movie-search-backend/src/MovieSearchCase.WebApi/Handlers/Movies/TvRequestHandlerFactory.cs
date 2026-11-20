using MovieSearchCase.Domain.Interfaces.Services;
using MovieSearchCase.Shared;

namespace MovieSearchCase.WebApi.Handlers.Movies;

public class TvRequestHandlerFactory : ITvRequestHandlerFactory
{
    private readonly IMovieService _movieService;

    public TvRequestHandlerFactory(IMovieService movieService)
    {
        _movieService = movieService;
    }

    public IRequestHandlerAsync Search(string query, int page) =>
        new SearchTvSeriesHandler(_movieService, query, page);
}
