using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Text.Json;
using MovieSearchCase.Domain.Exceptions;

namespace MovieSearchCase.WebApi.Application.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext, ProblemDetailsFactory problemDetailsFactory)
    {
        try
        {
            await _next(httpContext);
        }
        catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
        {
            // A disconnected caller is not an application error; do not write to its closed response.
            _logger.LogDebug("Request to {Path} was cancelled by the caller", httpContext.Request.Path);
        }
        catch (Exception exception)
        {
            if (httpContext.Response.HasStarted)
            {
                _logger.LogError(exception, "Request to {Path} failed after the response started", httpContext.Request.Path);
                throw;
            }

            var (statusCode, title, detail, instance) = exception switch
            {
                ProblemDetailsException problemDetailsException => (
                    problemDetailsException.ErrorCode.ToStatusCode(),
                    problemDetailsException.Title,
                    problemDetailsException.Details,
                    problemDetailsException.Instance.ToString()),
                HttpRequestException => (
                    StatusCodes.Status503ServiceUnavailable,
                    "Upstream service unavailable",
                    "A required upstream service could not complete the request.",
                    httpContext.Request.Path.ToString()),
                _ => (
                    StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred",
                    "The server could not complete the request.",
                    httpContext.Request.Path.ToString()),
            };

            _logger.LogError(exception, "Request to {Path} failed with status {StatusCode}", httpContext.Request.Path, statusCode);

            var problemDetails = problemDetailsFactory.CreateProblemDetails(
                httpContext,
                statusCode,
                title,
                instance: instance,
                detail: detail);

            problemDetails.Type = "about:blank";
            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/problem+json";
            await httpContext.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
        }
    }
}

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestLoggingMiddleware>();
}
