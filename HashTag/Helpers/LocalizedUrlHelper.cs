using Microsoft.AspNetCore.Localization;

namespace HashTag.Helpers;

/// <summary>
/// Helper class to generate localized URLs based on current culture
/// </summary>
public static class LocalizedUrlHelper
{
    /// <summary>
    /// Route mappings for Vietnamese and English URLs
    /// Key: Route name, Value: (Vietnamese URL, English URL)
    /// </summary>
    private static readonly Dictionary<string, (string Vi, string En)> RouteMap = new()
    {
        // Analytics
        ["growth-tracking"] = ("/phan-tich/theo-doi-tang-truong", "/analytics/growth-tracking"),

        // Hashtag
        ["ai-generator"] = ("/hashtag/tao-hashtag-ai", "/hashtag/ai-generator"),

        // Category
        ["category"] = ("/chu-de", "/category"),

        // Privacy & Terms
        ["privacy"] = ("/chinh-sach-bao-mat", "/privacy-policy"),
        ["terms"] = ("/dieu-khoan-su-dung", "/terms-of-service"),

        // Search
        ["search"] = ("/tim-kiem", "/search"),
    };

    /// <summary>
    /// Get localized URL based on current culture
    /// </summary>
    /// <param name="context">HttpContext to get current culture</param>
    /// <param name="routeName">Route name key</param>
    /// <param name="additionalPath">Optional additional path segment (e.g., category slug)</param>
    /// <returns>Localized URL</returns>
    public static string GetLocalizedUrl(HttpContext context, string routeName, string? additionalPath = null)
    {
        var isEnglish = IsEnglish(context);

        if (!RouteMap.TryGetValue(routeName, out var urls))
        {
            return additionalPath ?? "/";
        }

        var baseUrl = isEnglish ? urls.En : urls.Vi;

        if (!string.IsNullOrEmpty(additionalPath))
        {
            return $"{baseUrl}/{additionalPath}";
        }

        return baseUrl;
    }

    /// <summary>
    /// Check if current culture is English
    /// </summary>
    public static bool IsEnglish(HttpContext context)
    {
        var requestCulture = context.Features.Get<IRequestCultureFeature>();
        var currentCulture = requestCulture?.RequestCulture.UICulture.Name ?? "vi";
        return currentCulture.StartsWith("en", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Get localized URL for Growth Tracking page
    /// </summary>
    public static string GetGrowthTrackingUrl(HttpContext context, string? categorySlug = null)
    {
        return GetLocalizedUrl(context, "growth-tracking", categorySlug);
    }

    /// <summary>
    /// Get localized URL for AI Hashtag Generator page
    /// </summary>
    public static string GetAIGeneratorUrl(HttpContext context)
    {
        return GetLocalizedUrl(context, "ai-generator");
    }

    /// <summary>
    /// Get localized URL for Category page
    /// </summary>
    public static string GetCategoryUrl(HttpContext context, string categorySlug)
    {
        return GetLocalizedUrl(context, "category", categorySlug);
    }

    /// <summary>
    /// Get localized URL for Privacy Policy page
    /// </summary>
    public static string GetPrivacyUrl(HttpContext context)
    {
        return GetLocalizedUrl(context, "privacy");
    }

    /// <summary>
    /// Get localized URL for Terms of Service page
    /// </summary>
    public static string GetTermsUrl(HttpContext context)
    {
        return GetLocalizedUrl(context, "terms");
    }
}
