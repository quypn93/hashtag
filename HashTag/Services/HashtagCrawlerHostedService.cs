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

            // Get regions to crawl from configuration (default: VN only, can add US, UK, etc.)
            var regionsToCrawl = _configuration.GetSection("CrawlerSettings:Regions").Get<string[]>() ?? new[] { "VN" };
            _logger.LogInformation("Regions to crawl: {Regions}", string.Join(", ", regionsToCrawl));

            var totalHashtagsCollected = 0;
            var totalSuccessful = 0;
            var totalFailed = 0;
            var allResultDetails = new List<object>();

            // Step 1: Crawl hashtags for each region
            foreach (var region in regionsToCrawl)
            {
                _logger.LogInformation("=== Crawling region: {Region} ===", region);
                var summary = await crawlerService.CrawlAllSourcesAsync(region);

                _logger.LogInformation(
                    "Region {Region} crawl completed. Success: {Success}, Failed: {Failed}, Total hashtags: {Total}",
                    region, summary.SuccessfulSources, summary.FailedSources, summary.TotalHashtagsCollected);

                totalHashtagsCollected += summary.TotalHashtagsCollected;
                totalSuccessful += summary.SuccessfulSources;
                totalFailed += summary.FailedSources;

                // Log each result for this region
                foreach (var result in summary.Results)
                {
                    if (result.Success)
                    {
                        _logger.LogInformation("  ✓ [{Region}] {Source}: {Count} hashtags", region, result.SourceName, result.HashtagsCollected);
                        allResultDetails.Add(new { Region = region, Source = result.SourceName, Success = true, Count = result.HashtagsCollected });
                    }
                    else
                    {
                        _logger.LogWarning("  ✗ [{Region}] {Source}: {Error}", region, result.SourceName, result.ErrorMessage ?? "Unknown error");
                        allResultDetails.Add(new { Region = region, Source = result.SourceName, Success = false, Error = result.ErrorMessage ?? "Unknown error" });
                    }
                }

                // Small delay between regions to avoid rate limiting
                if (region != regionsToCrawl.Last())
                {
                    _logger.LogInformation("Waiting 30 seconds before crawling next region...");
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
            }

            // Log crawl result to database
            await LogToDatabase(
                totalSuccessful > 0 ? "CrawlSuccess" : "CrawlFailed",
                $"Multi-region crawl completed. Regions: {string.Join(", ", regionsToCrawl)}, Success: {totalSuccessful}, Failed: {totalFailed}, Hashtags: {totalHashtagsCollected}",
                new { Regions = regionsToCrawl, TotalSuccess = totalSuccessful, TotalFailed = totalFailed, TotalHashtags = totalHashtagsCollected, Results = allResultDetails });

            // Step 2: Calculate metrics after successful crawl
            if (totalSuccessful > 0)
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
