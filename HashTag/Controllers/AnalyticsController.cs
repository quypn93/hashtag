using HashTag.Data;
using HashTag.Services;
using HashTag.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HashTag.Controllers;

[Route("phan-tich")]
public class AnalyticsController : Controller
{
    private readonly IGrowthAnalysisService _growthService;
    private readonly TrendTagDbContext _context;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IGrowthAnalysisService growthService,
        TrendTagDbContext context,
        ILogger<AnalyticsController> logger)
    {
        _growthService = growthService;
        _context = context;
        _logger = logger;
    }

    [HttpGet("theo-doi-tang-truong")]
    [HttpGet("theo-doi-tang-truong/{catSlug}")]
    public async Task<IActionResult> Growth(string? catSlug = null)
    {
        try
        {
            // Get categories for filter dropdown
            var categories = await _context.HashtagCategories
                .OrderBy(c => c.Name)
                .ToListAsync();

            // Find categoryId from slug if provided
            int? categoryId = null;
            if (!string.IsNullOrEmpty(catSlug))
            {
                var category = categories.FirstOrDefault(c => c.Slug == catSlug);
                categoryId = category?.Id;
            }

            _logger.LogInformation("Loading Growth Tracker page, category: {CategorySlug} ({CategoryId})", catSlug, categoryId);

            // Analyze growth (7 days by default)
            var analysisResult = await _growthService.AnalyzeGrowthAsync(7, categoryId);

            // Prepare view model
            var viewModel = new GrowthTrackerViewModel
            {
                // Top 10 fastest growing (positive growth only)
                FastestGrowing = analysisResult.AllHashtags
                    .Where(h => h.GrowthPercentage > 0)
                    .OrderByDescending(h => h.GrowthPercentage)
                    .Take(10)
                    .ToList(),

                // Top 10 trending (highest absolute increase)
                TrendingGrowth = analysisResult.AllHashtags
                    .OrderByDescending(h => h.ViewsIncrease)
                    .Take(10)
                    .ToList(),

                // Declining hashtags
                Declining = analysisResult.AllHashtags
                    .Where(h => h.GrowthPercentage < -20) // Only show significant declines
                    .OrderBy(h => h.GrowthPercentage)
                    .Take(10)
                    .ToList(),

                SelectedCategoryId = categoryId,
                Categories = categories,
                AnalysisDays = 7
            };

            _logger.LogInformation(
                "Growth analysis complete: {Fast} fast-growing, {Trending} trending, {Declining} declining",
                viewModel.FastestGrowing.Count,
                viewModel.TrendingGrowth.Count,
                viewModel.Declining.Count);

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Growth Tracker page");
            TempData["Error"] = "Có lỗi xảy ra khi phân tích growth. Vui lòng thử lại sau.";
            return RedirectToAction("Index", "Home");
        }
    }
}
