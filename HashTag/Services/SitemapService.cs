using HashTag.Repositories;
using System.Text;
using System.Xml;

namespace HashTag.Services;

/// <summary>
/// Service for generating SEO-friendly sitemap.xml
/// Includes all blog posts and hashtag pages
/// </summary>
public class SitemapService : ISitemapService
{
    private readonly IHashtagRepository _hashtagRepository;
    private readonly IBlogRepository _blogRepository;
    private readonly ILogger<SitemapService> _logger;
    private readonly IConfiguration _configuration;

    // Cache for sitemap to avoid regenerating on every request
    private static string? _cachedSitemap;
    private static DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly object _cacheLock = new object();
    private const int CacheMinutes = 60; // Cache for 1 hour

    public SitemapService(
        IHashtagRepository hashtagRepository,
        IBlogRepository blogRepository,
        ILogger<SitemapService> logger,
        IConfiguration configuration)
    {
        _hashtagRepository = hashtagRepository;
        _blogRepository = blogRepository;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Get cached sitemap or generate new one if expired
    /// </summary>
    public async Task<string> GetSitemapXmlAsync()
    {
        lock (_cacheLock)
        {
            if (_cachedSitemap != null && DateTime.UtcNow < _cacheExpiry)
            {
                _logger.LogInformation("Returning cached sitemap (expires in {Minutes} minutes)",
                    (_cacheExpiry - DateTime.UtcNow).TotalMinutes);
                return _cachedSitemap;
            }
        }

        // Cache expired or doesn't exist, regenerate
        var sitemap = await GenerateSitemapAsync();

        lock (_cacheLock)
        {
            _cachedSitemap = sitemap;
            _cacheExpiry = DateTime.UtcNow.AddMinutes(CacheMinutes);
        }

        return sitemap;
    }

    /// <summary>
    /// Generate sitemap XML with all URLs and hreflang for international SEO
    /// </summary>
    public async Task<string> GenerateSitemapAsync()
    {
        _logger.LogInformation("Generating sitemap.xml with hreflang support");

        var baseUrl = _configuration["SiteUrl"] ?? "https://www.viralhashtag.vn"; // Get from config

        // Use MemoryStream instead of StringBuilder to ensure UTF-8 encoding
        using var ms = new MemoryStream();
        using (var writer = XmlWriter.Create(ms, new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false), // UTF-8 without BOM
            OmitXmlDeclaration = false,
            Async = false
        }))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");
            // Add xhtml namespace for hreflang support
            writer.WriteAttributeString("xmlns", "xhtml", null, "http://www.w3.org/1999/xhtml");

            // 1. Homepage (highest priority) - with hreflang
            WriteUrlWithHreflang(writer, baseUrl, baseUrl, DateTime.UtcNow, "1.0", "hourly");

            // 2. Blog index page - with hreflang
            WriteUrlWithHreflang(writer, $"{baseUrl}/blog", $"{baseUrl}/blog", DateTime.UtcNow, "0.8", "daily");

            // 3. Blog posts (important content) - with hreflang for both languages
            var blogPosts = await _blogRepository.GetPublishedPostsAsync(1, 1000); // Get all published posts
            foreach (var post in blogPosts)
            {
                var lastMod = post.UpdatedAt ?? post.PublishedAt ?? post.CreatedAt;
                var viUrl = $"{baseUrl}/blog/{post.Slug}";
                var enUrl = !string.IsNullOrEmpty(post.SlugEn) ? $"{baseUrl}/blog/{post.SlugEn}" : viUrl;

                // Write Vietnamese URL with hreflang
                WriteUrlWithHreflang(writer, viUrl, enUrl, lastMod, "0.7", "monthly");

                // If English slug exists and is different, write English URL too
                if (!string.IsNullOrEmpty(post.SlugEn) && post.SlugEn != post.Slug)
                {
                    WriteUrlWithHreflang(writer, enUrl, viUrl, lastMod, "0.7", "monthly", isEnglishPrimary: true);
                }
            }

            // 4. Blog categories
            var categories = await _blogRepository.GetActiveCategoriesAsync();
            foreach (var category in categories)
            {
                WriteUrl(writer, $"{baseUrl}/blog/category/{category.Slug}", DateTime.UtcNow, "0.6", "weekly");
            }

            // 5. Blog tags
            var tags = await _blogRepository.GetPopularTagsAsync(100);
            foreach (var tag in tags)
            {
                WriteUrl(writer, $"{baseUrl}/blog/tag/{tag.Slug}", DateTime.UtcNow, "0.5", "monthly");
            }

            // 6. Individual hashtag pages
            var allHashtags = await _hashtagRepository.GetAllHashtagsAsync();
            foreach (var hashtag in allHashtags.Where(h => h.IsActive))
            {
                // Remove # and trim whitespace, then URL encode
                var cleanTag = hashtag.Tag.TrimStart('#').Trim();
                var encodedTag = Uri.EscapeDataString(cleanTag);
                var hashtagUrl = $"{baseUrl}/hashtag/{encodedTag}";
                WriteUrl(writer, hashtagUrl, hashtag.LastSeen, "0.6", "daily");
            }

