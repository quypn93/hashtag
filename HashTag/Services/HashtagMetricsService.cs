using HashTag.Models;
using HashTag.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace HashTag.Services;

public class HashtagMetricsService : IHashtagMetricsService
{
    private readonly IHashtagRepository _repository;
    private readonly ILogger<HashtagMetricsService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public HashtagMetricsService(
        IHashtagRepository repository,
        ILogger<HashtagMetricsService> logger,
        IServiceProvider serviceProvider)
    {
        _repository = repository;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<bool> CalculateMetricsForHashtagAsync(int hashtagId)
    {
        try
        {
            var hashtag = await _repository.GetHashtagByIdAsync(hashtagId);
            if (hashtag == null)
            {
                _logger.LogWarning("Hashtag {Id} not found", hashtagId);
                return false;
            }

            // Get latest history data with metadata
            var latestHistory = await _repository.GetLatestHistoryWithMetadataAsync(hashtagId);
            if (latestHistory == null)
            {
                _logger.LogDebug("No history metadata available for hashtag {Id}", hashtagId);
                return false;
            }

            // Calculate metrics based on ViewCount AND PostCount from TikTok API
            var difficultyLevel = CalculateDifficultyLevel(latestHistory.ViewCount, latestHistory.PostCount);

            // IMPORTANT: Execute these sequentially to avoid DbContext concurrency issues
            var trendingDirection = await CalculateTrendingDirectionAsync(hashtagId, 7);
            var growthRate7d = await CalculateGrowthRateAsync(hashtagId, 7);
            var growthRate30d = await CalculateGrowthRateAsync(hashtagId, 30);

            // Calculate difficulty score (1-100) based on BOTH ViewCount and PostCount
            var difficultyScore = CalculateDifficultyScore(latestHistory.ViewCount, latestHistory.PostCount);

            // Calculate engagement rate if both metrics available
            var engagementRate = CalculateEngagementRate(latestHistory.ViewCount, latestHistory.PostCount);

            // Predict view range for new posts
            var (predictedMin, predictedMax) = CalculatePredictedViewRange(
                latestHistory.ViewCount,
                latestHistory.PostCount
            );

            // Create or update metrics record for today
            var metrics = new HashtagMetrics
            {
                HashtagId = hashtagId,
                Date = DateTime.UtcNow.Date,
                ViewCount = latestHistory.ViewCount,
                PostCount = latestHistory.PostCount,
                EngagementRate = engagementRate,
                DifficultyScore = difficultyScore,
                GrowthRate7d = growthRate7d,
                GrowthRate30d = growthRate30d,
                PredictedViewMin = predictedMin,
                PredictedViewMax = predictedMax,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddOrUpdateMetricsAsync(metrics);

            // Update cached DifficultyLevel in Hashtag entity for quick search access
            hashtag.DifficultyLevel = difficultyLevel;
            hashtag.LatestViewCount = latestHistory.ViewCount;
            hashtag.LatestPostCount = latestHistory.PostCount;
            await _repository.UpdateHashtagAsync(hashtag);

            // Calculate related hashtags (co-occurrence analysis)
            // DISABLED: Too slow for large datasets - runs O(n²) queries
            // TODO: Optimize this with bulk processing or run separately as background job
            // await CalculateRelatedHashtagsAsync(hashtagId);

            _logger.LogInformation(
                "Calculated metrics for #{Tag}: Difficulty={Difficulty}, Trending={Trending}, PostCount={PostCount}",
                hashtag.Tag, difficultyLevel, trendingDirection, latestHistory.PostCount
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating metrics for hashtag {Id}", hashtagId);
            return false;
        }
    }

    public async Task<MetricsCalculationResult> CalculateAllMetricsAsync()
    {
        var result = new MetricsCalculationResult
        {
            StartedAt = DateTime.UtcNow
        };

        try
        {
            // Get all hashtags with recent data (last 30 days)
            var recentHashtags = await _repository.GetHashtagsWithRecentDataAsync(30);
            result.TotalHashtags = recentHashtags.Count;

            _logger.LogInformation(
                "Starting metrics calculation for {Count} hashtags with ViewCount and PostCount from TikTok API",
                result.TotalHashtags
            );

            // Process in parallel batches to improve performance
            var batchSize = 10; // Process 10 hashtags at a time
            var batches = recentHashtags
                .Select((hashtag, index) => new { hashtag, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.hashtag).ToList())
                .ToList();

            _logger.LogInformation("Processing {Count} hashtags in {Batches} batches of {BatchSize}",
                result.TotalHashtags, batches.Count, batchSize);

            int batchNumber = 0;
            foreach (var batch in batches)
            {
                batchNumber++;
                _logger.LogInformation("Processing batch {Current}/{Total}...", batchNumber, batches.Count);

                // Process batch in parallel with separate scope per task (fix DbContext concurrency)
                var batchStartTime = DateTime.UtcNow;
                var tasks = batch.Select(async hashtag =>
                {
                    // Create new scope for each parallel task to avoid DbContext sharing
                    using var scope = _serviceProvider.CreateScope();
                    var metricsService = scope.ServiceProvider.GetRequiredService<IHashtagMetricsService>();
                    var success = await metricsService.CalculateMetricsForHashtagAsync(hashtag.Id);
                    return success;
                }).ToList();

                var results = await Task.WhenAll(tasks);

                result.SuccessfulCalculations += results.Count(r => r);
                result.FailedCalculations += results.Count(r => !r);

                var batchDuration = (DateTime.UtcNow - batchStartTime).TotalSeconds;
                _logger.LogInformation("Batch {Current}/{Total} completed in {Duration:F2}s: {Success} successful, {Failed} failed",
                    batchNumber, batches.Count, batchDuration, results.Count(r => r), results.Count(r => !r));
            }

            result.CompletedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Metrics calculation completed: {Success}/{Total} successful in {Duration}s",
                result.SuccessfulCalculations,
                result.TotalHashtags,
                result.Duration.TotalSeconds
            );

            // Calculate related hashtags in batch (MUCH faster than per-hashtag)
            _logger.LogInformation("Starting batch related hashtags calculation...");
            var relatedStartTime = DateTime.UtcNow;
            await CalculateAllRelatedHashtagsBatchAsync();
            var relatedDuration = (DateTime.UtcNow - relatedStartTime).TotalSeconds;
            _logger.LogInformation("Batch related hashtags calculation completed in {Duration:F2}s", relatedDuration);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during batch metrics calculation");
            result.CompletedAt = DateTime.UtcNow;
            result.Errors.Add(ex.Message);
            return result;
        }
    }

    public string CalculateDifficultyLevel(long? viewCount, long? postCount)
    {
        // Calculate difficulty using BOTH ViewCount and PostCount from TikTok API
        // This provides more accurate difficulty assessment

        if (!viewCount.HasValue && !postCount.HasValue)
            return "Unknown";

        // Calculate difficulty score
        int score = CalculateDifficultyScore(viewCount, postCount);

        // Map score (1-100) to difficulty levels
        return score switch
        {
            <= 25 => "Easy",           // Very low competition
            <= 50 => "Medium",         // Moderate competition
            <= 75 => "Hard",           // High competition
            _ => "Very Hard"           // Extremely competitive
        };
    }

    public async Task<string> CalculateTrendingDirectionAsync(int hashtagId, int days = 7)
    {
        try
        {
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-days);

            var history = await _repository.GetHashtagHistoryInPeriodAsync(hashtagId, startDate, endDate);

            if (history.Count < 2)
                return "Unknown";

            // Use PostCount instead of ViewCount for trending
            var postCounts = history
                .Where(h => h.PostCount.HasValue)
                .OrderBy(h => h.CollectedDate)
                .Select(h => h.PostCount!.Value)
                .ToList();

            if (postCounts.Count < 2)
                return "Unknown";

            // Calculate trend: compare recent average vs older average
            var midPoint = postCounts.Count / 2;
            var olderAvg = postCounts.Take(midPoint).Average();
            var recentAvg = postCounts.Skip(midPoint).Average();

            var growthPercent = ((recentAvg - olderAvg) / olderAvg) * 100;

            return growthPercent switch
            {
                > 10 => "Rising",      // >10% growth in posts
                < -10 => "Falling",    // >10% decline in posts
                _ => "Stable"          // ±10% range
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error calculating trending direction for {Id}: {Message}", hashtagId, ex.Message);
            return "Unknown";
        }
    }

    public decimal CalculateEngagementRate(long? viewCount, long? postCount)
    {
        // Calculate engagement rate: ViewCount / PostCount
        // This represents average views per post using this hashtag
        // Higher engagement rate = better performance potential

        if (!viewCount.HasValue || !postCount.HasValue || postCount.Value == 0)
            return 0;

        // Average views per post (in thousands for readability)
        var avgViewsPerPost = (decimal)viewCount.Value / postCount.Value;

        // Return as a metric (views per post)
        return Math.Round(avgViewsPerPost, 2);
    }

    public async Task<decimal?> CalculateGrowthRateAsync(int hashtagId, int days = 7)
    {
        try
        {
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-days);

            var history = await _repository.GetHashtagHistoryInPeriodAsync(hashtagId, startDate, endDate);

            // Use PostCount instead of ViewCount for growth rate
            var postCounts = history
                .Where(h => h.PostCount.HasValue)
                .OrderBy(h => h.CollectedDate)
                .Select(h => h.PostCount!.Value)
                .ToList();

            if (postCounts.Count < 2)
                return null;

            var firstValue = postCounts.First();
            var lastValue = postCounts.Last();

            if (firstValue == 0)
                return null;

            // Growth rate as percentage (based on PostCount)
            return ((decimal)(lastValue - firstValue) / firstValue) * 100;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error calculating growth rate for {Id}: {Message}", hashtagId, ex.Message);
            return null;
        }
    }

    private int CalculateDifficultyScore(long? viewCount, long? postCount)
    {
        // Calculate difficulty score (1-100) using BOTH ViewCount and PostCount
        // Lower score = easier (good for small creators)
        // Higher score = harder (very competitive)

        if (!viewCount.HasValue && !postCount.HasValue)
            return 50; // Default to medium

        int competitionScore = 0;
        int popularityScore = 0;

        // 1. Competition score based on POST count (0-50 points)
        // More posts = more competition = harder
        if (postCount.HasValue)
        {
            competitionScore = postCount.Value switch
            {
                < 500 => 5,           // Very few posts - very low competition
                < 1_000 => 10,        // <1K posts - low competition
                < 5_000 => 15,        // 1K-5K posts - moderate-low
                < 10_000 => 20,       // 5K-10K posts - moderate
                < 50_000 => 30,       // 10K-50K posts - moderate-high
                < 100_000 => 38,      // 50K-100K posts - high
                < 500_000 => 45,      // 100K-500K posts - very high
                _ => 50               // >500K posts - extremely high competition
            };
        }

        // 2. Popularity score based on VIEW count (0-50 points)
        // More views = more popular = harder to stand out
        if (viewCount.HasValue)
        {
            popularityScore = viewCount.Value switch
            {
                < 100_000 => 5,                // <100K views - very niche
                < 1_000_000 => 10,             // <1M views - niche
                < 10_000_000 => 15,            // <10M views - moderate-low
                < 50_000_000 => 20,            // 10-50M views - moderate
                < 100_000_000 => 30,           // 50-100M views - moderate-high
                < 500_000_000 => 38,           // 100-500M views - high
                < 1_000_000_000 => 45,         // 500M-1B views - very high
                _ => 50                        // >1B views - mega popular
            };
        }

        // Total score = competition + popularity
        var totalScore = competitionScore + popularityScore;

        // Clamp to 1-100 range
        return Math.Clamp(totalScore, 1, 100);
    }

    private (long? min, long? max) CalculatePredictedViewRange(
        long? avgViewCount,
        long? avgPostCount)
    {
        if (!avgViewCount.HasValue || !avgPostCount.HasValue || avgPostCount.Value == 0)
            return (null, null);

        // Predict view range for a new post using this hashtag
        // Based on average views per post with variance

        // Average views per post
        var avgViewsPerPost = avgViewCount.Value / avgPostCount.Value;

        // Conservative estimate: 50% of average (lower bound)
        var minViews = (long)(avgViewsPerPost * 0.5m);

        // Optimistic estimate: 150% of average (upper bound for viral potential)
        var maxViews = (long)(avgViewsPerPost * 1.5m);

        return (minViews, maxViews);
    }

    /// <summary>
    /// Calculate related hashtags for ALL hashtags in one batch operation (OPTIMIZED)
    /// This is MUCH faster than calling CalculateRelatedHashtagsAsync for each hashtag
    /// </summary>
    private async Task CalculateAllRelatedHashtagsBatchAsync()
    {
        try
        {
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-30);

            _logger.LogInformation("Loading all history from {Start} to {End}...", startDate, endDate);

            // Get ALL history in one query (instead of N queries)
            var allHistory = await _repository.GetAllHistoryInPeriodAsync(startDate, endDate);

            if (!allHistory.Any())
            {
                _logger.LogWarning("No history found for calculating related hashtags");
                return;
            }

            _logger.LogInformation("Processing {Count} history records to find co-occurrences...", allHistory.Count);

            // Group by (Date, SourceId) to find hashtags appearing together
            var dateSourceGroups = allHistory
                .GroupBy(h => new { h.CollectedDate.Date, h.SourceId })
                .ToList();

            _logger.LogInformation("Found {Count} date-source groups", dateSourceGroups.Count);

            // For each hashtag, find which other hashtags co-occurred with it
            var hashtagCoOccurrences = new Dictionary<int, Dictionary<int, int>>();

            foreach (var group in dateSourceGroups)
            {
                var hashtagsInGroup = group.Select(h => h.HashtagId).Distinct().ToList();

                // For each pair of hashtags in this group, increment co-occurrence count
                for (int i = 0; i < hashtagsInGroup.Count; i++)
                {
                    for (int j = i + 1; j < hashtagsInGroup.Count; j++)
                    {
                        var hashtag1 = hashtagsInGroup[i];
                        var hashtag2 = hashtagsInGroup[j];

                        // Track co-occurrence for hashtag1 -> hashtag2
                        if (!hashtagCoOccurrences.ContainsKey(hashtag1))
                            hashtagCoOccurrences[hashtag1] = new Dictionary<int, int>();

                        if (!hashtagCoOccurrences[hashtag1].ContainsKey(hashtag2))
                            hashtagCoOccurrences[hashtag1][hashtag2] = 0;

                        hashtagCoOccurrences[hashtag1][hashtag2]++;

                        // Track co-occurrence for hashtag2 -> hashtag1 (symmetric)
                        if (!hashtagCoOccurrences.ContainsKey(hashtag2))
                            hashtagCoOccurrences[hashtag2] = new Dictionary<int, int>();

                        if (!hashtagCoOccurrences[hashtag2].ContainsKey(hashtag1))
                            hashtagCoOccurrences[hashtag2][hashtag1] = 0;

                        hashtagCoOccurrences[hashtag2][hashtag1]++;
                    }
                }
            }

            _logger.LogInformation("Found co-occurrences for {Count} hashtags", hashtagCoOccurrences.Count);

            // Save relations (only keep pairs that co-occurred at least 2 times)
            int totalRelations = 0;
            foreach (var kvp in hashtagCoOccurrences)
            {
                // ⚠️ DISABLED: Relations table grows too large and causes performance issues
                // This feature is not critical and can be disabled to improve metrics calculation speed

                // var hashtagId = kvp.Key;
                // var relatedHashtags = kvp.Value
                //     .Where(r => r.Value >= 2) // Must co-occur at least 2 times
                //     .Select(r => r.Key)
                //     .ToList();

                // foreach (var relatedHashtagId in relatedHashtags)
                // {
                //     await _repository.AddOrUpdateRelationAsync(hashtagId, relatedHashtagId);
                //     totalRelations++;
                // }
            }

            _logger.LogInformation("Relations calculation disabled to improve performance (was: {Count} relations)", totalRelations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating related hashtags in batch: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Calculate related hashtags based on co-occurrence in same time periods from same source
    /// DEPRECATED: Use CalculateAllRelatedHashtagsBatchAsync instead for better performance
    /// </summary>
    private async Task CalculateRelatedHashtagsAsync(int hashtagId)
    {
        try
        {
            // Get history for this hashtag in the last 30 days
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-30);

            var thisHashtagHistory = await _repository.GetHashtagHistoryInPeriodAsync(hashtagId, startDate, endDate);

            if (!thisHashtagHistory.Any())
            {
                _logger.LogDebug("No history found for hashtag {Id} to calculate relations", hashtagId);
                return;
            }

            // Get all history in the same period
            var allHistory = await _repository.GetAllHistoryInPeriodAsync(startDate, endDate);

            // Group by (Date, SourceId) to find hashtags appearing together
            var thisHashtagDates = thisHashtagHistory
                .Select(h => new { h.CollectedDate.Date, h.SourceId })
                .Distinct()
                .ToHashSet();

            // Find hashtags that appeared on same dates from same sources
            var relatedHashtagIds = allHistory
                .Where(h => h.HashtagId != hashtagId &&
                           thisHashtagDates.Contains(new { h.CollectedDate.Date, h.SourceId }))
                .GroupBy(h => h.HashtagId)
                .Where(g => g.Count() >= 2) // Must co-occur at least 2 times
                .Select(g => g.Key)
                .ToList();

            // ⚠️ DISABLED: Relations table grows too large and causes performance issues
            // Create or update relations
            // foreach (var relatedHashtagId in relatedHashtagIds)
            // {
            //     await _repository.AddOrUpdateRelationAsync(hashtagId, relatedHashtagId);
            // }

            if (relatedHashtagIds.Any())
            {
                _logger.LogDebug("Found {Count} related hashtags for hashtag {Id} (relations disabled)", relatedHashtagIds.Count, hashtagId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating related hashtags for {Id}: {Message}", hashtagId, ex.Message);
        }
    }
}
