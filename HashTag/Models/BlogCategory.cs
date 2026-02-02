using System.ComponentModel.DataAnnotations;

namespace HashTag.Models;

/// <summary>
/// Category for blog posts
/// </summary>
public class BlogCategory
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Category name (Vietnamese - primary)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    /// <summary>
    /// Category name in English
    /// </summary>
    [MaxLength(100)]
    public string? NameEn { get; set; }

    [MaxLength(100)]
    public string? DisplayNameVi { get; set; }

    [Required]
    [MaxLength(150)]
    public required string Slug { get; set; }

    /// <summary>
    /// Description (Vietnamese - primary)
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Description in English
    /// </summary>
    [MaxLength(500)]
    public string? DescriptionEn { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public ICollection<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();

    // Localization helper methods
    public string GetLocalizedName(bool isEnglish) =>
        isEnglish && !string.IsNullOrEmpty(NameEn) ? NameEn : Name;

    public string? GetLocalizedDescription(bool isEnglish) =>
        isEnglish && !string.IsNullOrEmpty(DescriptionEn) ? DescriptionEn : Description;
}