            // 7. Hashtag categories (chu-de pages)
            // var hashtagCategories = await _hashtagRepository.GetActiveCategoriesAsync();
            // foreach (var category in hashtagCategories)
            // {
            //     WriteUrl(writer, $"{baseUrl}/chu-de/{category.Slug}", DateTime.UtcNow, "0.8", "daily");
            // }

            // 8. Static/Tool pages (important for SEO)
            WriteUrl(writer, $"{baseUrl}/hashtag/tao-hashtag-ai", DateTime.UtcNow, "0.9", "daily");
            WriteUrl(writer, $"{baseUrl}/phan-tich/theo-doi-tang-truong", DateTime.UtcNow, "0.8", "daily");
            WriteUrl(writer, $"{baseUrl}/chinh-sach-bao-mat", DateTime.UtcNow, "0.3", "yearly");
            WriteUrl(writer, $"{baseUrl}/dieu-khoan-su-dung", DateTime.UtcNow, "0.3", "yearly");
            WriteUrl(writer, $"{baseUrl}/hashtag/search", DateTime.UtcNow, "0.7", "daily");

            writer.WriteEndElement(); // urlset
            writer.WriteEndDocument();
        }

        // Convert MemoryStream to string with UTF-8 encoding
        ms.Position = 0;
        using var reader = new StreamReader(ms, Encoding.UTF8);
        var xml = reader.ReadToEnd();

        _logger.LogInformation("Sitemap generated with {Count} URLs", CountUrls(xml));

        return xml;
    }

    /// <summary>
    /// Invalidate cache to force regeneration
    /// </summary>
    public void InvalidateCache()
    {
        lock (_cacheLock)
        {
            _cachedSitemap = null;
            _cacheExpiry = DateTime.MinValue;
            _logger.LogInformation("Sitemap cache invalidated");
        }
    }

    #region Private Methods

    /// <summary>
    /// Write URL entry with hreflang annotations for international SEO
    /// </summary>
    private void WriteUrlWithHreflang(XmlWriter writer, string viUrl, string enUrl, DateTime lastMod, string priority, string changeFreq, bool isEnglishPrimary = false)
    {
        var primaryUrl = isEnglishPrimary ? enUrl : viUrl;

        writer.WriteStartElement("url");

        // loc: Required - URL of the page
        writer.WriteElementString("loc", primaryUrl);

        // xhtml:link for hreflang - Vietnamese
        writer.WriteStartElement("xhtml", "link", "http://www.w3.org/1999/xhtml");
        writer.WriteAttributeString("rel", "alternate");
        writer.WriteAttributeString("hreflang", "vi");
        writer.WriteAttributeString("href", viUrl);
        writer.WriteEndElement();

        // xhtml:link for hreflang - English
        writer.WriteStartElement("xhtml", "link", "http://www.w3.org/1999/xhtml");
        writer.WriteAttributeString("rel", "alternate");
        writer.WriteAttributeString("hreflang", "en");
        writer.WriteAttributeString("href", enUrl);
        writer.WriteEndElement();

        // xhtml:link for hreflang - x-default (fallback)
        writer.WriteStartElement("xhtml", "link", "http://www.w3.org/1999/xhtml");
        writer.WriteAttributeString("rel", "alternate");
        writer.WriteAttributeString("hreflang", "x-default");
        writer.WriteAttributeString("href", viUrl); // Vietnamese as default
        writer.WriteEndElement();

        // lastmod
        writer.WriteElementString("lastmod", lastMod.ToString("yyyy-MM-dd"));

        // changefreq
        writer.WriteElementString("changefreq", changeFreq);

        // priority
        writer.WriteElementString("priority", priority);

        writer.WriteEndElement();
    }

    private void WriteUrl(XmlWriter writer, string loc, DateTime lastMod, string priority, string changeFreq)
    {
        writer.WriteStartElement("url");

        // loc: Required - URL of the page (must be absolute URL)
        writer.WriteElementString("loc", loc);

        // lastmod: Optional - Date of last modification (W3C Datetime format)
        // Format: YYYY-MM-DD or YYYY-MM-DDThh:mm:ss+00:00
        writer.WriteElementString("lastmod", lastMod.ToString("yyyy-MM-dd"));

        // changefreq: Optional - How frequently the page is likely to change
        // Valid values: always, hourly, daily, weekly, monthly, yearly, never
        writer.WriteElementString("changefreq", changeFreq);

        // priority: Optional - Priority of this URL relative to other URLs on your site
        // Valid values: 0.0 to 1.0 (default 0.5)
        writer.WriteElementString("priority", priority);

        writer.WriteEndElement();
    }

    private int CountUrls(string xml)
    {
        return xml.Split("<url>").Length - 1;
    }

    #endregion
}
