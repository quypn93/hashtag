using System.Data;
using HashTag.Models;
using HashTag.Repositories;
using Microsoft.Data.SqlClient;

namespace HashTag.Services;

/// <summary>
/// ADO.NET-based service for executing stored procedures
/// Much faster than EF Core for complex queries (5-10x performance gain)
/// </summary>
public class StoredProcedureService : IStoredProcedureService
{
    private readonly string _connectionString;
    private readonly ILogger<StoredProcedureService> _logger;

    public StoredProcedureService(IConfiguration configuration, ILogger<StoredProcedureService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
    }

    /// <summary>
    /// Get trending hashtags using optimized stored procedure
    /// </summary>
    public async Task<List<TrendingHashtagDto>> GetTrendingHashtagsAsync(HashtagFilterDto? filters = null)
    {
        var results = new List<TrendingHashtagDto>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetTrendingHashtags", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30 // 30 seconds timeout
            };

            // Add parameters
            command.Parameters.AddWithValue("@CategoryId", (object?)filters?.CategoryId ?? DBNull.Value);
            command.Parameters.AddWithValue("@DifficultyLevel", (object?)filters?.DifficultyLevel ?? DBNull.Value);
            command.Parameters.AddWithValue("@SourceIds",
                filters?.SourceIds != null && filters.SourceIds.Any()
                    ? string.Join(",", filters.SourceIds)
                    : DBNull.Value);
            command.Parameters.AddWithValue("@MinRank", (object?)filters?.MinRank ?? DBNull.Value);
            command.Parameters.AddWithValue("@MaxRank", (object?)filters?.MaxRank ?? DBNull.Value);
            command.Parameters.AddWithValue("@StartDate", (object?)filters?.StartDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@EndDate", (object?)filters?.EndDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@SortBy", (object?)filters?.SortBy ?? "BestRank");
            command.Parameters.AddWithValue("@Limit", 100);

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var dto = new TrendingHashtagDto
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Tag = reader.GetString(reader.GetOrdinal("Tag")),
                    TagDisplay = reader.GetString(reader.GetOrdinal("TagDisplay")),
                    BestRank = reader.GetInt32(reader.GetOrdinal("BestRank")),
                    TotalAppearances = reader.GetInt32(reader.GetOrdinal("TotalAppearances")),
                    Sources = !reader.IsDBNull(reader.GetOrdinal("Sources"))
                        ? new List<string> { reader.GetString(reader.GetOrdinal("Sources")) }
                        : new List<string>(),
                    LastSeen = reader.GetDateTime(reader.GetOrdinal("LastSeen")),
                    DifficultyLevel = !reader.IsDBNull(reader.GetOrdinal("DifficultyLevel"))
                        ? reader.GetString(reader.GetOrdinal("DifficultyLevel"))
                        : null,
                    CategoryName = !reader.IsDBNull(reader.GetOrdinal("CategoryName"))
                        ? reader.GetString(reader.GetOrdinal("CategoryName"))
                        : null,
                    LatestViewCount = !reader.IsDBNull(reader.GetOrdinal("LatestViewCount"))
                        ? reader.GetInt64(reader.GetOrdinal("LatestViewCount"))
                        : null,
                    LatestPostCount = !reader.IsDBNull(reader.GetOrdinal("LatestPostCount"))
                        ? reader.GetInt64(reader.GetOrdinal("LatestPostCount"))
                        : null,
                    RankDiff = !reader.IsDBNull(reader.GetOrdinal("RankDiff"))
                        ? reader.GetInt32(reader.GetOrdinal("RankDiff"))
                        : null,
                    IsViral = reader.GetBoolean(reader.GetOrdinal("IsViral")),
                    TrendDataJson = !reader.IsDBNull(reader.GetOrdinal("TrendDataJson"))
                        ? reader.GetString(reader.GetOrdinal("TrendDataJson"))
                        : null,
                    TrendMomentum = !reader.IsDBNull(reader.GetOrdinal("TrendMomentum"))
                        ? reader.GetDecimal(reader.GetOrdinal("TrendMomentum"))
                        : null
                };

                results.Add(dto);
            }

            _logger.LogDebug("Retrieved {Count} trending hashtags via stored procedure", results.Count);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing sp_GetTrendingHashtags: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get active categories using stored procedure
    /// </summary>
    public async Task<List<HashtagCategory>> GetActiveCategoriesAsync()
    {
        var results = new List<HashtagCategory>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetActiveCategories", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 15
            };

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var category = new HashtagCategory
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    DisplayNameVi = !reader.IsDBNull(reader.GetOrdinal("DisplayNameVi"))
                        ? reader.GetString(reader.GetOrdinal("DisplayNameVi"))
                        : null,
                    Slug = !reader.IsDBNull(reader.GetOrdinal("Slug"))
                        ? reader.GetString(reader.GetOrdinal("Slug"))
                        : null,
                    Icon = !reader.IsDBNull(reader.GetOrdinal("Icon"))
                        ? reader.GetString(reader.GetOrdinal("Icon"))
                        : null,
                    ParentCategoryId = !reader.IsDBNull(reader.GetOrdinal("ParentCategoryId"))
                        ? reader.GetInt32(reader.GetOrdinal("ParentCategoryId"))
                        : null,
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
                };

                results.Add(category);
            }

            _logger.LogDebug("Retrieved {Count} active categories via stored procedure", results.Count);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing sp_GetActiveCategories: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Get recent blog posts using stored procedure
    /// </summary>
    public async Task<List<BlogPost>> GetRecentBlogPostsAsync(int count = 5)
    {
        var results = new List<BlogPost>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_GetRecentBlogPosts", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 15
            };

            command.Parameters.AddWithValue("@Count", count);

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var post = new BlogPost
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Title = reader.GetString(reader.GetOrdinal("Title")),
                    Slug = reader.GetString(reader.GetOrdinal("Slug")),
                    Excerpt = !reader.IsDBNull(reader.GetOrdinal("Excerpt"))
                        ? reader.GetString(reader.GetOrdinal("Excerpt"))
                        : null,
                    Content = reader.GetString(reader.GetOrdinal("Content")),
                    FeaturedImage = !reader.IsDBNull(reader.GetOrdinal("FeaturedImage"))
                        ? reader.GetString(reader.GetOrdinal("FeaturedImage"))
                        : null,
                    PublishedAt = !reader.IsDBNull(reader.GetOrdinal("PublishedAt"))
                        ? reader.GetDateTime(reader.GetOrdinal("PublishedAt"))
                        : null,
                    UpdatedAt = !reader.IsDBNull(reader.GetOrdinal("UpdatedAt"))
                        ? reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                        : null,
                    ViewCount = reader.GetInt32(reader.GetOrdinal("ViewCount")),
                    MetaTitle = !reader.IsDBNull(reader.GetOrdinal("MetaTitle"))
                        ? reader.GetString(reader.GetOrdinal("MetaTitle"))
                        : null,
                    MetaDescription = !reader.IsDBNull(reader.GetOrdinal("MetaDescription"))
                        ? reader.GetString(reader.GetOrdinal("MetaDescription"))
                        : null,
                    MetaKeywords = !reader.IsDBNull(reader.GetOrdinal("MetaKeywords"))
                        ? reader.GetString(reader.GetOrdinal("MetaKeywords"))
                        : null,
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                    // Load category inline
                    Category = new BlogCategory
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                        Name = reader.GetString(reader.GetOrdinal("CategoryName")),
                        Slug = reader.GetString(reader.GetOrdinal("CategorySlug")),
                        DisplayNameVi = !reader.IsDBNull(reader.GetOrdinal("CategoryDisplayNameVi"))
                            ? reader.GetString(reader.GetOrdinal("CategoryDisplayNameVi"))
                            : null
                    }
                };

                results.Add(post);
            }

            _logger.LogDebug("Retrieved {Count} recent blog posts via stored procedure", results.Count);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing sp_GetRecentBlogPosts: {Message}", ex.Message);
            throw;
        }
    }
}
