namespace MovieSearchCase.WebApi.Models.Movies;

/// <summary>Additive TV search result shape; existing movie endpoints keep their contract.</summary>
public record TvSeries
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public string? Overview { get; init; }

    public string? PosterPath { get; init; }

    public string? BackdropPath { get; init; }

    public double VoteAverage { get; init; }

    public DateOnly? FirstAirDate { get; init; }
}
