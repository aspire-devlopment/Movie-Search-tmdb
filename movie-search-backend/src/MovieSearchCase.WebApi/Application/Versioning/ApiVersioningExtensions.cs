using Asp.Versioning;
using MovieSearchCase.WebApi.Application;

namespace MovieSearchCase.WebApi.Application.Versioning;

public static class ApiVersioningExtensions
{
    public static IServiceCollection AddSupportToApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(ApiDefaults.MajorVersion, ApiDefaults.MinorVersion);
            // The URL segment is the canonical version source; avoid ambiguous overrides.
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
            options.AssumeDefaultVersionWhenUnspecified = false;
            options.ReportApiVersions = true;
        }).AddMvc();

        return services;
    }
}
