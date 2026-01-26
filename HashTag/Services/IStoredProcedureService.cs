using HashTag.Models;
using HashTag.Repositories;

namespace HashTag.Services;

/// <summary>
/// Service for executing optimized stored procedures using ADO.NET
/// Provides 5-10x performance improvement over EF Core for complex queries
/// </summary>
public interface IStoredProcedureService
{
    /// <summary>
    /// Get trending hashtags using optimized stored procedure
    /// </summary>
    Task<List<TrendingHashtagDto>> GetTrendingHashtagsAsync(HashtagFilterDto? filters = null);

    /// <summary>
    /// Get active categories using stored procedure
    /// </summary>
    Task<List<HashtagCategory>> GetActiveCategoriesAsync();

    /// <summary>
    /// Get recent blog posts using stored procedure
    /// </summary>
    Task<List<BlogPost>> GetRecentBlogPostsAsync(int count = 5);
}
