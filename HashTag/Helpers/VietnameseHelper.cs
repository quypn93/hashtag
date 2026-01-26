using System.Globalization;
using System.Text;

namespace HashTag.Helpers;

/// <summary>
/// Helper class for Vietnamese language SEO optimization
/// </summary>
public static class VietnameseHelper
{
    /// <summary>
    /// Remove Vietnamese diacritics from text for SEO-friendly URLs
    /// Converts: "Thời Trang" -> "thoi trang"
    /// </summary>
    public static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Normalize to FormD (decomposed form)
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            // Skip non-spacing marks (diacritics)
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        // Normalize back to FormC (composed form)
        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Convert text to SEO-friendly URL slug
    /// Example: "Thời Trang Việt Nam" -> "thoi-trang-viet-nam"
    /// </summary>
    public static string ToUrlSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Remove diacritics
        var slug = RemoveDiacritics(text);

        // Convert to lowercase
        slug = slug.ToLowerInvariant();

        // Remove # symbol
        slug = slug.Replace("#", "");

        // Replace spaces with hyphens
        slug = slug.Trim().Replace(" ", "-");

        // Remove consecutive hyphens
        while (slug.Contains("--"))
            slug = slug.Replace("--", "-");

        // Remove special characters (keep only alphanumeric and hyphens)
        slug = new string(slug.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

        return slug;
    }

    /// <summary>
    /// Format number with K/M/B suffixes for Vietnamese display
    /// Example: 94100000000 -> "94.1 Tỷ"
    /// </summary>
    public static string FormatNumber(long? number)
    {
        if (!number.HasValue || number == 0)
            return "0";

        var value = number.Value;

        if (value >= 1_000_000_000)
            return $"{value / 1_000_000_000.0:F1} Tỷ"; // Billion = Tỷ
        if (value >= 1_000_000)
            return $"{value / 1_000_000.0:F1} Triệu"; // Million = Triệu
        if (value >= 1_000)
            return $"{value / 1_000.0:F1}K";

        return value.ToString("N0");
    }

    /// <summary>
    /// Get Vietnamese month name
    /// </summary>
    public static string GetMonthName(int month)
    {
        return month switch
        {
            1 => "Tháng 1",
            2 => "Tháng 2",
            3 => "Tháng 3",
            4 => "Tháng 4",
            5 => "Tháng 5",
            6 => "Tháng 6",
            7 => "Tháng 7",
            8 => "Tháng 8",
            9 => "Tháng 9",
            10 => "Tháng 10",
            11 => "Tháng 11",
            12 => "Tháng 12",
            _ => $"Tháng {month}"
        };
    }
}
