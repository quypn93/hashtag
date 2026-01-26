using HashTag.Repositories;
using HashTag.Services;
using Microsoft.AspNetCore.Mvc;

namespace HashTag.Controllers.Api;

[ApiController]
[Route("api/hashtags")]
public class HashtagApiController : ControllerBase
{
    private readonly IHashtagAnalyticsService _analyticsService;
    private readonly IHashtagRepository _repository;
    private readonly ILogger<HashtagApiController> _logger;

    public HashtagApiController(
        IHashtagAnalyticsService analyticsService,
        IHashtagRepository repository,
        ILogger<HashtagApiController> logger)
    {
        _analyticsService = analyticsService;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/hashtags/suggest?q=dance
    /// Returns auto-suggest results for hashtag search
    /// </summary>
    [HttpGet("suggest")]
    public async Task<IActionResult> Suggest([FromQuery] string q)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            {
                return Ok(Array.Empty<object>());
            }

            // Get top 10 matching hashtags
            var results = await _repository.SearchHashtagsAsync(q, 1, 10);

            var suggestions = results.Items.Select(h => new
            {
                h.Id,
                h.Tag,
                h.TagDisplay,
                h.BestRank,
                h.TotalAppearances,
                h.Sources
            });

            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suggestions for query: {Query}", q);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// GET /api/hashtags/trending-chart?days=7
    /// Returns data for trending hashtags line chart
    /// </summary>
    [HttpGet("trending-chart")]
    public async Task<IActionResult> GetTrendingChart([FromQuery] int days = 7)
    {
        try
        {
            if (days < 1 || days > 90)
            {
                return BadRequest(new { error = "Days must be between 1 and 90" });
            }

            var data = await _analyticsService.GetTrendingChartDataAsync(days);
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trending chart data");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// GET /api/hashtags/source-comparison
    /// Returns data for source comparison bar chart
    /// </summary>
    [HttpGet("source-comparison")]
    public async Task<IActionResult> GetSourceComparison()
    {
        try
        {
            var data = await _analyticsService.GetSourceComparisonDataAsync();
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting source comparison data");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// GET /api/hashtags/{id}/history?days=30
    /// Returns history data for individual hashtag
    /// </summary>
    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetHashtagHistory(int id, [FromQuery] int days = 30)
    {
        try
        {
            if (days < 1 || days > 365)
            {
                return BadRequest(new { error = "Days must be between 1 and 365" });
            }

            var data = await _analyticsService.GetHashtagHistoryDataAsync(id, days);

            if (string.IsNullOrEmpty(data.HashtagName))
            {
                return NotFound(new { error = "Hashtag not found" });
            }

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hashtag history for {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// GET /api/hashtags/daily-activity?days=30
    /// Returns daily activity chart data
    /// </summary>
    [HttpGet("daily-activity")]
    public async Task<IActionResult> GetDailyActivity([FromQuery] int days = 30)
    {
        try
        {
            if (days < 1 || days > 90)
            {
                return BadRequest(new { error = "Days must be between 1 and 90" });
            }

            var data = await _analyticsService.GetDailyActivityDataAsync(days);
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily activity data");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
