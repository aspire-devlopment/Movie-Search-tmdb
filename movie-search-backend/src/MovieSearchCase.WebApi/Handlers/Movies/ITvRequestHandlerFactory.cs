using MovieSearchCase.Shared;

namespace MovieSearchCase.WebApi.Handlers.Movies;

public interface ITvRequestHandlerFactory
{
    IRequestHandlerAsync Search(string query, int page);
}
