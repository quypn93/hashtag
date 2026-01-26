namespace HashTag.Services;

/// <summary>
/// Service for generating and managing sitemap.xml
/// </summary>
public interface ISitemapService
{
    /// <summary>
    /// Generate sitemap XML content with all blog posts and hashtags
    /// </summary>
    Task<string> GenerateSitemapAsync();

    /// <summary>
    /// Get cached sitemap XML or generate new one if expired
    /// </summary>
    Task<string> GetSitemapXmlAsync();

    /// <summary>
    /// Invalidate sitemap cache to force regeneration on next request
    /// </summary>
    void InvalidateCache();
}
