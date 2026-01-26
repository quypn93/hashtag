using HashTag.Models;

namespace HashTag.ViewModels;

public class AdminDashboardViewModel
{
    public List<HashtagSource> Sources { get; set; } = new();
    public List<CrawlLog> RecentCrawlLogs { get; set; } = new();
}

public class ManualCrawlViewModel
{
    public List<SourceOption> AvailableSources { get; set; } = new();
}
