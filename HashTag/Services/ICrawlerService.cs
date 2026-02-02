namespace HashTag.Services;

public interface ICrawlerService
{
    /// <summary>
    /// Crawl all active sources and save to database (default region: VN)
    /// </summary>
    Task<CrawlSummary> CrawlAllSourcesAsync();

    /// <summary>
    /// Crawl all active sources for a specific region
    /// </summary>
    Task<CrawlSummary> CrawlAllSourcesAsync(string countryCode);

    /// <summary>
    /// Crawl a specific source by name (default region: VN)
    /// </summary>
    Task<CrawlResult> CrawlSourceAsync(string sourceName);

    /// <summary>
    /// Crawl a specific source by name for a specific region
    /// </summary>
    Task<CrawlResult> CrawlSourceAsync(string sourceName, string countryCode);

    /// <summary>
    /// Get list of supported country codes for regional crawling
    /// </summary>
    IReadOnlyList<RegionInfo> GetSupportedRegions();

    /// <summary>
    /// Crawl all active sources for ALL configured regions (from appsettings.json)
    /// </summary>
    Task<MultiRegionCrawlSummary> CrawlAllRegionsAsync();
}

/// <summary>
/// Information about a supported region for crawling
/// </summary>
public class RegionInfo
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameVi { get; set; } = string.Empty;
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

/// <summary>
/// Summary of crawling across multiple regions
/// </summary>
public class MultiRegionCrawlSummary
{
    public int TotalRegions { get; set; }
    public int SuccessfulRegions { get; set; }
    public int FailedRegions { get; set; }
    public int TotalHashtagsCollected { get; set; }
    public Dictionary<string, CrawlSummary> RegionResults { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
}
