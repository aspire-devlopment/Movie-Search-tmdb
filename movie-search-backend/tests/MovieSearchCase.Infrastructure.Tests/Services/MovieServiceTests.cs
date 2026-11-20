using FluentAssertions;
using Moq;
using MovieSearchCase.Domain.Entities;
using MovieSearchCase.Domain.Interfaces.Clients;
using MovieSearchCase.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace MovieSearchCase.Infrastructure.Tests.Services;

public class MovieServiceTests
{
    [Fact]
    public async Task GetTrendingAsyncShouldReturnMoviesFromTmdbClient()
    {
        var expectedMovies = new List<Movie>
        {
            new()
            {
                Id = 42,
                Title = "The Answer",
                VoteAverage = 9.2,
            },
        };

        var tmdbClientMock = new Mock<ITmdbClient>();
        tmdbClientMock
            .Setup(client => client.GetTrendingMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMovies);

        var movieService = new MovieService(tmdbClientMock.Object, new MemoryCache(new MemoryCacheOptions()));

        var result = await movieService.GetTrendingAsync(CancellationToken.None);

        result.Should().BeEquivalentTo(expectedMovies);
    }

    [Fact]
    public async Task SearchAsyncShouldDelegateAndCacheSuccessfulPages()
    {
        var expected = new PagedResult<Movie>
        {
            Page = 2,
            TotalPages = 3,
            TotalResults = 41,
            Results = [new Movie { Id = 42, Title = "The Answer" }],
        };
        var client = new Mock<ITmdbClient>();
        client.Setup(c => c.SearchMoviesAsync("The Answer", 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new MovieService(client.Object, cache);

        var first = await service.SearchAsync("The Answer", 2, CancellationToken.None);
        var second = await service.SearchAsync("the answer", 2, CancellationToken.None);

        first.Should().BeSameAs(expected);
        second.Should().BeSameAs(expected);
        client.Verify(c => c.SearchMoviesAsync("The Answer", 2, It.IsAny<CancellationToken>()), Times.Once);
    }
}
