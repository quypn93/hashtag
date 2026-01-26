using HashTag.Data;
using HashTag.Filters;
using HashTag.Repositories;
using HashTag.Services;
using HashTag.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HashTag.Controllers;

[ServiceFilter(typeof(AdminAuthFilter))]
public class AdminController : Controller
{
    private readonly IHashtagRepository _repository;
    private readonly ICrawlerService _crawlerService;
    private readonly IHashtagMetricsService _metricsService;
    private readonly TrendTagDbContext _dbContext;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IHashtagRepository repository,
        ICrawlerService crawlerService,
        IHashtagMetricsService metricsService,
        TrendTagDbContext dbContext,
        ILogger<AdminController> logger)
    {
        _repository = repository;
        _crawlerService = crawlerService;
        _metricsService = metricsService;
        _dbContext = dbContext;
        _logger = logger;
    }

    // Admin Dashboard
    public async Task<IActionResult> Index()
    {
        var sources = await _repository.GetActiveSourcesAsync();
        var recentLogs = await _repository.GetRecentCrawlLogsAsync(20);

        var viewModel = new AdminDashboardViewModel
        {
            Sources = sources,
            RecentCrawlLogs = recentLogs
        };

        return View(viewModel);
    }

    // Manual Crawl Page
    public async Task<IActionResult> Crawl()
    {
        var sources = await _repository.GetActiveSourcesAsync();

        var viewModel = new ManualCrawlViewModel
        {
            AvailableSources = sources.Select(s => new SourceOption
            {
                Id = s.Id,
                Name = s.Name,
                IsSelected = false
            }).ToList()
        };

        return View(viewModel);
    }

    // Trigger Manual Crawl - All Sources
    [HttpPost]
    public async Task<IActionResult> CrawlAll()
    {
        try
        {
            _logger.LogInformation("Manual crawl of all sources triggered");

            var summary = await _crawlerService.CrawlAllSourcesAsync();

            // After crawl, calculate metrics
            _logger.LogInformation("Crawl completed, now calculating metrics...");
            var metricsResult = await _metricsService.CalculateAllMetricsAsync();
            _logger.LogInformation("Metrics calculated: {Success}/{Total}", metricsResult.SuccessfulCalculations, metricsResult.TotalHashtags);

            // Sync latest counts from HashtagHistory to Hashtags
            _logger.LogInformation("Syncing latest counts...");
            var syncCount = await SyncLatestCountsInternalAsync();
            _logger.LogInformation("Synced {Count} hashtags", syncCount);

            TempData["SuccessMessage"] = $"Crawl completed! Success: {summary.SuccessfulSources}, Failed: {summary.FailedSources}, Total hashtags: {summary.TotalHashtagsCollected}. Metrics: {metricsResult.SuccessfulCalculations} calculated. Synced: {syncCount} hashtags.";
            TempData["CrawlSummary"] = Newtonsoft.Json.JsonConvert.SerializeObject(summary);

            return RedirectToAction(nameof(CrawlResults));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual crawl: {Message}", ex.Message);
            TempData["ErrorMessage"] = $"Crawl failed: {ex.Message}";
            return RedirectToAction(nameof(Crawl));
        }
    }

    // Trigger Manual Crawl - Specific Source
    [HttpPost]
    public async Task<IActionResult> CrawlSource(string sourceName)
    {
        try
        {
            _logger.LogInformation("Manual crawl of source '{SourceName}' triggered", sourceName);

            var result = await _crawlerService.CrawlSourceAsync(sourceName);

            if (result.Success)
            {
                // After crawl, calculate metrics
                _logger.LogInformation("Crawl completed, now calculating metrics...");
                var metricsResult = await _metricsService.CalculateAllMetricsAsync();

                // Sync latest counts
                var syncCount = await SyncLatestCountsInternalAsync();

                TempData["SuccessMessage"] = $"Successfully crawled {sourceName}: {result.HashtagsCollected} hashtags. Metrics: {metricsResult.SuccessfulCalculations} calculated. Synced: {syncCount} hashtags.";
            }
            else
            {
                TempData["ErrorMessage"] = $"Failed to crawl {sourceName}: {result.ErrorMessage}";
            }

            return RedirectToAction(nameof(Crawl));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error crawling source '{SourceName}': {Message}", sourceName, ex.Message);
            TempData["ErrorMessage"] = $"Error: {ex.Message}";
            return RedirectToAction(nameof(Crawl));
        }
    }

    // Crawl Results Page
    public IActionResult CrawlResults()
    {
        var summaryJson = TempData["CrawlSummary"] as string;
        if (summaryJson != null)
        {
            var summary = Newtonsoft.Json.JsonConvert.DeserializeObject<CrawlSummary>(summaryJson);
            return View(summary);
        }

        return RedirectToAction(nameof(Index));
    }

    // View All Crawl Logs
    public async Task<IActionResult> CrawlLogs(int count = 50)
    {
        var logs = await _repository.GetRecentCrawlLogsAsync(count);
        return View(logs);
    }

    // View System Logs (Background Service Lifecycle)
    public async Task<IActionResult> SystemLogs(int count = 100)
    {
        var logs = await _dbContext.SystemLogs
            .OrderByDescending(l => l.Timestamp)
            .Take(count)
            .ToListAsync();

        return View(logs);
    }

    // Clear System Logs
    [HttpPost]
    public async Task<IActionResult> ClearSystemLogs()
    {
        try
        {
            var logs = await _dbContext.SystemLogs.ToListAsync();
            _dbContext.SystemLogs.RemoveRange(logs);
            await _dbContext.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Cleared {logs.Count} system log entries.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing system logs: {Message}", ex.Message);
            TempData["ErrorMessage"] = $"Error: {ex.Message}";
        }

        return RedirectToAction(nameof(SystemLogs));
    }

    // Sync Latest PostCount/ViewCount from HashtagHistory to Hashtags
    [HttpPost]
    public async Task<IActionResult> SyncLatestCounts()
    {
        try
        {
            _logger.LogInformation("Syncing LatestPostCount and LatestViewCount from HashtagHistory to Hashtags");

            var updated = await SyncLatestCountsInternalAsync();

            TempData["SuccessMessage"] = $"Synced LatestPostCount/ViewCount for {updated} hashtags!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing latest counts: {Message}", ex.Message);
            TempData["ErrorMessage"] = $"Sync failed: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // Internal method for syncing latest counts (used by CrawlAll, CrawlSource, and SyncLatestCounts)
    private async Task<int> SyncLatestCountsInternalAsync()
    {
        var hashtags = await _repository.GetAllHashtagsAsync();
        int updated = 0;

        foreach (var hashtag in hashtags)
        {
            var latestHistory = await _repository.GetLatestHistoryWithMetadataAsync(hashtag.Id);
            if (latestHistory != null)
            {
                bool hasChanges = false;

                if (hashtag.LatestPostCount != latestHistory.PostCount)
                {
                    hashtag.LatestPostCount = latestHistory.PostCount;
                    hasChanges = true;
                }

                if (hashtag.LatestViewCount != latestHistory.ViewCount)
                {
                    hashtag.LatestViewCount = latestHistory.ViewCount;
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    await _repository.UpdateHashtagAsync(hashtag);
                    updated++;
                }
            }
        }

        return updated;
    }

    // Calculate Metrics Manually
    [HttpPost]
    public async Task<IActionResult> CalculateMetrics()
    {
        try
        {
            _logger.LogInformation("Manual metrics calculation triggered");

            var result = await _metricsService.CalculateAllMetricsAsync();

            if (result.SuccessfulCalculations > 0)
            {
                TempData["SuccessMessage"] = $"Metrics calculated! Success: {result.SuccessfulCalculations}/{result.TotalHashtags}, Failed: {result.FailedCalculations}, Duration: {result.Duration.TotalSeconds:F2}s";
            }
            else
            {
                TempData["ErrorMessage"] = "No metrics were calculated. Check if there are hashtags with recent data.";
            }

            if (result.Errors.Count > 0)
            {
                var errorSummary = string.Join("; ", result.Errors.Take(3));
                TempData["ErrorMessage"] = $"Some errors occurred: {errorSummary}";
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual metrics calculation: {Message}", ex.Message);
            TempData["ErrorMessage"] = $"Metrics calculation failed: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // Trending Hashtags Dashboard (Admin only) - WITH PAGINATION
    public async Task<IActionResult> Hashtags(HashtagFilterViewModel? filters, int page = 1)
    {
        try
        {
            const int pageSize = 50; // Show 50 hashtags per page

            var filterDto = filters != null ? new HashtagFilterDto
            {
                SourceIds = filters.SelectedSourceIds,
                StartDate = filters.StartDate,
                EndDate = filters.EndDate,
                MinRank = filters.MinRank,
                MaxRank = filters.MaxRank,
                SortBy = filters.SortBy,
                CategoryId = filters.CategoryId,
                DifficultyLevel = filters.DifficultyLevel
            } : null;

            var trendingHashtags = await _repository.GetTrendingHashtagsAsync(filterDto);

            // Apply pagination
            var totalCount = trendingHashtags.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var paginatedHashtags = trendingHashtags
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var sources = await _repository.GetActiveSourcesAsync();
            var sourceOptions = sources.Select(s => new SourceOption
            {
                Id = s.Id,
                Name = s.Name,
                IsSelected = filters?.SelectedSourceIds?.Contains(s.Id) ?? false
            }).ToList();

            var categories = await _repository.GetActiveCategoriesAsync();
            var categoryOptions = categories.Select(c => new CategoryOption
            {
                Id = c.Id,
                Name = c.Name,
                DisplayNameVi = c.DisplayNameVi
            }).ToList();

            var stats = new DashboardStatsViewModel
            {
                TotalHashtags = totalCount,
                TotalSources = sources.Count,
                LastCrawled = sources.Max(s => s.LastCrawled),
                TodayCollected = trendingHashtags.Count(h => h.LastSeen.Date == DateTime.UtcNow.Date)
            };

            var viewModel = new HashtagDashboardViewModel
            {
                TrendingHashtags = paginatedHashtags,
                Filters = filters ?? new HashtagFilterViewModel(),
                Stats = stats,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount
            };

            viewModel.Filters.AvailableSources = sourceOptions;
            viewModel.Filters.AvailableCategories = categoryOptions;

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading hashtag dashboard: {Message}", ex.Message);
            return View(new HashtagDashboardViewModel());
        }
    }

    // Admin Search (fixes 404 error)
    public async Task<IActionResult> Search(string q, int page = 1)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return RedirectToAction(nameof(Hashtags));
            }

            // Reuse the repository's search functionality
            var result = await _repository.SearchHashtagsAsync(q, page, pageSize: 50);

            var viewModel = new HashtagSearchViewModel
            {
                Query = q,
                Results = result,
                CurrentPage = page,
                IsAdminView = true // Flag to show admin-specific UI
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching hashtags in admin: {Message}", ex.Message);
            return View(new HashtagSearchViewModel { Query = q, IsAdminView = true });
        }
    }

    // Clear All Data (Admin only)
    [HttpPost]
    public async Task<IActionResult> ClearAllData()
    {
        try
        {
            _logger.LogWarning("CLEAR ALL DATA triggered - This will delete all hashtags, history, metrics, and categories!");

            await _repository.ClearAllDataAsync();

            _logger.LogInformation("All data cleared successfully");
            TempData["SuccessMessage"] = "All data has been cleared successfully! Database is ready for fresh crawl.";

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all data: {Message}", ex.Message);
            TempData["ErrorMessage"] = $"Failed to clear data: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // Generate Blog Posts Manually
    [HttpPost]
    public async Task<IActionResult> GenerateBlogPosts(string type = "all")
    {
        try
        {
            _logger.LogInformation("Manual blog generation triggered: {Type}", type);

            var blogGenerator = HttpContext.RequestServices.GetRequiredService<IBlogAutoGeneratorService>();
            var generatedPosts = new List<string>();

            switch (type.ToLower())
            {
                case "monthly":
                    var lastMonth = DateTime.Now.AddMonths(-1);
                    var monthlyPost = await blogGenerator.GenerateMonthlyTopHashtagsAsync(lastMonth.Month, lastMonth.Year);
                    if (monthlyPost != null)
                        generatedPosts.Add(monthlyPost.Title);
                    break;

                case "weekly":
                    var weeklyPost = await blogGenerator.GenerateWeeklyTrendingReportAsync();
                    if (weeklyPost != null)
                        generatedPosts.Add(weeklyPost.Title);
                    break;

                case "category":
                    var categories = await _dbContext.HashtagCategories
                        .Where(c => c.IsActive && c.Hashtags.Any(h => h.IsActive && h.LatestViewCount > 0))
                        .ToListAsync();

                    foreach (var cat in categories)
                    {
                        var catPost = await blogGenerator.GenerateCategoryTopHashtagsAsync(cat.Id);
                        if (catPost != null)
                            generatedPosts.Add(catPost.Title);
                    }
                    break;

                case "all":
                default:
                    // Generate monthly
                    var lm = DateTime.Now.AddMonths(-1);
                    var mp = await blogGenerator.GenerateMonthlyTopHashtagsAsync(lm.Month, lm.Year);
                    if (mp != null) generatedPosts.Add(mp.Title);

                    // Generate weekly
                    var wp = await blogGenerator.GenerateWeeklyTrendingReportAsync();
                    if (wp != null) generatedPosts.Add(wp.Title);

                    // Generate category posts
                    var cats = await _dbContext.HashtagCategories
                        .Where(c => c.IsActive && c.Hashtags.Any(h => h.IsActive && h.LatestViewCount > 0))
                        .ToListAsync();

                    foreach (var cat in cats)
                    {
                        var cp = await blogGenerator.GenerateCategoryTopHashtagsAsync(cat.Id);
                        if (cp != null) generatedPosts.Add(cp.Title);
                    }
                    break;
            }

            if (generatedPosts.Any())
            {
                TempData["SuccessMessage"] = $"Generated {generatedPosts.Count} blog posts: {string.Join(", ", generatedPosts.Take(3))}{(generatedPosts.Count > 3 ? "..." : "")}";
            }
            else
            {
                TempData["SuccessMessage"] = "No new posts generated (posts may already exist).";
            }

            return RedirectToAction("Index", "AdminBlog");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating blog posts: {Message}", ex.Message);
            TempData["ErrorMessage"] = $"Error: {ex.Message}";
            return RedirectToAction("Index", "AdminBlog");
        }
    }
}
