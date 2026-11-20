namespace MovieSearchCase.Domain.Entities;

/// <summary>Video metadata returned by the movie videos endpoint.</summary>
public class MovieVideo
{
    public required string Key { get; init; }

    public required string Name { get; init; }

    public required string Site { get; init; }

    public required string Type { get; init; }
}
