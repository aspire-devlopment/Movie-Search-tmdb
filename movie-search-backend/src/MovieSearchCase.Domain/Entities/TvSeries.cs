namespace MovieSearchCase.Domain.Entities;

/// <summary>Domain representation of a TMDB television series search result.</summary>
public class TvSeries
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public string? Overview { get; init; }

    public string? PosterPath { get; init; }

    public string? BackdropPath { get; init; }

    public double VoteAverage { get; init; }

    public DateOnly? FirstAirDate { get; init; }
}
