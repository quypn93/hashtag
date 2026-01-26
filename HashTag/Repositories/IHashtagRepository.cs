using HashTag.Models;

namespace HashTag.Repositories;

public interface IHashtagRepository
{
    // Trending & Search
    Task<List<TrendingHashtagDto>> GetTrendingHashtagsAsync(HashtagFilterDto? filters = null);
    Task<PagedResult<TrendingHashtagDto>> SearchHashtagsAsync(string query, int page = 1, int pageSize = 20);

    // Hashtag CRUD
    Task<List<Hashtag>> GetAllHashtagsAsync();
    Task<Hashtag?> GetHashtagByIdAsync(int id);
    Task<Hashtag?> GetHashtagByTagAsync(string tag);
    Task<Hashtag> GetOrCreateHashtagAsync(string tag);
    Task UpdateHashtagAsync(Hashtag hashtag);

    // History
    Task<List<HashtagHistory>> GetHashtagHistoryAsync(int hashtagId, int days = 30);
    Task<bool> HashtagHistoryExistsAsync(int hashtagId, int sourceId, DateTime date);
    Task<bool> HashtagHistoryExistsAsync(int hashtagId, int sourceId, DateTime date, string? category);
    Task<HashtagHistory?> GetHashtagHistoryAsync(int hashtagId, int sourceId, DateTime date, string? category);
    Task AddHashtagHistoryAsync(HashtagHistory history);
    Task UpdateHashtagHistoryAsync(HashtagHistory history);
    Task<List<HashtagHistory>> GetHistoryByDateRangeAsync(DateTime startDate, DateTime endDate);

    // Sources
    Task<List<HashtagSource>> GetActiveSourcesAsync();
    Task<HashtagSource?> GetSourceByNameAsync(string name);
    Task UpdateSourceAsync(HashtagSource source);

    // Crawl Logs
    Task<CrawlLog> AddCrawlLogAsync(CrawlLog log);
    Task UpdateCrawlLogAsync(CrawlLog log);
    Task<List<CrawlLog>> GetRecentCrawlLogsAsync(int count = 50);

    // Metrics (for future search features)
    Task<HashtagMetrics?> GetLatestMetricsAsync(int hashtagId);
    Task AddOrUpdateMetricsAsync(HashtagMetrics metrics);

    // Categories
    Task<List<HashtagCategory>> GetActiveCategoriesAsync();
    Task<HashtagCategory?> GetCategoryByNameAsync(string name);
    Task<HashtagCategory> GetOrCreateCategoryAsync(string name);

    // Relations (for related hashtags feature) - DISABLED: Performance issues
    // Task AddOrUpdateRelationAsync(int hashtagId1, int hashtagId2);
    // Task<List<HashtagRelation>> GetRelatedHashtagsAsync(int hashtagId, int limit = 10);

    // Get hashtags by same category
    Task<List<Hashtag>> GetHashtagsByCategoryAsync(int? categoryId, int excludeHashtagId, int limit = 10);

    // Keywords (for intent-based search)
    Task AddKeywordAsync(HashtagKeyword keyword);
    Task<List<Hashtag>> SearchByKeywordAsync(string keyword);

    // Analytics methods
    Task<List<Hashtag>> GetTopHashtagsInPeriodAsync(DateTime startDate, DateTime endDate, int limit = 10);
    Task<List<HashtagHistory>> GetHashtagHistoryInPeriodAsync(int hashtagId, DateTime startDate, DateTime endDate);
    Task<List<HashtagSource>> GetAllSourcesAsync();
    Task<int> GetUniqueHashtagCountBySourceAsync(int sourceId);
    Task<List<HashtagHistory>> GetAllHistoryInPeriodAsync(DateTime startDate, DateTime endDate);

    // Metrics calculation methods
    Task<HashtagHistory?> GetLatestHistoryWithMetadataAsync(int hashtagId);
    Task<List<Hashtag>> GetHashtagsWithRecentDataAsync(int days);

    // Admin operations
    Task ClearAllDataAsync();

    // Bulk operations for performance optimization
    Task<List<Hashtag>> GetHashtagsByTagsAsync(List<string> tags);
    Task<List<HashtagCategory>> GetCategoriesByNamesAsync(List<string> names);
    Task<List<HashtagHistory>> GetHashtagHistoriesForDateAsync(int sourceId, DateTime date);
    Task BulkSaveChangesAsync(
        List<Hashtag> hashtagsToInsert,
        List<Hashtag> hashtagsToUpdate,
        List<HashtagCategory> categoriesToInsert,
        List<HashtagHistory> historiesToInsert,
        List<HashtagHistory> historiesToUpdate);
}

// DTOs for data transfer
public class TrendingHashtagDto
{
    public int Id { get; set; }
    public required string Tag { get; set; }
    public required string TagDisplay { get; set; }
    public int BestRank { get; set; }
    public int TotalAppearances { get; set; }
    public List<string> Sources { get; set; } = new();
    public DateTime LastSeen { get; set; }
    public string? DifficultyLevel { get; set; }
    public string? CategoryName { get; set; }
    public long? LatestViewCount { get; set; }
    public long? LatestPostCount { get; set; }

    // NEW: Phase 1 - Rank momentum indicators
    public int? RankDiff { get; set; }  // +15 = moved up 15 spots, -5 = dropped 5 spots
    public bool IsViral { get; set; }   // true = explosive/viral trending

    // NEW: Phase 2 - Trend analysis
    public string? TrendDataJson { get; set; }  // JSON array for sparkline chart
    public decimal? TrendMomentum { get; set; }  // Momentum score (-100 to +100)
}

public class HashtagFilterDto
{
    public List<int>? SourceIds { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? MinRank { get; set; }
    public int? MaxRank { get; set; }
    public string? SortBy { get; set; } // "BestRank", "TotalAppearances", "LastSeen"
    public int? CategoryId { get; set; }
    public string? DifficultyLevel { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
