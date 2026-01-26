namespace HashTag.Services;

public interface ICrawlerService
{
    /// <summary>
    /// Crawl all active sources and save to database
    /// </summary>
    Task<CrawlSummary> CrawlAllSourcesAsync();

    /// <summary>
    /// Crawl a specific source by name
    /// </summary>
    Task<CrawlResult> CrawlSourceAsync(string sourceName);
}

public class CrawlResult
{
    public string SourceName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int HashtagsCollected { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class CrawlSummary
{
    public int TotalSources { get; set; }
    public int SuccessfulSources { get; set; }
    public int FailedSources { get; set; }
    public int TotalHashtagsCollected { get; set; }
    public List<CrawlResult> Results { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
}
