using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using MovieSearchCase.Shared;
using MovieSearchCase.WebApi.Controllers.V1;
using MovieSearchCase.WebApi.Application;
using MovieSearchCase.WebApi.Handlers.Movies;
using Xunit;

namespace MovieSearchCase.WebApi.Tests.Controllers;

public class MoviesControllerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchShouldRejectMissingOrWhitespaceQuery(string? query)
    {
        var factory = new Mock<IMoviesRequestHandlerFactory>();
        var controller = CreateController(factory.Object);

        var result = await controller.Search(query, null);

        var problem = result.Should().BeOfType<ObjectResult>().Subject;
        problem.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        problem.ContentTypes.Should().Contain("application/problem+json");
        problem.Value.Should().BeOfType<ProblemDetails>().Which.Detail.Should().Contain("query");
        factory.Verify(f => f.SearchMovies(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    public async Task SearchShouldRejectPageOutsideContractRange(int page)
    {
        var controller = CreateController(Mock.Of<IMoviesRequestHandlerFactory>());

        var result = await controller.Search("valid", page);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task SearchShouldRejectQueryLongerThanConfiguredMaximum()
    {
        var controller = CreateController(Mock.Of<IMoviesRequestHandlerFactory>());

        var result = await controller.Search(new string('x', ApiDefaults.MaximumSearchQueryLength + 1), 1);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task SearchShouldDefaultPageAndTrimQuery()
    {
        var handler = new Mock<IRequestHandlerAsync>();
        handler.Setup(h => h.HandleAsync(It.IsAny<HttpRequest>())).ReturnsAsync(new OkResult());
        var factory = new Mock<IMoviesRequestHandlerFactory>();
        factory.Setup(f => f.SearchMovies("valid", 1)).Returns(handler.Object);
        var controller = CreateController(factory.Object);

        var result = await controller.Search("  valid  ", null);

        result.Should().BeOfType<OkResult>();
        factory.Verify(f => f.SearchMovies("valid", 1), Times.Once);
    }

    private static MoviesController CreateController(IMoviesRequestHandlerFactory factory) => new(factory)
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
    };
}
