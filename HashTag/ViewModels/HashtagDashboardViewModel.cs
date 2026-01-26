using HashTag.Repositories;

namespace HashTag.ViewModels;

public class HashtagDashboardViewModel
{
    public List<TrendingHashtagDto> TrendingHashtags { get; set; } = new();
    public HashtagFilterViewModel Filters { get; set; } = new();
    public DashboardStatsViewModel Stats { get; set; } = new();

    // Pagination properties
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}

public class HashtagFilterViewModel
{
    public List<int>? SelectedSourceIds { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? MinRank { get; set; }
    public int? MaxRank { get; set; }
    public string? SortBy { get; set; }
    public int? CategoryId { get; set; }
    public string? DifficultyLevel { get; set; }

    // For display
    public List<SourceOption> AvailableSources { get; set; } = new();
    public List<CategoryOption> AvailableCategories { get; set; } = new();
}

public class SourceOption
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public bool IsSelected { get; set; }
}

public class CategoryOption
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? DisplayNameVi { get; set; }
    public string? Slug { get; set; }
}

public class DashboardStatsViewModel
{
    public int TotalHashtags { get; set; }
    public int TotalSources { get; set; }
    public DateTime? LastCrawled { get; set; }
    public int TodayCollected { get; set; }
}
