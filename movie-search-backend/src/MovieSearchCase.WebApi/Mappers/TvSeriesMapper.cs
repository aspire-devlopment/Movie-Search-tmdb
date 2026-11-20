namespace MovieSearchCase.WebApi.Mappers;

internal static class TvSeriesMapper
{
    public static Models.Movies.TvSeries ToApiModel(this Domain.Entities.TvSeries series) => new()
    {
        Id = series.Id,
        Name = series.Name,
        Overview = series.Overview,
        PosterPath = series.PosterPath,
        BackdropPath = series.BackdropPath,
        VoteAverage = series.VoteAverage,
        FirstAirDate = series.FirstAirDate,
    };
}
