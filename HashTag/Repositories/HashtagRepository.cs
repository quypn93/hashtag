using HashTag.Data;
using HashTag.Models;
using HashTag.Services;
using Microsoft.EntityFrameworkCore;

namespace HashTag.Repositories;

public class HashtagRepository : IHashtagRepository
{
    private readonly TrendTagDbContext _context;
    private readonly IStoredProcedureService _spService;
    private readonly ILogger<HashtagRepository> _logger;

    public HashtagRepository(
        TrendTagDbContext context,
        IStoredProcedureService spService,
        ILogger<HashtagRepository> logger)
    {
        _context = context;
        _spService = spService;
        _logger = logger;
    }

    #region Trending & Search

    public async Task<List<TrendingHashtagDto>> GetTrendingHashtagsAsync(HashtagFilterDto? filters = null)
    {
        try
        {
            // ⚡ PERFORMANCE BOOST: Use ADO.NET stored procedure instead of EF Core
            // This provides 5-10x performance improvement for complex queries
            _logger.LogDebug("Calling stored procedure sp_GetTrendingHashtags via ADO.NET");
            return await _spService.GetTrendingHashtagsAsync(filters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling stored procedure, falling back to EF Core: {Message}", ex.Message);

            // FALLBACK: Use original EF Core implementation if SP fails
            return await GetTrendingHashtagsEFCoreAsync(filters);
        }
    }

    /// <summary>
    /// FALLBACK: Original EF Core implementation (kept for compatibility)
    /// Use this if stored procedure fails or during development
    /// </summary>
    private async Task<List<TrendingHashtagDto>> GetTrendingHashtagsEFCoreAsync(HashtagFilterDto? filters = null)
    {
        var query = _context.HashtagHistories
            .Include(h => h.Hashtag)
                .ThenInclude(ht => ht.Category)
            .Include(h => h.Source)
            .AsQueryable();

        // Apply filters
        if (filters != null)
        {
            if (filters.SourceIds != null && filters.SourceIds.Any())
            {
                query = query.Where(h => filters.SourceIds.Contains(h.SourceId));
            }

            if (filters.StartDate.HasValue)
            {
                query = query.Where(h => h.CollectedDate >= filters.StartDate.Value);
            }

            if (filters.EndDate.HasValue)
            {
                query = query.Where(h => h.CollectedDate <= filters.EndDate.Value);
            }

            if (filters.MinRank.HasValue)
            {
                query = query.Where(h => h.Rank >= filters.MinRank.Value);
            }

            if (filters.MaxRank.HasValue)
            {
                query = query.Where(h => h.Rank <= filters.MaxRank.Value);
            }

            if (filters.CategoryId.HasValue)
            {
                query = query.Where(h => h.Hashtag.CategoryId == filters.CategoryId.Value);
            }

            if (!string.IsNullOrEmpty(filters.DifficultyLevel))
            {
                query = query.Where(h => h.Hashtag.DifficultyLevel == filters.DifficultyLevel);
            }
        }

        // ⚡ OPTIMIZED: Simplified approach to avoid high query cost
        // Step 1: Get distinct hashtag IDs with limit to avoid scanning too many rows
        var hashtagIds = await query
            .OrderByDescending(h => h.CollectedDate) // Most recent first
            .Select(h => h.HashtagId)
            .Take(100) // Limit to reduce cost
            .Distinct()
            .ToListAsync();

        // Step 2: Load hashtags directly (already has aggregated data)
        var hashtags = await _context.Hashtags
            .Include(h => h.Category)
            .Where(h => hashtagIds.Contains(h.Id))
            .ToListAsync();

        // Step 3: Get ONLY the most recent history for each hashtag (simplified - load all then group in-memory)
        var latestHistories = await _context.HashtagHistories
            .Include(h => h.Source)
            .Where(h => hashtagIds.Contains(h.HashtagId))
            .OrderByDescending(h => h.CollectedDate)
            .ToListAsync();

        // Group in-memory to avoid expensive database GroupBy
        var historyDict = latestHistories
            .GroupBy(h => h.HashtagId)
            .ToDictionary(g => g.Key, g => g.First());

        var grouped = hashtags.Select(h =>
        {
            var latestHistory = historyDict.GetValueOrDefault(h.Id);
            return new TrendingHashtagDto
            {
                Id = h.Id,
                Tag = h.Tag,
                TagDisplay = h.TagDisplay,
                BestRank = latestHistory?.Rank ?? 999,
                TotalAppearances = h.TotalAppearances,
                Sources = latestHistory != null ? [latestHistory.Source.Name] : [],
                LastSeen = h.LastSeen,
                DifficultyLevel = h.DifficultyLevel,
                CategoryName = latestHistory?.Category ?? h.Category?.Name,
                LatestViewCount = latestHistory?.ViewCount,
                LatestPostCount = latestHistory?.PostCount,
                RankDiff = latestHistory?.RankDiff,
                IsViral = latestHistory?.IsViral ?? false,
                TrendDataJson = latestHistory?.TrendDataJson,
                TrendMomentum = latestHistory?.TrendMomentum
            };
        }).ToList();

        // Apply sorting
        var sortBy = filters?.SortBy ?? "BestRank";
        grouped = sortBy switch
        {
            "TotalAppearances" => grouped.OrderByDescending(h => h.TotalAppearances).ToList(),
            "LastSeen" => grouped.OrderByDescending(h => h.LastSeen).ToList(),
            "TrendMomentum" => grouped.OrderByDescending(h => h.TrendMomentum ?? -999).ToList(), // Highest momentum first, nulls last
            _ => grouped.OrderBy(h => h.BestRank).ToList()
        };

        return grouped;
    }

    public async Task<PagedResult<TrendingHashtagDto>> SearchHashtagsAsync(string query, int page = 1, int pageSize = 20)
    {
        // Normalize search query (remove # if present and trim whitespace)
        var normalizedQuery = query.TrimStart('#').Trim().ToLower();

        // ✅ FIX: Get unique hashtag IDs, prioritize by ViewCount/PostCount for duplicates
        var hashtagIdsQuery = _context.Hashtags
            .Where(h => h.Tag.Contains(normalizedQuery) || h.TagDisplay.Contains(normalizedQuery))
            .Where(h => h.IsActive)
            // ✅ PRIORITY: Order by ViewCount DESC, PostCount DESC to get best hashtags first
            .OrderByDescending(h => h.LatestViewCount ?? 0)
            .ThenByDescending(h => h.LatestPostCount ?? 0)
            .ThenByDescending(h => h.TotalAppearances)
            .ThenBy(h => h.FirstSeen) // Older hashtags preferred if metrics are equal
            .Select(h => h.Id);

        var totalCount = await hashtagIdsQuery.CountAsync();

        var hashtagIds = await hashtagIdsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // ✅ FIX: Now load full hashtag entities with includes (no duplicates because we have exact IDs)
        var hashtags = await _context.Hashtags
            .Include(h => h.Category)
            .Include(h => h.History)
                .ThenInclude(hist => hist.Source)
            .Where(h => hashtagIds.Contains(h.Id))
            .AsSplitQuery() // Prevent cartesian explosion
            .ToListAsync();

        // ✅ Preserve the order from hashtagIds (sorted by ViewCount/PostCount)
        hashtags = hashtags.OrderBy(h => hashtagIds.IndexOf(h.Id)).ToList();

        // ⚡ OPTIMIZED: Get latest history once per hashtag instead of 4 times
        var dtos = hashtags.Select(h =>
        {
            var latestHistory = h.History.OrderByDescending(hist => hist.CollectedDate).FirstOrDefault();

            return new TrendingHashtagDto
            {
                Id = h.Id,
                Tag = h.Tag,
                TagDisplay = h.TagDisplay,
                BestRank = h.History.Any() ? h.History.Min(hist => hist.Rank) : 999,
                TotalAppearances = h.TotalAppearances,
                Sources = h.History.Select(hist => hist.Source.Name).Distinct().ToList(),
                LastSeen = h.LastSeen,
                DifficultyLevel = h.DifficultyLevel,
                CategoryName = h.Category?.Name,
                LatestViewCount = h.LatestViewCount,
                LatestPostCount = h.LatestPostCount,
                RankDiff = latestHistory?.RankDiff,
                IsViral = latestHistory?.IsViral ?? false,
                TrendDataJson = latestHistory?.TrendDataJson,
                TrendMomentum = latestHistory?.TrendMomentum
            };
        }).ToList();

        return new PagedResult<TrendingHashtagDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    #endregion

    #region Hashtag CRUD

    public async Task<List<Hashtag>> GetAllHashtagsAsync()
    {
        return await _context.Hashtags
            .Where(h => h.IsActive)
            .ToListAsync();
    }

    public async Task<Hashtag?> GetHashtagByIdAsync(int id)
    {
        return await _context.Hashtags
            .Include(h => h.Category)
            .Include(h => h.History)
            .Include(h => h.Metrics)
            .Include(h => h.Keywords)
            .FirstOrDefaultAsync(h => h.Id == id);
    }

    public async Task<Hashtag?> GetHashtagByTagAsync(string tag)
    {
        var normalizedTag = tag.TrimStart('#').ToLower();
        return await _context.Hashtags
            .FirstOrDefaultAsync(h => h.Tag == normalizedTag);
    }

    public async Task<Hashtag> GetOrCreateHashtagAsync(string tag)
    {
        // ✅ FIX: Remove # and trim LEADING whitespace only
        var normalizedTag = tag.TrimStart('#', ' ').ToLower();
        var existing = await GetHashtagByTagAsync(normalizedTag);

        if (existing != null)
        {
            return existing;
        }

        var newHashtag = new Hashtag
        {
            Tag = normalizedTag,
            TagDisplay = "#" + normalizedTag,
            FirstSeen = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow,
            IsActive = true,
            TotalAppearances = 0
        };

        _context.Hashtags.Add(newHashtag);
        await _context.SaveChangesAsync();

        return newHashtag;
    }

    public async Task UpdateHashtagAsync(Hashtag hashtag)
    {
        _context.Hashtags.Update(hashtag);
        await _context.SaveChangesAsync();
    }

    #endregion

    #region History

    public async Task<List<HashtagHistory>> GetHashtagHistoryAsync(int hashtagId, int days = 30)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);

        return await _context.HashtagHistories
            .Include(h => h.Source)
            .Where(h => h.HashtagId == hashtagId && h.CollectedDate >= startDate)
            .OrderByDescending(h => h.CollectedDate)
            .ToListAsync();
    }

