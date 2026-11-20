namespace MovieSearchCase.WebApi.Application;

/// <summary>Shared API policy values; keep contract limits in one place.</summary>
public static class ApiDefaults
{
    public const string Version = "1.0";
    public const int MajorVersion = 1;
    public const int MinorVersion = 0;
    public const int DefaultPage = 1;
    public const int MaximumPage = 500;
    public const int MaximumSearchQueryLength = 200;
    public const int RequestsPerWindow = 60;
    public const int RateLimitWindowSeconds = 60;
    public const int ErrorLogRetentionDays = 14;
    public const int ErrorLogQueueCapacity = 4096;
    public const string ErrorLogDirectory = "logs";
    public const string VersionedControllerRoute = "api/v{version:apiVersion}/[controller]";
    public const string TvRoute = "api/v{version:apiVersion}/tv";
}
