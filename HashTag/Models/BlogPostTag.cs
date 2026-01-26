using System.ComponentModel.DataAnnotations.Schema;

namespace HashTag.Models;

/// <summary>
/// Many-to-many relationship between BlogPost and BlogTag
/// </summary>
public class BlogPostTag
{
    public int BlogPostId { get; set; }

    public int BlogTagId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    [ForeignKey(nameof(BlogPostId))]
    public BlogPost BlogPost { get; set; } = null!;

    [ForeignKey(nameof(BlogTagId))]
    public BlogTag BlogTag { get; set; } = null!;
}
