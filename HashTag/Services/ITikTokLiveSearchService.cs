namespace HashTag.Services;

/// <summary>
/// Service for real-time TikTok hashtag lookup when hashtag is not in database
/// </summary>
public interface ITikTokLiveSearchService
{
    /// <summary>
    /// Search TikTok Creative Center for hashtag information in real-time
    /// </summary>
    /// <param name="hashtag">Hashtag to search (without # prefix)</param>
    /// <returns>Live hashtag data or null if not found</returns>
    Task<LiveHashtagResult?> SearchTikTokCreativeCenterAsync(string hashtag);
}

/// <summary>
/// Result from live TikTok search
/// </summary>
public class LiveHashtagResult
{
    public required string Tag { get; set; }
    public required string TagDisplay { get; set; }
    public long? PostCount { get; set; }
    public long? ViewCount { get; set; }
    public bool IsAvailable { get; set; }
    public string Source { get; set; } = "TikTok Creative Center (Live)";
    public DateTime SearchedAt { get; set; } = DateTime.UtcNow;
}
