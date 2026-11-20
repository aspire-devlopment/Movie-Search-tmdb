using MovieSearchCase.Domain.Interfaces.Services;
using MovieSearchCase.Shared;

namespace MovieSearchCase.WebApi.Handlers.Movies;

public class MoviesRequestHandlerFactory : IMoviesRequestHandlerFactory
{
    private readonly IMovieService _movieService;

    public MoviesRequestHandlerFactory(IMovieService movieService)
    {
        _movieService = movieService;
    }

    public IRequestHandlerAsync GetTrendingMovies() => new GetTrendingMoviesHandler(_movieService);

    // The factory is the existing composition point for movie request handlers.
    public IRequestHandlerAsync SearchMovies(string query, int page) =>
        new SearchMoviesHandler(_movieService, query, page);

    public IRequestHandlerAsync GetMovie(int id) => new GetMovieHandler(_movieService, id);

    public IRequestHandlerAsync GetMovieVideos(int id) => new GetMovieVideosHandler(_movieService, id);
}
