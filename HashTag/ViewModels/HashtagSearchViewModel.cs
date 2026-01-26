using HashTag.Repositories;
using HashTag.Services;

namespace HashTag.ViewModels;

public class HashtagSearchViewModel
{
    public required string Query { get; set; }
    public PagedResult<TrendingHashtagDto> Results { get; set; } = new();
    public int CurrentPage { get; set; } = 1;

    /// <summary>
    /// Live search result from TikTok (when hashtag not in database)
    /// </summary>
    public LiveHashtagResult? LiveResult { get; set; }

    /// <summary>
    /// Flag to indicate if this is admin view (different layout/options)
    /// </summary>
    public bool IsAdminView { get; set; } = false;
}
