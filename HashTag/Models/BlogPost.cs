using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HashTag.Models;

/// <summary>
/// Blog post for content marketing and SEO
/// </summary>
public class BlogPost
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Title { get; set; }

    [Required]
    [MaxLength(250)]
    public required string Slug { get; set; }

    [MaxLength(500)]
    public string? Excerpt { get; set; }

    [Required]
    public required string Content { get; set; }

    [MaxLength(500)]
    public string? FeaturedImage { get; set; }

    // SEO Fields
    [MaxLength(200)]
    public string? MetaTitle { get; set; }

    [MaxLength(500)]
    public string? MetaDescription { get; set; }

    [MaxLength(500)]
    public string? MetaKeywords { get; set; }

    // Author & Category
    [MaxLength(100)]
    public string Author { get; set; } = "TrendTag Team";

    public int? CategoryId { get; set; }

    // Status & Publishing
    [MaxLength(20)]
    public string Status { get; set; } = "Draft"; // Draft, Published, Archived

    public DateTime? PublishedAt { get; set; }

    public int ViewCount { get; set; } = 0;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    [ForeignKey(nameof(CategoryId))]
    public BlogCategory? Category { get; set; }

    public ICollection<BlogPostTag> BlogPostTags { get; set; } = new List<BlogPostTag>();

    // Computed Properties
    [NotMapped]
    public IEnumerable<BlogTag> Tags => BlogPostTags.Select(pt => pt.BlogTag);

    [NotMapped]
    public bool IsPublished => Status == "Published" && PublishedAt.HasValue && PublishedAt.Value <= DateTime.UtcNow;

    [NotMapped]
    public int ReadingTimeMinutes
    {
        get
        {
            if (string.IsNullOrEmpty(Content))
                return 0;

            // Average reading speed: 200 words per minute
            var wordCount = Content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
            return Math.Max(1, (int)Math.Ceiling(wordCount / 200.0));
        }
    }
}
