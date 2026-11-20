namespace MovieSearchCase.WebApi.Mappers;

internal static class MovieMapper
{
    public static Models.Movies.Movie ToApiModel(this Domain.Entities.Movie movie) => new()
    {
        Id = movie.Id,
        Title = movie.Title,
        Overview = movie.Overview,
        PosterPath = movie.PosterPath,
        BackdropPath = movie.BackdropPath,
        VoteAverage = movie.VoteAverage,
        ReleaseDate = movie.ReleaseDate,
    };

    // The detail response has its own DTO so list and search responses retain the
    // exact Movie contract while details can include their three additional fields.
    public static Models.Movies.MovieDetails ToDetailsApiModel(this Domain.Entities.Movie movie) => new()
    {
        Id = movie.Id,
        Title = movie.Title,
        Overview = movie.Overview,
        PosterPath = movie.PosterPath,
        BackdropPath = movie.BackdropPath,
        VoteAverage = movie.VoteAverage,
        ReleaseDate = movie.ReleaseDate,
        Runtime = movie.Runtime,
        Tagline = movie.Tagline,
        Genres = movie.Genres,
    };
}
