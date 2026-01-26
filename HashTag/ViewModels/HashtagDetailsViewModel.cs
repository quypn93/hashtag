using HashTag.Models;

namespace HashTag.ViewModels;

public class HashtagDetailsViewModel
{
    public required Hashtag Hashtag { get; set; }
    public List<HashtagHistory> History { get; set; } = new();
    // public List<HashtagRelation> RelatedHashtags { get; set; } = new(); // DISABLED: Using category-based instead
    public List<Hashtag> RelatedHashtags { get; set; } = new(); // Hashtags cùng category
    public HashtagMetrics? Metrics { get; set; }  // Latest metrics with ViewCount and predictions
}
