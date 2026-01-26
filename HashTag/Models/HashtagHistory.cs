namespace HashTag.Models;

public class HashtagHistory
{
    public int Id { get; set; }

    /// <summary>
    /// Reference to the hashtag
    /// </summary>
    public int HashtagId { get; set; }

    /// <summary>
    /// Reference to the source where this was found
    /// </summary>
    public int SourceId { get; set; }

    /// <summary>
    /// Ranking position at the time of collection (1 = top)
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// Date when this ranking was collected (normalized to date only, no time)
    /// </summary>
    public DateTime CollectedDate { get; set; }

    /// <summary>
    /// Exact timestamp when the record was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // NEW METADATA FIELDS (from TikTok Creative Center)

    /// <summary>
    /// Total video views for this hashtag (e.g., 161,981,859)
    /// </summary>
    public long? ViewCount { get; set; }

    /// <summary>
    /// Total posts using this hashtag (e.g., 222,000)
    /// </summary>
    public long? PostCount { get; set; }

    /// <summary>
    /// Category/Industry classification (e.g., "Fashion", "Beauty", "News & Entertainment")
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Trend score from 0-1 indicating popularity trend
    /// </summary>
    public decimal? TrendScore { get; set; }

    /// <summary>
    /// Rank change indicator: "new", "up", "down", "stable"
    /// </summary>
    public string? RankChange { get; set; }

    /// <summary>
    /// Numeric rank difference (e.g., +15 means moved up 15 positions, -5 means dropped 5)
    /// From TikTok API: rank_diff field
    /// </summary>
    public int? RankDiff { get; set; }

    /// <summary>
    /// Whether this hashtag is "viral" (trending_type == 1 from TikTok API)
    /// Indicates explosive/sudden trending vs steady growth
    /// </summary>
    public bool IsViral { get; set; }

    /// <summary>
    /// Phase 2: JSON array of 7-day trend data from TikTok API
    /// Format: [{"time":1766361600,"value":0.83},{"time":1766448000,"value":0.89}...]
    /// Used for sparkline charts and momentum calculation
    /// </summary>
    public string? TrendDataJson { get; set; }

    /// <summary>
    /// Phase 2: Trend momentum score calculated from TrendDataJson
    /// Positive = growing, Negative = declining, 0 = stable
    /// Formula: (last_value - first_value) / first_value * 100
    /// </summary>
    public decimal? TrendMomentum { get; set; }

    /// <summary>
    /// JSON array of featured creators using this hashtag
    /// Format: [{"nickname":"...", "avatar":"..."}]
    /// </summary>
    public string? FeaturedCreatorsJson { get; set; }

    // Navigation properties
    public Hashtag Hashtag { get; set; } = null!;
    public HashtagSource Source { get; set; } = null!;
}
