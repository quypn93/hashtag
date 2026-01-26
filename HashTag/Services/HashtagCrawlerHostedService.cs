using HashTag.Data;
using HashTag.Models;
using System.Text.Json;

namespace HashTag.Services;

public class HashtagCrawlerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HashtagCrawlerHostedService> _logger;
    private readonly IConfiguration _configuration;
    private const string ServiceName = "HashtagCrawlerHostedService";

    // Default schedule: 2AM and 2PM Vietnam time (UTC+7)
    private static readonly int[] DefaultScheduleHours = [2, 14];

    public HashtagCrawlerHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<HashtagCrawlerHostedService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    private async Task LogToDatabase(string eventType, string message, object? additionalData = null)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TrendTagDbContext>();

            dbContext.SystemLogs.Add(new SystemLog
            {
                ServiceName = ServiceName,
                EventType = eventType,
                Message = message,
                Timestamp = DateTime.UtcNow,
                AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null
            });

            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write to SystemLog: {Message}", ex.Message);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("HashtagCrawlerHostedService is starting.");
        _logger.LogInformation("========================================");

        // Check if crawler is enabled
        var isEnabled = _configuration.GetValue<bool>("CrawlerSettings:Enabled", true);
        if (!isEnabled)
        {
            _logger.LogWarning("Auto crawl is DISABLED via configuration. Service will not run.");
            await LogToDatabase("Disabled", "Auto crawl is DISABLED via configuration");
            return;
        }

        // Get configuration
        var runOnStartup = _configuration.GetValue<bool>("CrawlerSettings:RunOnStartup", false);
        var scheduleHours = _configuration.GetSection("CrawlerSettings:ScheduleHours").Get<int[]>() ?? DefaultScheduleHours;
        var timeZoneId = _configuration.GetValue<string>("CrawlerSettings:TimeZone", "SE Asia Standard Time");

        var configInfo = new
        {
            RunOnStartup = runOnStartup,
            ScheduleHours = scheduleHours,
            TimeZone = timeZoneId,
            MachineName = Environment.MachineName,
            ProcessId = Environment.ProcessId
        };

        _logger.LogInformation("Crawler config - RunOnStartup: {RunOnStartup}, ScheduleHours: [{Hours}], TimeZone: {TimeZone}",
            runOnStartup, string.Join(", ", scheduleHours.Select(h => $"{h}:00")), timeZoneId);

        await LogToDatabase("Started", "Service started successfully", configInfo);

        // Optional: Run immediately on startup
        if (runOnStartup && !stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Running initial crawl on startup...");
            await LogToDatabase("RunOnStartup", "Running initial crawl on startup");
            await ExecuteCrawlAsync(stoppingToken);
        }

        // Schedule crawling at fixed times
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var delay = GetDelayUntilNextScheduledTime(scheduleHours, timeZoneId);
                var nextRunTime = DateTime.Now.Add(delay);

                _logger.LogInformation("Next crawl scheduled in {Hours:F1} hours at {NextTime:yyyy-MM-dd HH:mm:ss}",
                    delay.TotalHours, nextRunTime);

                await LogToDatabase("Scheduled", $"Next crawl in {delay.TotalHours:F1} hours", new
                {
                    DelayHours = delay.TotalHours,
                    NextRunTime = nextRunTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });

                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await LogToDatabase("Executing", "Starting scheduled crawl");
                    await ExecuteCrawlAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("HashtagCrawlerHostedService is stopping (cancellation requested).");
                await LogToDatabase("Stopping", "Service stopping due to cancellation request");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in crawler loop: {Message}. Retrying in 5 minutes...", ex.Message);
                await LogToDatabase("Error", $"Loop error: {ex.Message}", new
                {
                    ExceptionType = ex.GetType().FullName,
                    StackTrace = ex.StackTrace?.Substring(0, Math.Min(ex.StackTrace.Length, 1000))
                });

                // Wait 5 minutes before retrying to avoid tight loop on persistent errors
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    await LogToDatabase("Stopping", "Service stopping during error recovery wait");
                    break;
                }
            }
        }

        await LogToDatabase("Stopped", "Service has stopped");
        _logger.LogInformation("HashtagCrawlerHostedService has stopped.");
    }

    private TimeSpan GetDelayUntilNextScheduledTime(int[] scheduleHours, string timeZoneId)
    {
        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            _logger.LogDebug("Using timezone: {TimeZone}", timeZone.DisplayName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Timezone '{TimeZoneId}' not found, using UTC+7 fallback", timeZoneId);
            // Fallback to UTC+7 offset if timezone not found
            timeZone = TimeZoneInfo.CreateCustomTimeZone("VN", TimeSpan.FromHours(7), "Vietnam", "Vietnam");
        }

        var nowUtc = DateTime.UtcNow;
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone);

        _logger.LogDebug("Current time - UTC: {UtcTime}, Local: {LocalTime}", nowUtc, nowLocal);

        // Find next scheduled time
        DateTime nextRun = DateTime.MaxValue;
        foreach (var hour in scheduleHours.OrderBy(h => h))
        {
            var scheduledToday = nowLocal.Date.AddHours(hour);
            if (scheduledToday > nowLocal)
            {
                if (scheduledToday < nextRun)
                    nextRun = scheduledToday;
            }
        }

        // If no more runs today, schedule for first run tomorrow
        if (nextRun == DateTime.MaxValue)
        {
            var firstHourTomorrow = scheduleHours.Min();
            nextRun = nowLocal.Date.AddDays(1).AddHours(firstHourTomorrow);
        }

        // Convert back to UTC and calculate delay
        var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(nextRun, timeZone);
        var delay = nextRunUtc - nowUtc;

        _logger.LogDebug("Next run scheduled at {NextRunLocal} (local) / {NextRunUtc} (UTC), delay: {Delay}",
            nextRun, nextRunUtc, delay);

        // Minimum delay of 1 minute to avoid tight loops
        return delay < TimeSpan.FromMinutes(1) ? TimeSpan.FromMinutes(1) : delay;
    }

    private async Task ExecuteCrawlAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== Starting scheduled crawl at {Time} ===", DateTime.Now);

        try
        {
            _logger.LogInformation("Creating service scope...");
            using var scope = _scopeFactory.CreateScope();

            _logger.LogInformation("Resolving ICrawlerService...");
            var crawlerService = scope.ServiceProvider.GetRequiredService<ICrawlerService>();

            // Step 1: Crawl hashtags
            _logger.LogInformation("Calling CrawlAllSourcesAsync...");
            var summary = await crawlerService.CrawlAllSourcesAsync();

            _logger.LogInformation(
                "Crawl completed. Success: {Success}, Failed: {Failed}, Total hashtags: {Total}",
                summary.SuccessfulSources,
                summary.FailedSources,
                summary.TotalHashtagsCollected);

            // Log each result
            var resultDetails = new List<object>();
            foreach (var result in summary.Results)
            {
                if (result.Success)
                {
                    _logger.LogInformation("  ✓ {Source}: {Count} hashtags", result.SourceName, result.HashtagsCollected);
                    resultDetails.Add(new { Source = result.SourceName, Success = true, Count = result.HashtagsCollected });
                }
                else
                {
                    _logger.LogWarning("  ✗ {Source}: {Error}", result.SourceName, result.ErrorMessage ?? "Unknown error");
                    resultDetails.Add(new { Source = result.SourceName, Success = false, Error = result.ErrorMessage ?? "Unknown error" });
                }
            }

            // Log crawl result to database
            await LogToDatabase(
                summary.SuccessfulSources > 0 ? "CrawlSuccess" : "CrawlFailed",
                $"Crawl completed. Success: {summary.SuccessfulSources}, Failed: {summary.FailedSources}, Hashtags: {summary.TotalHashtagsCollected}",
                new { Summary = summary, Results = resultDetails });

            // Step 2: Calculate metrics after successful crawl
            if (summary.SuccessfulSources > 0)
            {
                _logger.LogInformation("Starting metrics calculation...");
                var metricsService = scope.ServiceProvider.GetRequiredService<IHashtagMetricsService>();
                await metricsService.CalculateAllMetricsAsync();
                _logger.LogInformation("Metrics calculation completed.");
            }

            _logger.LogInformation("=== Scheduled job completed at {Time} ===", DateTime.Now);
            await LogToDatabase("Completed", "Scheduled job completed successfully");
        }
        catch (Exception ex)
        {
            // Log full exception details including inner exception and stack trace
            _logger.LogError(ex,
                "ERROR during scheduled crawl!\n" +
                "Message: {Message}\n" +
                "Type: {ExceptionType}\n" +
                "InnerException: {InnerMessage}\n" +
                "StackTrace: {StackTrace}",
                ex.Message,
                ex.GetType().FullName,
                ex.InnerException?.Message ?? "None",
                ex.StackTrace);

            // Also log to database so we can see the error in admin panel
            await LogToDatabase("CrawlError", $"Error during scheduled crawl: {ex.Message}", new
            {
                ExceptionType = ex.GetType().FullName,
                Message = ex.Message,
                InnerException = ex.InnerException?.Message,
                StackTrace = ex.StackTrace?.Substring(0, Math.Min(ex.StackTrace?.Length ?? 0, 1500))
            });
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("HashtagCrawlerHostedService is stopping gracefully.");
        await LogToDatabase("StopAsync", "StopAsync called - graceful shutdown initiated");
        await base.StopAsync(cancellationToken);
    }
}
