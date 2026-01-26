namespace HashTag.Models;

/// <summary>
/// Content categories for hashtag classification
/// </summary>
public class HashtagCategory
{
    public int Id { get; set; }

    /// <summary>
    /// Category name (Fashion, Tech, Beauty, Food, Gaming, etc.)
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Parent category ID for hierarchical categories
    /// Example: "LocalFashion" → parent: "Fashion"
    /// </summary>
    public int? ParentCategoryId { get; set; }

    /// <summary>
    /// Display name in Vietnamese
    /// </summary>
    public string? DisplayNameVi { get; set; }

    /// <summary>
    /// URL-friendly slug for SEO (e.g., "vehicle-transportation")
    /// </summary>
    public string? Slug { get; set; }

    /// <summary>
    /// Icon or emoji representation
    /// </summary>
    public string? Icon { get; set; }

    public bool IsActive { get; set; }

    // Navigation
    public HashtagCategory? ParentCategory { get; set; }
    public ICollection<HashtagCategory> SubCategories { get; set; } = new List<HashtagCategory>();
    public ICollection<Hashtag> Hashtags { get; set; } = new List<Hashtag>();
}
