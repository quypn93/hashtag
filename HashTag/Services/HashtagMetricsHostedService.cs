namespace HashTag.Services;

/// <summary>
/// Background service that calculates hashtag metrics daily
/// Runs at 2 AM every day to calculate difficulty, trending direction, etc.
/// </summary>
public class HashtagMetricsHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HashtagMetricsHostedService> _logger;
    private readonly IConfiguration _configuration;

    public HashtagMetricsHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<HashtagMetricsHostedService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Hashtag Metrics Service started");

        // Run immediately on startup if configured
        var runOnStartup = _configuration.GetValue<bool>("MetricsSettings:RunOnStartup", false);
        if (runOnStartup)
        {
            _logger.LogInformation("Running metrics calculation on startup");
            await CalculateMetricsAsync(stoppingToken);
        }

        // Schedule to run daily at 2 AM
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextRun = new DateTime(now.Year, now.Month, now.Day, 2, 0, 0); // 2 AM today

            // If it's already past 2 AM today, schedule for tomorrow
            if (now > nextRun)
            {
                nextRun = nextRun.AddDays(1);
            }

            var delay = nextRun - now;
            _logger.LogInformation("Next metrics calculation scheduled for {NextRun} (in {Hours}h {Minutes}m)",
                nextRun, delay.Hours, delay.Minutes);

            try
            {
                await Task.Delay(delay, stoppingToken);
                await CalculateMetricsAsync(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Service is stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in metrics calculation schedule");
                // Wait 1 hour before retrying
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        _logger.LogInformation("Hashtag Metrics Service stopped");
    }

    private async Task CalculateMetricsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var metricsService = scope.ServiceProvider.GetRequiredService<IHashtagMetricsService>();

        try
        {
            _logger.LogInformation("Starting metrics calculation");
            var result = await metricsService.CalculateAllMetricsAsync();

            _logger.LogInformation(
                "Metrics calculation completed: {Success}/{Total} successful, {Failed} failed, took {Duration}s",
                result.SuccessfulCalculations,
                result.TotalHashtags,
                result.FailedCalculations,
                result.Duration.TotalSeconds
            );

            if (result.Errors.Count > 0)
            {
                _logger.LogWarning("Metrics calculation had {Count} errors", result.Errors.Count);
                foreach (var error in result.Errors.Take(5)) // Log first 5 errors
                {
                    _logger.LogWarning("Error: {Error}", error);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate metrics");
        }
    }
}
