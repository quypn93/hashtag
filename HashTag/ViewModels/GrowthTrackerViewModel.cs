namespace HashTag.ViewModels;

/// <summary>
/// ViewModel for Hashtag Growth Tracker page
/// </summary>
public class GrowthTrackerViewModel
{
    public List<HashtagGrowthInfo> TrendingGrowth { get; set; } = new();
    public List<HashtagGrowthInfo> FastestGrowing { get; set; } = new();
    public List<HashtagGrowthInfo> Declining { get; set; } = new();
    public int? SelectedCategoryId { get; set; }
    public List<Models.HashtagCategory> Categories { get; set; } = new();
    public int AnalysisDays { get; set; } = 7;
}

/// <summary>
/// Individual hashtag growth information
/// </summary>
public class HashtagGrowthInfo
{
    public int Id { get; set; }
    public string Tag { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;

    // Current metrics
    public long CurrentViews { get; set; }
    public long CurrentPosts { get; set; }

    // Previous metrics (7 days ago)
    public long PreviousViews { get; set; }
    public long PreviousPosts { get; set; }

    // Growth calculations
    public long ViewsIncrease { get; set; }
    public decimal GrowthPercentage { get; set; }
    public string GrowthStatus { get; set; } = string.Empty; // "Bùng nổ", "Đang lên", "Ổn định", "Giảm"
    public string GrowthIcon { get; set; } = string.Empty; // "🚀", "📈", "→", "↘️"

    // Additional context
    public string DifficultyLevel { get; set; } = string.Empty;
    public string RecommendationNote { get; set; } = string.Empty;
}

/// <summary>
/// Growth analysis result
/// </summary>
public class GrowthAnalysisResult
{
    public List<HashtagGrowthInfo> AllHashtags { get; set; } = new();
    public int TotalAnalyzed { get; set; }
    public int FastGrowingCount { get; set; }
    public int StableCount { get; set; }
    public int DecliningCount { get; set; }
    public DateTime AnalyzedAt { get; set; }
    public int DaysAnalyzed { get; set; }
}
