namespace HashTag.ViewModels;

/// <summary>
/// SEO metadata for pages - used in _LayoutPublic.cshtml
/// </summary>
public class SeoMetadata
{
    /// <summary>
    /// Page title (without site name suffix)
    /// Example: "Phân Tích Hashtag #XuHướng TikTok"
    /// </summary>
    public string Title { get; set; } = "Hashtag Trending TikTok Việt Nam";

    /// <summary>
    /// Meta description (150-160 characters)
    /// </summary>
    public string Description { get; set; } = "Theo dõi và phân tích hashtag TikTok trending tại Việt Nam. Cập nhật real-time, dự đoán lượt xem, phân tích độ khó hashtag.";

    /// <summary>
    /// Meta keywords (comma-separated)
    /// </summary>
    public string Keywords { get; set; } = "hashtag tiktok, trending hashtag, viral hashtag, tiktok vietnam, phân tích hashtag";

    /// <summary>
    /// Canonical URL (absolute URL)
    /// Example: "https://trendtag.vn/hashtag/xuhuong"
    /// </summary>
    public string? CanonicalUrl { get; set; }

    /// <summary>
    /// Open Graph title (can be different from page title)
    /// </summary>
    public string? OgTitle { get; set; }

    /// <summary>
    /// Open Graph description (can be different from meta description)
    /// </summary>
    public string? OgDescription { get; set; }

    /// <summary>
    /// Open Graph image URL (absolute URL)
    /// Recommended: 1200x628px
    /// </summary>
    public string? OgImage { get; set; }

    /// <summary>
    /// Open Graph image width
    /// </summary>
    public int OgImageWidth { get; set; } = 1200;

    /// <summary>
    /// Open Graph image height
    /// </summary>
    public int OgImageHeight { get; set; } = 628;

    /// <summary>
    /// Open Graph type
    /// </summary>
    public string OgType { get; set; } = "website";

    /// <summary>
    /// Twitter card type
    /// </summary>
    public string TwitterCard { get; set; } = "summary_large_image";

    /// <summary>
    /// Structured data (JSON-LD) as JSON string
    /// </summary>
    public string? StructuredDataJson { get; set; }

    /// <summary>
    /// Page type for tracking (hashtag-detail, trending-list, search-results, etc.)
    /// </summary>
    public string PageType { get; set; } = "general";

    /// <summary>
    /// Additional meta tags (key-value pairs)
    /// </summary>
    public Dictionary<string, string> AdditionalMetaTags { get; set; } = new();
}
