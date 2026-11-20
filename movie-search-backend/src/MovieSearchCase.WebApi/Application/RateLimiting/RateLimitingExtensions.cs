using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace MovieSearchCase.WebApi.Application.RateLimiting;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetClientPartition(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = Application.ApiDefaults.RequestsPerWindow,
                        Window = TimeSpan.FromSeconds(Application.ApiDefaults.RateLimitWindowSeconds),
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry)
                    ? Math.Max(1, (int)Math.Ceiling(retry.TotalSeconds))
                    : Application.ApiDefaults.RateLimitWindowSeconds;

                context.HttpContext.Response.Headers[HeaderNames.RetryAfter] = retryAfter.ToString();
                context.HttpContext.Response.ContentType = "application/problem+json";

                var body = new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Type = "about:blank",
                    Title = "Too many requests",
                    Status = StatusCodes.Status429TooManyRequests,
                    Detail = "The request limit was reached. Try again after the Retry-After interval.",
                };

                await context.HttpContext.Response.WriteAsync(
                    JsonSerializer.Serialize(body), cancellationToken);
            };
        });

        return services;
    }

    // Do not trust X-Forwarded-For unless trusted-proxy middleware is explicitly configured.
    private static string GetClientPartition(HttpContext context) =>
        context.Connection.RemoteIpAddress?.MapToIPv6().ToString() ?? "unknown-client";
}
