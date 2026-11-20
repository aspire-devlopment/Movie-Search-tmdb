using Microsoft.AspNetCore.Mvc;

namespace MovieSearchCase.WebApi.Controllers;

[ApiController]
public class ApiControllerBase : ControllerBase
{
    // Success responses use MVC's JSON default; error results set the problem media type.
    protected ObjectResult ProblemResponse(int status, string title, string detail)
    {
        return new ObjectResult(new ProblemDetails
        {
            Type = "about:blank",
            Title = title,
            Status = status,
            Detail = detail,
        })
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" },
        };
    }
}