    public async Task<bool> HashtagHistoryExistsAsync(int hashtagId, int sourceId, DateTime date)
    {
        // Check if entry exists for same hashtag, source, and date (ignoring time)
        var dateOnly = date.Date;
        return await _context.HashtagHistories
            .AnyAsync(h => h.HashtagId == hashtagId
                && h.SourceId == sourceId
                && h.CollectedDate.Date == dateOnly);
    }

    public async Task<bool> HashtagHistoryExistsAsync(int hashtagId, int sourceId, DateTime date, string? category)
    {
        // Check if entry exists for same hashtag, source, date, AND category (ignoring time)
        // This allows same hashtag to be saved multiple times with different categories
        var dateOnly = date.Date;
        return await _context.HashtagHistories
            .AnyAsync(h => h.HashtagId == hashtagId
                && h.SourceId == sourceId
                && h.CollectedDate.Date == dateOnly
                && h.Category == category);
    }

    public async Task<HashtagHistory?> GetHashtagHistoryAsync(int hashtagId, int sourceId, DateTime date, string? category)
    {
        var dateOnly = date.Date;
        var nextDay = dateOnly.AddDays(1);

        var query = _context.HashtagHistories
            .Where(h => h.HashtagId == hashtagId &&
                       h.SourceId == sourceId &&
                       h.CollectedDate >= dateOnly &&
                       h.CollectedDate < nextDay);

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(h => h.Category == category);
        }

