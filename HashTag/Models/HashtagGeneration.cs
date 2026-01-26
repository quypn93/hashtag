using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HashTag.Models;

/// <summary>
/// Represents a hashtag generation request and its AI-generated results
/// </summary>
public class HashtagGeneration
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(1000)]
    public string InputDescription { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? InputDescriptionHash { get; set; }

    [Required]
    public string RecommendedHashtags { get; set; } = string.Empty; // JSON

    [MaxLength(50)]
    public string GenerationMethod { get; set; } = "AI";

    public int? UserId { get; set; }

    public bool WasCopied { get; set; }

    public bool WasSaved { get; set; }

    [MaxLength(20)]
    public string? UserFeedback { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CachedUntil { get; set; }

    public int? TokensUsed { get; set; }

    // Navigation properties
    public virtual ICollection<GenerationHashtagSelection> SelectedHashtags { get; set; } = new List<GenerationHashtagSelection>();
}

/// <summary>
/// Tracks which hashtags were selected by user from a generation
/// </summary>
public class GenerationHashtagSelection
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int GenerationId { get; set; }

    public int? HashtagId { get; set; }

    [Required]
    [MaxLength(100)]
    public string HashtagText { get; set; } = string.Empty;

    public bool WasSelected { get; set; }

    public long? PerformanceViews { get; set; }

    // Navigation properties
    [ForeignKey(nameof(GenerationId))]
    public virtual HashtagGeneration Generation { get; set; } = null!;

    [ForeignKey(nameof(HashtagId))]
    public virtual Hashtag? Hashtag { get; set; }
}

/// <summary>
/// Rate limiting tracking per user/IP
/// </summary>
public class GenerationRateLimit
{
    [Key]
    public int Id { get; set; }

    public int? UserId { get; set; }

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    public int GenerationCount { get; set; }

    public DateTime WindowStartTime { get; set; } = DateTime.UtcNow;
}
