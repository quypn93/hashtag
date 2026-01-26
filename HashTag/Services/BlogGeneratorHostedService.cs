using HashTag.Data;
using Microsoft.EntityFrameworkCore;

namespace HashTag.Services;

/// <summary>
/// Background service that automatically generates blog content on schedule:
/// - Monthly report: 1st of each month at 8 AM
/// - Weekly report: Every Monday at 8 AM
/// </summary>
public class BlogGeneratorHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BlogGeneratorHostedService> _logger;
    private readonly IConfiguration _configuration;

    public BlogGeneratorHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<BlogGeneratorHostedService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BlogGeneratorHostedService is starting.");

        var enableAutoGenerate = _configuration.GetValue<bool>("BlogGeneratorSettings:EnableAutoGenerate", true);
        if (!enableAutoGenerate)
        {
            _logger.LogInformation("Auto blog generation is disabled.");
            return;
        }

        var generateHour = _configuration.GetValue<int>("BlogGeneratorSettings:GenerateHour", 8);
        var timeZoneId = _configuration.GetValue<string>("BlogGeneratorSettings:TimeZone", "SE Asia Standard Time");

        _logger.LogInformation("Blog generator scheduled: Monthly (1st) and Weekly (Monday) at {Hour}:00 ({TimeZone})",
            generateHour, timeZoneId);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var (delay, jobType) = GetDelayUntilNextJob(generateHour, timeZoneId);

                _logger.LogInformation("Next blog generation ({JobType}) scheduled in {Hours:F1} hours at {NextTime:yyyy-MM-dd HH:mm:ss}",
                    jobType, delay.TotalHours, DateTime.Now.Add(delay));

                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await ExecuteGenerationAsync(jobType, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("BlogGeneratorHostedService is stopping.");
        }
    }

    private (TimeSpan delay, string jobType) GetDelayUntilNextJob(int generateHour, string timeZoneId)
    {
        TimeZoneInfo timeZone;
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch
        {
            timeZone = TimeZoneInfo.CreateCustomTimeZone("VN", TimeSpan.FromHours(7), "Vietnam", "Vietnam");
        }

        var nowUtc = DateTime.UtcNow;
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, timeZone);

        // Find next scheduled time
        DateTime nextMonthly = GetNextMonthlyDate(nowLocal, generateHour);
        DateTime nextWeekly = GetNextWeeklyDate(nowLocal, generateHour);

        DateTime nextRun;
        string jobType;

        if (nextMonthly <= nextWeekly)
        {
            nextRun = nextMonthly;
            jobType = "monthly";
        }
        else
        {
            nextRun = nextWeekly;
            jobType = "weekly";
        }

        var nextRunUtc = TimeZoneInfo.ConvertTimeToUtc(nextRun, timeZone);
        var delay = nextRunUtc - nowUtc;

        return (delay < TimeSpan.FromMinutes(1) ? TimeSpan.FromMinutes(1) : delay, jobType);
    }

    private static DateTime GetNextMonthlyDate(DateTime now, int hour)
    {
        // Monthly report on the 1st
        var day = 1;

        // Check if we can still run today
        if (now.Day == day && now.Hour < hour)
        {
            return now.Date.AddHours(hour);
        }

        // Next month
        var nextMonth = now.Day >= day ? now.AddMonths(1) : now;
        return new DateTime(nextMonth.Year, nextMonth.Month, day, hour, 0, 0);
    }

    private static DateTime GetNextWeeklyDate(DateTime now, int hour)
    {
        // Weekly report on Monday
        var targetDay = DayOfWeek.Monday;
        int daysUntilTarget = ((int)targetDay - (int)now.DayOfWeek + 7) % 7;

        // If today is the target day and we haven't passed the hour yet
        if (daysUntilTarget == 0 && now.Hour < hour)
        {
            return now.Date.AddHours(hour);
        }

        // If today is target day but hour passed, go to next week
        if (daysUntilTarget == 0)
        {
            daysUntilTarget = 7;
        }

        return now.Date.AddDays(daysUntilTarget).AddHours(hour);
    }

    private async Task ExecuteGenerationAsync(string jobType, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("=== Starting blog generation ({JobType}) at {Time} ===", jobType, DateTime.Now);

            using var scope = _scopeFactory.CreateScope();
            var blogGenerator = scope.ServiceProvider.GetRequiredService<IBlogAutoGeneratorService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<TrendTagDbContext>();

            switch (jobType)
            {
                case "monthly":
                    // Generate report for previous month
                    var lastMonth = DateTime.Now.AddMonths(-1);
                    var post = await blogGenerator.GenerateMonthlyTopHashtagsAsync(lastMonth.Month, lastMonth.Year);
                    if (post != null)
                    {
                        _logger.LogInformation("Generated monthly report: {Title}", post.Title);
                    }

                    // Also generate posts for each hashtag category
                    await GenerateCategoryPostsAsync(blogGenerator, dbContext);
                    break;

                case "weekly":
                    var weeklyPost = await blogGenerator.GenerateWeeklyTrendingReportAsync();
                    if (weeklyPost != null)
                    {
                        _logger.LogInformation("Generated weekly report: {Title}", weeklyPost.Title);
                    }
                    break;
            }

            _logger.LogInformation("=== Blog generation completed at {Time} ===", DateTime.Now);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during blog generation: {Message}", ex.Message);
        }
    }

    private async Task GenerateCategoryPostsAsync(IBlogAutoGeneratorService blogGenerator, TrendTagDbContext dbContext)
    {
        try
        {
            // Get all active hashtag categories that have hashtags with data
            var categories = await dbContext.HashtagCategories
                .Where(c => c.IsActive && c.Hashtags.Any(h => h.IsActive && h.LatestViewCount > 0))
                .ToListAsync();

            _logger.LogInformation("Found {Count} categories with active hashtags for blog generation", categories.Count);

            foreach (var category in categories)
            {
                try
                {
                    var post = await blogGenerator.GenerateCategoryTopHashtagsAsync(category.Id);
                    if (post != null)
                    {
                        _logger.LogInformation("Generated category post: {Title}", post.Title);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error generating post for category {Category}: {Message}",
                        category.Name, ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating category posts: {Message}", ex.Message);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BlogGeneratorHostedService is stopping gracefully.");
        await base.StopAsync(cancellationToken);
    }
}
