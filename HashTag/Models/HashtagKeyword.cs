namespace HashTag.Models;

/// <summary>
/// Search keywords mapping for intent-based search
/// Example: "áo local brand" → maps to #localfashion, #streetwear, #aothun
/// </summary>
public class HashtagKeyword
{
    public int Id { get; set; }

    public int HashtagId { get; set; }

    /// <summary>
    /// Keyword or phrase (lowercase, normalized)
    /// </summary>
    public required string Keyword { get; set; }

    /// <summary>
    /// Keyword in Vietnamese (if different)
    /// </summary>
    public string? KeywordVi { get; set; }

    /// <summary>
    /// Relevance score for this keyword-hashtag mapping (0.0 - 1.0)
    /// </summary>
    public decimal RelevanceScore { get; set; }

    /// <summary>
    /// How this keyword was added (Manual, AutoExtracted, MLGenerated)
    /// </summary>
    public required string Source { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation
    public Hashtag Hashtag { get; set; } = null!;
}
