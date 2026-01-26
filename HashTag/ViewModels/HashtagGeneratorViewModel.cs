namespace HashTag.ViewModels;

/// <summary>
/// Request model for hashtag generation
/// </summary>
public class HashtagGeneratorRequest
{
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Response model for hashtag generation
/// </summary>
public class HashtagGeneratorResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public HashtagRecommendation? Recommendation { get; set; }
    public int GenerationId { get; set; }
}

/// <summary>
/// Recommended hashtags grouped by type
/// </summary>
public class HashtagRecommendation
{
    public List<RecommendedHashtag> TrendingHashtags { get; set; } = new();
    public List<RecommendedHashtag> NicheHashtags { get; set; } = new();
    public List<RecommendedHashtag> UltraNicheHashtags { get; set; } = new();
    public string? Reasoning { get; set; }

    /// <summary>
    /// Get all hashtags combined for easy copying
    /// </summary>
    public string GetAllHashtagsText()
    {
        var all = TrendingHashtags
            .Concat(NicheHashtags)
            .Concat(UltraNicheHashtags)
            .Select(h => $"#{h.Tag}")
            .ToList();

        return string.Join(" ", all);
    }

    /// <summary>
    /// Get total count of recommended hashtags
    /// </summary>
    public int TotalCount => TrendingHashtags.Count + NicheHashtags.Count + UltraNicheHashtags.Count;
}

/// <summary>
/// Individual recommended hashtag with metadata
/// </summary>
public class RecommendedHashtag
{
    public string Tag { get; set; } = string.Empty;
    public long ViewCount { get; set; }
    public long PostCount { get; set; }
    public string CompetitionLevel { get; set; } = string.Empty; // "Thấp", "Trung Bình", "Cao"
    public string ExpectedReach { get; set; } = string.Empty; // "2K-10K người"
    public int ViralProbability { get; set; } // 0-100
    public string? RecommendationNote { get; set; } // Special notes like "Đề xuất: Dễ viral cho creators mới"
    public bool IsSelected { get; set; } = true; // Default all selected
    public bool ExistsInDatabase { get; set; } = true; // Whether hashtag exists in our database
}
