using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieSearchCase.Domain.Entities;
using MovieSearchCase.Domain.Interfaces.Services;
using MovieSearchCase.WebApi.Handlers.Movies;
using Xunit;

namespace MovieSearchCase.WebApi.Tests.Handlers;

public class SearchMoviesHandlerTests
{
    [Fact]
    public async Task HandleShouldReturnPagingEnvelopeAndMappedMovies()
    {
        var paged = new PagedResult<Movie>
        {
            Page = 2,
            TotalPages = 3,
            TotalResults = 41,
            Results = [new Movie { Id = 7, Title = "Example", VoteAverage = 8.0 }],
        };
        var service = new Mock<IMovieService>();
        service.Setup(s => s.SearchAsync("Example", 2, It.IsAny<CancellationToken>())).ReturnsAsync(paged);
        var handler = new SearchMoviesHandler(service.Object, "Example", 2);

        var response = await handler.HandleAsync(new DefaultHttpContext().Request);

        var value = response.Should().BeOfType<OkObjectResult>().Subject.Value!;
        value.GetType().GetProperty("Page")!.GetValue(value).Should().Be(2);
        value.GetType().GetProperty("TotalPages")!.GetValue(value).Should().Be(3);
        value.GetType().GetProperty("TotalResults")!.GetValue(value).Should().Be(41);
        value.GetType().GetProperty("Results")!.GetValue(value)
            .Should().BeAssignableTo<IEnumerable<MovieSearchCase.WebApi.Models.Movies.Movie>>()
            .Which.Should().ContainSingle().Which.Id.Should().Be(7);
    }

    [Fact]
    public async Task HandleShouldPreserveEmptySearchResult()
    {
        var service = new Mock<IMovieService>();
        service.Setup(s => s.SearchAsync("no match", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Movie>() { Page = 1 });
        var handler = new SearchMoviesHandler(service.Object, "no match", 1);

        var response = await handler.HandleAsync(new DefaultHttpContext().Request);

        var value = response.Should().BeOfType<OkObjectResult>().Subject.Value!;
        value.GetType().GetProperty("TotalPages")!.GetValue(value).Should().Be(0);
        value.GetType().GetProperty("Results")!.GetValue(value)
            .Should().BeAssignableTo<IEnumerable<MovieSearchCase.WebApi.Models.Movies.Movie>>()
            .Which.Should().BeEmpty();
    }
}
