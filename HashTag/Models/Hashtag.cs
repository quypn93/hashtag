namespace HashTag.Models;

public class Hashtag
{
    public int Id { get; set; }

    /// <summary>
    /// Normalized hashtag without # symbol (e.g., "fyp", "trending")
    /// </summary>
    public required string Tag { get; set; }

    /// <summary>
    /// Display version with # symbol (e.g., "#fyp", "#trending")
    /// </summary>
    public required string TagDisplay { get; set; }

    /// <summary>
    /// Country/region code for this hashtag data (e.g., "VN", "US", "UK")
    /// Default is "VN" for backwards compatibility
    /// </summary>
    public string CountryCode { get; set; } = "VN";

    /// <summary>
    /// First time this hashtag was seen in any source
    /// </summary>
    public DateTime FirstSeen { get; set; }

    /// <summary>
    /// Most recent time this hashtag appeared
    /// </summary>
    public DateTime LastSeen { get; set; }

    /// <summary>
    /// Whether this hashtag is still active/being tracked
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Total number of appearances across all sources
    /// </summary>
    public int TotalAppearances { get; set; }

    /// <summary>
    /// Category ID for content classification
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Current difficulty level (cached from latest metrics)
    /// </summary>
    public string? DifficultyLevel { get; set; } // "Easy", "Medium", "Hard"

    /// <summary>
    /// Latest view count (cached for quick access)
    /// </summary>
    public long? LatestViewCount { get; set; }

    /// <summary>
    /// Latest post count (cached for quick access)
    /// </summary>
    public long? LatestPostCount { get; set; }

    // Navigation properties
    public HashtagCategory? Category { get; set; }
    public ICollection<HashtagHistory> History { get; set; } = new List<HashtagHistory>();
    public ICollection<HashtagMetrics> Metrics { get; set; } = new List<HashtagMetrics>();
    public ICollection<HashtagKeyword> Keywords { get; set; } = new List<HashtagKeyword>();

    // Relations where this hashtag is either side
    public ICollection<HashtagRelation> RelationsAsHashtag1 { get; set; } = new List<HashtagRelation>();
    public ICollection<HashtagRelation> RelationsAsHashtag2 { get; set; } = new List<HashtagRelation>();
}
