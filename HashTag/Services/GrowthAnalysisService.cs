using HashTag.Data;
using HashTag.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HashTag.Services;

public interface IGrowthAnalysisService
{
    Task<GrowthAnalysisResult> AnalyzeGrowthAsync(int days = 7, int? categoryId = null, string countryCode = "VN");
}

public class GrowthAnalysisService : IGrowthAnalysisService
{
    private readonly TrendTagDbContext _context;
    private readonly ILogger<GrowthAnalysisService> _logger;

    public GrowthAnalysisService(
        TrendTagDbContext context,
        ILogger<GrowthAnalysisService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<GrowthAnalysisResult> AnalyzeGrowthAsync(int days = 7, int? categoryId = null, string countryCode = "VN")
    {
        try
        {
            _logger.LogInformation("Starting growth analysis for {Days} days, category: {CategoryId}, region: {CountryCode}", days, categoryId, countryCode);

            var result = new GrowthAnalysisResult
            {
                AnalyzedAt = DateTime.UtcNow,
                DaysAnalyzed = days
            };

            // Get current date and comparison date
            var currentDate = DateTime.UtcNow.Date;
            var comparisonDate = currentDate.AddDays(-days);

            // Get hashtags with recent data, filtered by region
            var query = _context.Hashtags
                .Include(h => h.Category)
                .Where(h => h.LatestViewCount > 0)
                .Where(h => h.CountryCode == countryCode);

            if (categoryId.HasValue)
            {
                query = query.Where(h => h.CategoryId == categoryId.Value);
            }

            var hashtags = await query.ToListAsync();
            _logger.LogInformation("Found {Count} hashtags to analyze", hashtags.Count);

            // ⚡ OPTIMIZATION: Load ALL history data in ONE query to avoid N+1 problem
            var hashtagIds = hashtags.Select(h => h.Id).ToList();
            var allHistories = await _context.HashtagHistories
                .Where(h => hashtagIds.Contains(h.HashtagId))
                .OrderByDescending(h => h.CollectedDate)
                .ToListAsync();

            // Group histories by hashtag ID for fast lookup
            var historiesByHashtag = allHistories
                .GroupBy(h => h.HashtagId)
                .ToDictionary(g => g.Key, g => g.ToList());

            _logger.LogInformation("Loaded {Count} history records for analysis", allHistories.Count);

            // For each hashtag, get current and previous metrics
            foreach (var hashtag in hashtags)
            {
                try
                {
                    // Get histories for this hashtag from preloaded data
                    if (!historiesByHashtag.TryGetValue(hashtag.Id, out var hashtagHistories))
                        continue;

                    // Get most recent history record (current)
                    var currentHistory = hashtagHistories.FirstOrDefault();

                    if (currentHistory == null)
                        continue;

                    // Get history record from ~7 days ago
                    var previousHistory = hashtagHistories
                        .Where(h => h.CollectedDate <= comparisonDate)
                        .FirstOrDefault();

                    // If no previous data, skip
                    if (previousHistory == null || previousHistory.ViewCount == null)
                        continue;

                    var currentViews = currentHistory.ViewCount ?? 0;
                    var previousViews = previousHistory.ViewCount ?? 0;

                    // Calculate growth
                    var viewsIncrease = currentViews - previousViews;
                    var growthPercentage = previousViews > 0
                        ? (decimal)viewsIncrease / previousViews * 100
                        : 0;

                    // Classify growth status
                    var (growthStatus, growthIcon, recommendationNote) = ClassifyGrowth(growthPercentage, hashtag.DifficultyLevel);

                    var growthInfo = new HashtagGrowthInfo
                    {
                        Id = hashtag.Id,
                        Tag = hashtag.Tag,
                        CategoryName = hashtag.Category?.Name ?? "Unknown",
                        CurrentViews = currentViews,
                        CurrentPosts = currentHistory.PostCount ?? 0,
                        PreviousViews = previousViews,
                        PreviousPosts = previousHistory.PostCount ?? 0,
                        ViewsIncrease = viewsIncrease,
                        GrowthPercentage = growthPercentage,
                        GrowthStatus = growthStatus,
                        GrowthIcon = growthIcon,
                        DifficultyLevel = hashtag.DifficultyLevel ?? "Unknown",
                        RecommendationNote = recommendationNote
                    };

                    result.AllHashtags.Add(growthInfo);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error analyzing growth for hashtag {Tag}", hashtag.Tag);
                }
            }

            // Update counts
            result.TotalAnalyzed = result.AllHashtags.Count;
            result.FastGrowingCount = result.AllHashtags.Count(h => h.GrowthPercentage >= 50);
            result.StableCount = result.AllHashtags.Count(h => h.GrowthPercentage >= 0 && h.GrowthPercentage < 50);
            result.DecliningCount = result.AllHashtags.Count(h => h.GrowthPercentage < 0);

            _logger.LogInformation(
                "Growth analysis completed: {Total} hashtags, {Fast} fast-growing, {Stable} stable, {Declining} declining",
                result.TotalAnalyzed, result.FastGrowingCount, result.StableCount, result.DecliningCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during growth analysis");
            throw;
        }
    }

    private (string status, string icon, string note) ClassifyGrowth(decimal growthPercentage, string? difficulty)
    {
        // Growth thresholds
        if (growthPercentage >= 100)
        {
            var note = difficulty == "Easy" || difficulty == "Medium"
                ? "🔥 Đang bùng nổ! Post NGAY để viral!"
                : "🔥 Đang bùng nổ! Cạnh tranh cao nhưng reach lớn.";
            return ("Bùng nổ", "🚀", note);
        }
        else if (growthPercentage >= 50)
        {
            var note = difficulty == "Easy"
                ? "✨ Đang lên mạnh, cơ hội tốt cho creators mới!"
                : "✨ Đang lên mạnh, cân nhắc post sớm.";
            return ("Đang lên mạnh", "📈", note);
        }
        else if (growthPercentage >= 20)
        {
            return ("Tăng trưởng ổn định", "↗️", "💡 Tăng trưởng ổn định, phù hợp cho chiến lược dài hạn.");
        }
        else if (growthPercentage >= 0)
        {
            return ("Ổn định", "→", "📊 Ổn định, cạnh tranh không đổi.");
        }
        else if (growthPercentage >= -20)
        {
            return ("Giảm nhẹ", "↘️", "⚠️ Đang giảm nhẹ, cân nhắc hashtag khác.");
        }
        else
        {
            return ("Đang giảm", "📉", "❌ Đang giảm mạnh, tránh sử dụng.");
        }
    }
}
