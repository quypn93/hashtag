namespace HashTag.Models;

/// <summary>
/// Tracks co-occurrence of hashtags for "Related Hashtags" suggestions
/// </summary>
public class HashtagRelation
{
    public int Id { get; set; }

    /// <summary>
    /// First hashtag in the relationship
    /// </summary>
    public int HashtagId1 { get; set; }

    /// <summary>
    /// Second hashtag in the relationship
    /// </summary>
    public int HashtagId2 { get; set; }

    /// <summary>
    /// How many times these hashtags appeared together
    /// </summary>
    public int CoOccurrenceCount { get; set; }

    /// <summary>
    /// Correlation strength (0.0 - 1.0)
    /// 1.0 = always appear together, 0.0 = never
    /// </summary>
    public decimal CorrelationScore { get; set; }

    /// <summary>
    /// Last time this relation was observed
    /// </summary>
    public DateTime LastSeenTogether { get; set; }

    /// <summary>
    /// When this relation record was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last time this relation was updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public Hashtag Hashtag1 { get; set; } = null!;
    public Hashtag Hashtag2 { get; set; } = null!;
}
