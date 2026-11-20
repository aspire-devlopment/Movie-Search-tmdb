using Microsoft.Extensions.Hosting;

namespace MovieSearchCase.WebApi.Application.Logging;

/// <summary>Writes queued errors off the request thread and removes expired daily logs.</summary>
public sealed class ErrorFileLogWriter(
    ErrorLogQueue queue,
    IHostEnvironment environment,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var configuredDirectory = configuration["ErrorLogging:Directory"];
        var logDirectory = Path.GetFullPath(Path.Combine(
            environment.ContentRootPath,
            string.IsNullOrWhiteSpace(configuredDirectory) ? ApiDefaults.ErrorLogDirectory : configuredDirectory));

        try
        {
            Directory.CreateDirectory(logDirectory);
            RemoveExpiredFiles(logDirectory);

            await foreach (var entry in queue.ReadAllAsync(stoppingToken))
            {
                var fileName = $"errors-{DateTime.UtcNow:yyyyMMdd}.jsonl";
                var filePath = Path.Combine(logDirectory, fileName);
                try
                {
                    await File.AppendAllTextAsync(filePath, entry + Environment.NewLine, stoppingToken);
                }
                catch (IOException exception)
                {
                    // Keep the writer alive so a transient file lock does not lose every later error.
                    Console.Error.WriteLine($"Could not append to the error log file '{filePath}': {exception.GetType().Name}");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown; the host is stopping the background writer.
        }
        catch (Exception exception)
        {
            // Do not throw from the log sink and cascade failures back into request handling.
            Console.Error.WriteLine($"The error file log writer stopped unexpectedly: {exception.GetType().Name}");
        }
    }

    private static void RemoveExpiredFiles(string directory)
    {
        var oldestRetainedUtc = DateTime.UtcNow.AddDays(-ApiDefaults.ErrorLogRetentionDays);
        foreach (var file in Directory.EnumerateFiles(directory, "errors-*.jsonl"))
        {
            if (File.GetLastWriteTimeUtc(file) < oldestRetainedUtc)
            {
                File.Delete(file);
            }
        }
    }
}
