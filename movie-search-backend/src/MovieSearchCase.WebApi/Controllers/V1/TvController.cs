using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using MovieSearchCase.WebApi.Handlers.Movies;
using MovieSearchCase.WebApi.Controllers;
using MovieSearchCase.WebApi.Application;
using Swashbuckle.AspNetCore.Annotations;

namespace MovieSearchCase.WebApi.Controllers.V1;

[ApiVersion(ApiDefaults.Version)]
[Route(ApiDefaults.TvRoute)]
[ApiController]
public class TvController : ApiControllerBase
{
    private readonly ITvRequestHandlerFactory _handlerFactory;

    public TvController(ITvRequestHandlerFactory handlerFactory)
    {
        _handlerFactory = handlerFactory;
    }

    /// <summary>Searches TV series. This additive endpoint uses movie-search paging rules.</summary>
    [HttpGet("search")]
    [SwaggerOperation(Summary = "Search TV series", OperationId = "SearchTvSeries")]
    [SwaggerResponse(StatusCodes.Status200OK, "Paged TV series results")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid query or page")]
    public async Task<IActionResult> Search([FromQuery] string? query, [FromQuery] int? page)
    {
        // Keep the stretch endpoint's validation aligned with movie search.
        if (string.IsNullOrWhiteSpace(query))
        {
            return ProblemResponse(StatusCodes.Status400BadRequest, "Invalid request",
                "The 'query' parameter is required and must not contain only spaces.");
        }

        query = query.Trim();
        if (query.Length > ApiDefaults.MaximumSearchQueryLength || query.Any(char.IsControl))
        {
            return ProblemResponse(StatusCodes.Status400BadRequest, "Invalid request",
                $"The 'query' parameter must be at most {ApiDefaults.MaximumSearchQueryLength} characters and contain no control characters.");
        }

        var requestedPage = page ?? ApiDefaults.DefaultPage;
        if (requestedPage < ApiDefaults.DefaultPage || requestedPage > ApiDefaults.MaximumPage)
        {
            return ProblemResponse(StatusCodes.Status400BadRequest, "Invalid request",
                $"The 'page' parameter must be a whole number from {ApiDefaults.DefaultPage} to {ApiDefaults.MaximumPage}.");
        }

        return await _handlerFactory.Search(query, requestedPage).HandleAsync(Request);
    }
}
