using HashTag.Models;

namespace HashTag.Services;

/// <summary>
/// Service for auto-generating blog content from hashtag data
/// </summary>
public interface IBlogAutoGeneratorService
{
    /// <summary>
    /// Generate a blog post with top hashtags for a specific month
    /// </summary>
    Task<BlogPost?> GenerateMonthlyTopHashtagsAsync(int month, int year);

    /// <summary>
    /// Generate a blog post with top hashtags for a specific category
    /// </summary>
    Task<BlogPost?> GenerateCategoryTopHashtagsAsync(int categoryId);

    /// <summary>
    /// Generate a weekly trending report blog post
    /// </summary>
    Task<BlogPost?> GenerateWeeklyTrendingReportAsync();
}