        return await query.FirstOrDefaultAsync();
    }

    public async Task AddHashtagHistoryAsync(HashtagHistory history)
    {
        _context.HashtagHistories.Add(history);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateHashtagHistoryAsync(HashtagHistory history)
    {
        _context.HashtagHistories.Update(history);
        await _context.SaveChangesAsync();
    }

    public async Task<List<HashtagHistory>> GetHistoryByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.HashtagHistories
            .Include(h => h.Hashtag)
            .Include(h => h.Source)
            .Where(h => h.CollectedDate >= startDate && h.CollectedDate <= endDate)
            .OrderByDescending(h => h.CollectedDate)
            .ToListAsync();
    }

    #endregion

    #region Sources

    public async Task<List<HashtagSource>> GetActiveSourcesAsync()
    {
        return await _context.HashtagSources
            .Where(s => s.IsActive)
            .ToListAsync();
    }

    public async Task<HashtagSource?> GetSourceByNameAsync(string name)
    {
        return await _context.HashtagSources
            .FirstOrDefaultAsync(s => s.Name == name);
    }

    public async Task UpdateSourceAsync(HashtagSource source)
    {
        _context.HashtagSources.Update(source);
        await _context.SaveChangesAsync();
    }

    #endregion

    #region Crawl Logs

    public async Task<CrawlLog> AddCrawlLogAsync(CrawlLog log)
    {
        _context.CrawlLogs.Add(log);
        await _context.SaveChangesAsync();
        return log;
    }

    public async Task UpdateCrawlLogAsync(CrawlLog log)
    {
        _context.CrawlLogs.Update(log);
        await _context.SaveChangesAsync();
    }

    public async Task<List<CrawlLog>> GetRecentCrawlLogsAsync(int count = 50)
    {
        return await _context.CrawlLogs
            .Include(l => l.Source)
            .OrderByDescending(l => l.StartedAt)
            .Take(count)
            .ToListAsync();
    }

    #endregion

    #region Metrics (for future search features)

    public async Task<HashtagMetrics?> GetLatestMetricsAsync(int hashtagId)
    {
        return await _context.HashtagMetrics
            .Where(m => m.HashtagId == hashtagId)
            .OrderByDescending(m => m.Date)
            .FirstOrDefaultAsync();
    }

    public async Task AddOrUpdateMetricsAsync(HashtagMetrics metrics)
    {
        var existing = await _context.HashtagMetrics
            .FirstOrDefaultAsync(m => m.HashtagId == metrics.HashtagId && m.Date.Date == metrics.Date.Date);

        if (existing != null)
        {
            existing.ViewCount = metrics.ViewCount;
            existing.PostCount = metrics.PostCount;
            existing.EngagementRate = metrics.EngagementRate;
            existing.DifficultyScore = metrics.DifficultyScore;
            existing.GrowthRate7d = metrics.GrowthRate7d;
            existing.GrowthRate30d = metrics.GrowthRate30d;
            existing.PredictedViewMin = metrics.PredictedViewMin;
            existing.PredictedViewMax = metrics.PredictedViewMax;
            _context.HashtagMetrics.Update(existing);
        }
        else
        {
            _context.HashtagMetrics.Add(metrics);
        }

        await _context.SaveChangesAsync();
    }

    #endregion

    #region Categories

    public async Task<List<HashtagCategory>> GetActiveCategoriesAsync()
    {
        try
        {
            // ⚡ PERFORMANCE BOOST: Use ADO.NET stored procedure
            _logger.LogDebug("Calling stored procedure sp_GetActiveCategories via ADO.NET");
            var categories = await _spService.GetActiveCategoriesAsync();

            // Note: SP doesn't load SubCategories navigation property
            // Load them separately if needed (rarely used in Home page)
            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling stored procedure, falling back to EF Core: {Message}", ex.Message);

            // FALLBACK: Use original EF Core implementation
            return await _context.HashtagCategories
                .Where(c => c.IsActive)
                .Include(c => c.SubCategories)
                .ToListAsync();
        }
    }

    public async Task<HashtagCategory?> GetCategoryByNameAsync(string name)
    {
        return await _context.HashtagCategories
            .FirstOrDefaultAsync(c => c.Name == name);
    }

    public async Task<HashtagCategory> GetOrCreateCategoryAsync(string name)
    {
        var existing = await GetCategoryByNameAsync(name);

        if (existing != null)
        {
            return existing;
        }

        var newCategory = new HashtagCategory
        {
            Name = name,
            IsActive = true
        };

        _context.HashtagCategories.Add(newCategory);
        await _context.SaveChangesAsync();

        return newCategory;
    }

    #endregion

    #region Relations (for related hashtags feature) - DISABLED: Performance issues

    // public async Task AddOrUpdateRelationAsync(int hashtagId1, int hashtagId2)
    // {
    //     // Ensure consistent ordering (lower ID first)
    //     var (id1, id2) = hashtagId1 < hashtagId2 ? (hashtagId1, hashtagId2) : (hashtagId2, hashtagId1);
    //
    //     var existing = await _context.HashtagRelations
    //         .FirstOrDefaultAsync(r => r.HashtagId1 == id1 && r.HashtagId2 == id2);
    //
    //     if (existing != null)
    //     {
    //         existing.CoOccurrenceCount++;
    //         existing.LastSeenTogether = DateTime.UtcNow;
    //         existing.UpdatedAt = DateTime.UtcNow;
    //         // Recalculate correlation score (simple implementation)
    //         existing.CorrelationScore = Math.Min(existing.CoOccurrenceCount / 10.0m, 1.0m);
    //         _context.HashtagRelations.Update(existing);
    //     }
    //     else
    //     {
    //         var newRelation = new HashtagRelation
    //         {
    //             HashtagId1 = id1,
    //             HashtagId2 = id2,
    //             CoOccurrenceCount = 1,
    //             CorrelationScore = 0.1m,
    //             LastSeenTogether = DateTime.UtcNow,
    //             CreatedAt = DateTime.UtcNow,
    //             UpdatedAt = DateTime.UtcNow
    //         };
    //         _context.HashtagRelations.Add(newRelation);
    //     }
    //
    //     await _context.SaveChangesAsync();
    // }
    //
    // public async Task<List<HashtagRelation>> GetRelatedHashtagsAsync(int hashtagId, int limit = 10)
    // {
    //     return await _context.HashtagRelations
    //         .Include(r => r.Hashtag1)
    //         .Include(r => r.Hashtag2)
    //         .Where(r => r.HashtagId1 == hashtagId || r.HashtagId2 == hashtagId)
    //         .OrderByDescending(r => r.CorrelationScore)
    //         .Take(limit)
    //         .ToListAsync();
    // }

    /// <summary>
    /// Get hashtags by same category (replacement for GetRelatedHashtagsAsync)
    /// </summary>
    public async Task<List<Hashtag>> GetHashtagsByCategoryAsync(int? categoryId, int excludeHashtagId, int limit = 10)
    {
        if (!categoryId.HasValue)
        {
            return new List<Hashtag>();
        }

        return await _context.Hashtags
            .Include(h => h.Category)
            .Where(h => h.CategoryId == categoryId.Value 
                     && h.Id != excludeHashtagId 
                     && h.IsActive)
            .OrderByDescending(h => h.LatestViewCount ?? 0)
            .ThenByDescending(h => h.TotalAppearances)
            .Take(limit)
            .ToListAsync();
    }

    #endregion

    #region Keywords (for intent-based search)

    public async Task AddKeywordAsync(HashtagKeyword keyword)
    {
        var existing = await _context.HashtagKeywords
            .FirstOrDefaultAsync(k => k.HashtagId == keyword.HashtagId && k.Keyword == keyword.Keyword);

        if (existing == null)
        {
            _context.HashtagKeywords.Add(keyword);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Hashtag>> SearchByKeywordAsync(string keyword)
    {
        var normalizedKeyword = keyword.ToLower();

        return await _context.HashtagKeywords
            .Include(k => k.Hashtag)
                .ThenInclude(h => h.Category)
            .Where(k => k.Keyword.Contains(normalizedKeyword) && k.IsActive)
            .OrderByDescending(k => k.RelevanceScore)
            .Select(k => k.Hashtag)
            .Distinct()
            .Take(20)
            .ToListAsync();
    }

    #endregion

    #region Analytics

    public async Task<List<Hashtag>> GetTopHashtagsInPeriodAsync(DateTime startDate, DateTime endDate, int limit = 10)
    {
        var topHashtagIds = await _context.HashtagHistories
            .Where(h => h.CollectedDate >= startDate && h.CollectedDate <= endDate)
            .GroupBy(h => h.HashtagId)
            .OrderByDescending(g => g.Count()) // Most appearances
            .ThenBy(g => g.Min(h => h.Rank)) // Then by best rank
            .Select(g => g.Key)
            .Take(limit)
            .ToListAsync();

        return await _context.Hashtags
            .Where(h => topHashtagIds.Contains(h.Id))
            .ToListAsync();
    }

    public async Task<List<HashtagHistory>> GetHashtagHistoryInPeriodAsync(int hashtagId, DateTime startDate, DateTime endDate)
    {
        return await _context.HashtagHistories
            .Include(h => h.Source)
            .Where(h => h.HashtagId == hashtagId &&
                       h.CollectedDate >= startDate &&
                       h.CollectedDate <= endDate)
            .OrderBy(h => h.CollectedDate)
            .ToListAsync();
    }

    public async Task<List<HashtagSource>> GetAllSourcesAsync()
    {
        return await _context.HashtagSources
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<int> GetUniqueHashtagCountBySourceAsync(int sourceId)
    {
        return await _context.HashtagHistories
            .Where(h => h.SourceId == sourceId)
            .Select(h => h.HashtagId)
            .Distinct()
            .CountAsync();
    }

    public async Task<List<HashtagHistory>> GetAllHistoryInPeriodAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.HashtagHistories
            .Where(h => h.CollectedDate >= startDate && h.CollectedDate <= endDate)
            .ToListAsync();
    }

    public async Task<HashtagHistory?> GetLatestHistoryWithMetadataAsync(int hashtagId)
    {
        return await _context.HashtagHistories
            .Where(h => h.HashtagId == hashtagId)
            .OrderByDescending(h => h.CollectedDate)
            .ThenByDescending(h => h.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Hashtag>> GetHashtagsWithRecentDataAsync(int days)
    {
        var cutoffDate = DateTime.UtcNow.Date.AddDays(-days);

        var hashtagIds = await _context.HashtagHistories
            .Where(h => h.CollectedDate >= cutoffDate && h.PostCount.HasValue)
            .Select(h => h.HashtagId)
            .Distinct()
            .ToListAsync();

        return await _context.Hashtags
            .Where(h => hashtagIds.Contains(h.Id))
            .ToListAsync();
    }

    #endregion

    #region Admin Operations

    public async Task ClearAllDataAsync()
    {
        // Delete in order to respect foreign key constraints
        // 1. HashtagRelations (references Hashtags) - DISABLED: Not used anymore
        // _context.HashtagRelations.RemoveRange(_context.HashtagRelations);

        // 2. HashtagKeywords (references Hashtags)
        _context.HashtagKeywords.RemoveRange(_context.HashtagKeywords);

        // 3. HashtagMetrics (references Hashtags)
        _context.HashtagMetrics.RemoveRange(_context.HashtagMetrics);

        // 4. HashtagHistory (references Hashtags and Sources)
        _context.HashtagHistories.RemoveRange(_context.HashtagHistories);

        // 5. CrawlLogs (references Sources)
        _context.CrawlLogs.RemoveRange(_context.CrawlLogs);

        // 6. Hashtags (references Categories)
        _context.Hashtags.RemoveRange(_context.Hashtags);

        // 7. HashtagCategories (no dependencies)
        _context.HashtagCategories.RemoveRange(_context.HashtagCategories);

        // Note: We keep HashtagSources because they are seed data

        await _context.SaveChangesAsync();
    }

    #endregion

    #region Bulk Operations

    public async Task<List<Hashtag>> GetHashtagsByTagsAsync(List<string> tags)
    {
        if (!tags.Any()) return new List<Hashtag>();

        // Normalize all tags to lowercase for case-insensitive comparison
        var normalizedTags = tags.Select(t => t.ToLower()).ToList();

        return await _context.Hashtags
            .Include(h => h.Category)
            .Where(h => normalizedTags.Contains(h.Tag.ToLower()))
            .ToListAsync();
    }

    public async Task<List<HashtagCategory>> GetCategoriesByNamesAsync(List<string> names)
    {
        if (!names.Any()) return new List<HashtagCategory>();

        return await _context.HashtagCategories
            .Where(c => names.Contains(c.Name))
            .ToListAsync();
    }

    public async Task<List<HashtagHistory>> GetHashtagHistoriesForDateAsync(int sourceId, DateTime date)
    {
        var dateOnly = date.Date;
        var nextDay = dateOnly.AddDays(1);

        return await _context.HashtagHistories
            .Include(h => h.Hashtag)
            .Where(h => h.SourceId == sourceId
                && h.CollectedDate >= dateOnly
                && h.CollectedDate < nextDay)
            .ToListAsync();
    }

    public async Task BulkSaveChangesAsync(
        List<Hashtag> hashtagsToInsert,
        List<Hashtag> hashtagsToUpdate,
        List<HashtagCategory> categoriesToInsert,
        List<HashtagHistory> historiesToInsert,
        List<HashtagHistory> historiesToUpdate)
    {
        // Use a transaction for atomic operations
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // STEP 1: Insert categories first (they have no dependencies)
            if (categoriesToInsert.Any())
            {
                await _context.HashtagCategories.AddRangeAsync(categoriesToInsert);
                await _context.SaveChangesAsync();
            }

            // STEP 2: Insert new hashtags
            if (hashtagsToInsert.Any())
            {
                await _context.Hashtags.AddRangeAsync(hashtagsToInsert);
                await _context.SaveChangesAsync();
            }

            // STEP 3: Update existing hashtags
            if (hashtagsToUpdate.Any())
            {
                _context.Hashtags.UpdateRange(hashtagsToUpdate);
                await _context.SaveChangesAsync();
            }

            // STEP 4: Insert new histories (depends on hashtags)
            if (historiesToInsert.Any())
            {
                await _context.HashtagHistories.AddRangeAsync(historiesToInsert);
                await _context.SaveChangesAsync();
            }

            // STEP 5: Update existing histories
            if (historiesToUpdate.Any())
            {
                _context.HashtagHistories.UpdateRange(historiesToUpdate);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    #endregion
}
