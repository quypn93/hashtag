namespace HashTag.Services;

public interface IHashtagAnalyticsService
{
    /// <summary>
    /// Get trending hashtags chart data for the last N days
    /// Returns top trending hashtags with their daily ranks
    /// </summary>
    Task<TrendingChartData> GetTrendingChartDataAsync(int days = 7);

    /// <summary>
    /// Get source comparison data - how many hashtags each source provides
    /// </summary>
    Task<SourceComparisonData> GetSourceComparisonDataAsync();

    /// <summary>
    /// Get individual hashtag history for detail view
    /// </summary>
    Task<HashtagHistoryData> GetHashtagHistoryDataAsync(int hashtagId, int days = 30);

    /// <summary>
    /// Get daily activity chart - how many hashtags collected per day
    /// </summary>
    Task<DailyActivityData> GetDailyActivityDataAsync(int days = 30);
}

// Chart Data DTOs
public class TrendingChartData
{
    public List<string> Labels { get; set; } = new(); // Dates
    public List<TrendingHashtagSeries> Series { get; set; } = new(); // Top hashtags
}

public class TrendingHashtagSeries
{
    public string HashtagName { get; set; } = "";
    public List<int?> Ranks { get; set; } = new(); // Rank per day (null if not present)
}

public class SourceComparisonData
{
    public List<string> SourceNames { get; set; } = new();
    public List<int> HashtagCounts { get; set; } = new();
    public List<string> Colors { get; set; } = new();
}

public class HashtagHistoryData
{
    public string HashtagName { get; set; } = "";
    public List<string> Dates { get; set; } = new();
    public List<int> Ranks { get; set; } = new();
    public Dictionary<string, List<int?>> SourceRanks { get; set; } = new(); // Rank per source
}

public class DailyActivityData
{
    public List<string> Dates { get; set; } = new();
    public List<int> TotalHashtags { get; set; } = new();
    public List<int> UniqueHashtags { get; set; } = new();
}
