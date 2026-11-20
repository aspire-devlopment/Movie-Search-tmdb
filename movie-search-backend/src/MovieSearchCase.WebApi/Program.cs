using MovieSearchCase.Infrastructure.Extensions;
using MovieSearchCase.WebApi.Application.Cors;
using MovieSearchCase.WebApi.Application.Middleware;
using MovieSearchCase.WebApi.Application.ProblemDetails;
using MovieSearchCase.WebApi.Application.Logging;
using MovieSearchCase.WebApi.Application.RateLimiting;
using MovieSearchCase.WebApi.Application.Swagger;
using MovieSearchCase.WebApi.Application.Versioning;
using MovieSearchCase.WebApi.Handlers;

var builder = WebApplication.CreateBuilder(args);

// The developer-only token file must never override production secret configuration.
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Development.local.json", optional: true, reloadOnChange: true);
}

var errorLogQueue = new ErrorLogQueue();
// Use portable sinks instead of Windows Event Log, which may be unavailable to local/dev accounts.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddProvider(new ErrorFileLoggerProvider(errorLogQueue));
builder.Services.AddSingleton(errorLogQueue);
builder.Services.AddHostedService<ErrorFileLogWriter>();

builder.Services
    .AddControllers().Services
    .AddCustomProblemDetails()
    .AddCustomSwagger()
    .AddCorsDefaultPolicy(builder.Configuration)
    .AddSupportToApiVersioning()
    .AddApiRateLimiting()
    .AddServices()
    .AddClients(builder.Configuration)
    .AddRequestHandlerFactories();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRequestLogging();
app.UseHttpsRedirection();
app.UseRouting();
app.UseCorsDefaultPolicy();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program
{
}
