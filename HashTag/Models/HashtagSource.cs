namespace HashTag.Models;

public class HashtagSource
{
    public int Id { get; set; }

    /// <summary>
    /// Source name (e.g., "TikTok", "GoogleTrends", "Buffer")
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// URL where hashtags are scraped from
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// Whether this source is currently active for crawling
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Last successful crawl timestamp
    /// </summary>
    public DateTime? LastCrawled { get; set; }

    /// <summary>
    /// Last error message if crawl failed
    /// </summary>
    public string? LastError { get; set; }

    // Navigation properties
    public ICollection<HashtagHistory> History { get; set; } = new List<HashtagHistory>();
    public ICollection<CrawlLog> CrawlLogs { get; set; } = new List<CrawlLog>();
}
