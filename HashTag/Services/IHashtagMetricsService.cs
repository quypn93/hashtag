namespace HashTag.Services;

/// <summary>
/// Service for calculating hashtag metrics like difficulty, trending direction, etc.
/// </summary>
public interface IHashtagMetricsService
{
    /// <summary>
    /// Calculate and save metrics for a specific hashtag
    /// </summary>
    Task<bool> CalculateMetricsForHashtagAsync(int hashtagId);

    /// <summary>
    /// Calculate and save metrics for all hashtags with recent data
    /// </summary>
    Task<MetricsCalculationResult> CalculateAllMetricsAsync();

    /// <summary>
    /// Get difficulty level based on view count and post count
    /// </summary>
    string CalculateDifficultyLevel(long? viewCount, long? postCount);

    /// <summary>
    /// Calculate trending direction based on historical data
    /// </summary>
    Task<string> CalculateTrendingDirectionAsync(int hashtagId, int days = 7);

    /// <summary>
    /// Calculate engagement rate (views per post)
    /// </summary>
    decimal CalculateEngagementRate(long? viewCount, long? postCount);

    /// <summary>
    /// Calculate growth rate over specified period
    /// </summary>
    Task<decimal?> CalculateGrowthRateAsync(int hashtagId, int days = 7);
}

/// <summary>
/// Result of metrics calculation batch job
/// </summary>
public class MetricsCalculationResult
{
    public int TotalHashtags { get; set; }
    public int SuccessfulCalculations { get; set; }
    public int FailedCalculations { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public TimeSpan Duration => CompletedAt - StartedAt;
    public List<string> Errors { get; set; } = new();
}
