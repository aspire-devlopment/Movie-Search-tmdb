namespace MovieSearchCase.Domain.Entities;

public class Movie
{
    public required int Id { get; init; }

    public required string Title { get; init; }

    public string? Overview { get; init; }

    public string? PosterPath { get; init; }

    public string? BackdropPath { get; init; }

    public double VoteAverage { get; init; }

    public DateOnly? ReleaseDate { get; init; }

    // Detail fields are null on list/search results and populated by the detail lookup.
    public int? Runtime { get; init; }

    public string? Tagline { get; init; }

    public IReadOnlyList<string> Genres { get; init; } = [];
}
