using System.Net;
using FluentAssertions;
using MovieSearchCase.Domain.Exceptions;
using MovieSearchCase.Infrastructure.Clients;
using Xunit;

namespace MovieSearchCase.Infrastructure.Tests.Clients;

public class TmdbClientTests
{
    [Fact]
    public async Task SearchMoviesAsyncShouldEncodeQueryAndMapPagingAndMovies()
    {
        Uri? requestedUri = null;
        var client = CreateClient(request =>
        {
            requestedUri = request.RequestUri;
            return Json("""
                {"page":2,"total_pages":4,"total_results":65,"results":[{"id":7,"title":"Star Wars","overview":null,"poster_path":"/p.jpg","backdrop_path":null,"vote_average":8.1,"release_date":"1977-05-25"}]}
                """);
        });

        var result = await client.SearchMoviesAsync("Star Wars", 2, CancellationToken.None);

        requestedUri!.Query.Should().Contain("query=Star%20Wars");
        requestedUri.Query.Should().Contain("page=2");
        result.Page.Should().Be(2);
        result.TotalPages.Should().Be(4);
        result.TotalResults.Should().Be(65);
        result.Results.Should().ContainSingle().Which.ReleaseDate.Should().Be(new DateOnly(1977, 5, 25));
    }

    [Fact]
    public async Task GetMovieAsyncShouldReturnNullForTmdbNotFound()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await client.GetMovieAsync(404, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMovieAsyncShouldMapDetailFields()
    {
        var client = CreateClient(_ => Json("""
            {"id":9,"title":"Example","overview":"Plot","poster_path":null,"backdrop_path":null,"vote_average":7.5,"release_date":"2020-01-02","runtime":101,"tagline":"A line","genres":[{"name":"Drama"}]}
            """));

        var result = await client.GetMovieAsync(9, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Runtime.Should().Be(101);
        result.Tagline.Should().Be("A line");
        result.Genres.Should().Equal("Drama");
    }

    [Fact]
    public async Task GetMovieVideosAsyncShouldMapEmptyAndPopulatedResults()
    {
        var client = CreateClient(_ => Json("""
            {"id":9,"results":[{"key":"abc123","name":"Trailer","site":"YouTube","type":"Trailer"}]}
            """));

        var result = await client.GetMovieVideosAsync(9, CancellationToken.None);

        result.Should().ContainSingle().Which.Key.Should().Be("abc123");
    }

    [Fact]
    public async Task GetMovieVideosAsyncShouldReturnEmptyArrayWhenTmdbHasNoVideos()
    {
        var client = CreateClient(_ => Json("""{"id":9,"results":[]}"""));

        var result = await client.GetMovieVideosAsync(9, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchMoviesAsyncShouldTranslateUpstreamFailureTo503()
    {
        var client = CreateClient(_ => throw new HttpRequestException("offline"));

        var act = () => client.SearchMoviesAsync("test", 1, CancellationToken.None);

        await act.Should().ThrowAsync<MovieException>()
            .Where(exception => exception.ErrorCode == ErrorType.UpstreamServiceUnavailable);
    }

    [Fact]
    public async Task SearchTvSeriesAsyncShouldMapTmdbSeriesAndPaging()
    {
        var client = CreateClient(_ => Json("""
            {"page":1,"total_pages":2,"total_results":21,"results":[{"id":12,"name":"Example Series","overview":"Plot","poster_path":"/p.jpg","backdrop_path":null,"vote_average":8.2,"first_air_date":"2021-02-03"}]}
            """));

        var result = await client.SearchTvSeriesAsync("Example", 1, CancellationToken.None);

        result.TotalResults.Should().Be(21);
        result.Results.Should().ContainSingle().Which.Name.Should().Be("Example Series");
        result.Results.Single().FirstAirDate.Should().Be(new DateOnly(2021, 2, 3));
    }

    private static TmdbClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        var handler = new DelegateHandler(responseFactory);
        return new TmdbClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.themoviedb.org/3/"),
        });
    }

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
    };

    private sealed class DelegateHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(responseFactory(request));
    }
}
