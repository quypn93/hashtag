using System.ComponentModel.DataAnnotations;

namespace HashTag.Models;

/// <summary>
/// Category for blog posts
/// </summary>
public class BlogCategory
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    [MaxLength(100)]
    public string? DisplayNameVi { get; set; }

    [Required]
    [MaxLength(150)]
    public required string Slug { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public ICollection<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();
}
