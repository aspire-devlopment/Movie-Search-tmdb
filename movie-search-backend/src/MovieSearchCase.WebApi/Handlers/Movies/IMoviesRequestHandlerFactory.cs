using MovieSearchCase.Shared;

namespace MovieSearchCase.WebApi.Handlers.Movies;

public interface IMoviesRequestHandlerFactory
{
    IRequestHandlerAsync GetTrendingMovies();

    // Keep request-specific values in the handler so the controller stays thin.
    IRequestHandlerAsync SearchMovies(string query, int page);

    IRequestHandlerAsync GetMovie(int id);

    IRequestHandlerAsync GetMovieVideos(int id);
}
