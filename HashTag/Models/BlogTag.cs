using System.ComponentModel.DataAnnotations;

namespace HashTag.Models;

/// <summary>
/// Tag for blog posts (many-to-many relationship)
/// </summary>
public class BlogTag
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(70)]
    public required string Slug { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public ICollection<BlogPostTag> BlogPostTags { get; set; } = new List<BlogPostTag>();
}
