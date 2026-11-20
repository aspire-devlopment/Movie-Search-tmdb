namespace MovieSearchCase.WebApi.Models.Movies;

/// <summary>The detail endpoint extends the shared movie shape with TMDB detail fields.</summary>
public record MovieDetails : Movie
{
    public int? Runtime { get; init; }

    public string? Tagline { get; init; }

    public IReadOnlyList<string> Genres { get; init; } = [];
}
