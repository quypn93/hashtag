namespace HashTag.Models;

public class CrawlLog
{
    public int Id { get; set; }

    /// <summary>
    /// Reference to the source that was crawled
    /// </summary>
    public int SourceId { get; set; }

    /// <summary>
    /// When the crawl operation started
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the crawl operation completed (null if still running or failed)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Whether the crawl was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Number of hashtags collected during this crawl
    /// </summary>
    public int HashtagsCollected { get; set; }

    /// <summary>
    /// Error message if the crawl failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Detailed log messages during the crawl (JSON array or newline-separated)
    /// </summary>
    public string? LogMessages { get; set; }

    // Navigation properties
    public HashtagSource Source { get; set; } = null!;
}
