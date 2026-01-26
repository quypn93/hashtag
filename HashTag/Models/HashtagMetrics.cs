namespace HashTag.Models;

/// <summary>
/// Daily metrics snapshot for difficulty scoring and trend analysis
/// </summary>
public class HashtagMetrics
{
    public int Id { get; set; }

    public int HashtagId { get; set; }

    /// <summary>
    /// Date of this metrics snapshot
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Total view count for this hashtag (if available from source)
    /// </summary>
    public long? ViewCount { get; set; }

    /// <summary>
    /// Total posts using this hashtag
    /// </summary>
    public long? PostCount { get; set; }

    /// <summary>
    /// Average engagement rate (likes + comments / views)
    /// </summary>
    public decimal? EngagementRate { get; set; }

    /// <summary>
    /// Calculated difficulty score (1-100, higher = harder to rank)
    /// Easy: 1-33, Medium: 34-66, Hard: 67-100
    /// </summary>
    public int DifficultyScore { get; set; }

    /// <summary>
    /// Growth percentage compared to previous week
    /// </summary>
    public decimal? GrowthRate7d { get; set; }

    /// <summary>
    /// Growth percentage compared to previous month
    /// </summary>
    public decimal? GrowthRate30d { get; set; }

    /// <summary>
    /// Predicted view range min for posts using this hashtag
    /// </summary>
    public long? PredictedViewMin { get; set; }

    /// <summary>
    /// Predicted view range max for posts using this hashtag
    /// </summary>
    public long? PredictedViewMax { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public Hashtag Hashtag { get; set; } = null!;
}
