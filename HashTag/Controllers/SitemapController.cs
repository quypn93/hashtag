using HashTag.Services;
using Microsoft.AspNetCore.Mvc;

namespace HashTag.Controllers;

/// <summary>
/// Controller for serving sitemap.xml to search engines
/// </summary>
public class SitemapController : Controller
{
    private readonly ISitemapService _sitemapService;
    private readonly ILogger<SitemapController> _logger;

    public SitemapController(
        ISitemapService sitemapService,
        ILogger<SitemapController> logger)
    {
        _sitemapService = sitemapService;
        _logger = logger;
    }

    /// <summary>
    /// GET: /sitemap.xml
    /// Returns sitemap XML for search engines
    /// </summary>
    [HttpGet]
    [Route("sitemap.xml")]
    [ResponseCache(Duration = 3600)] // Cache for 1 hour in browser
    public async Task<IActionResult> Index()
    {
        try
        {
            _logger.LogInformation("Sitemap.xml requested");

            var xml = await _sitemapService.GetSitemapXmlAsync();

            return Content(xml, "application/xml");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating sitemap.xml: {Message}", ex.Message);
            return StatusCode(500, "Error generating sitemap");
        }
    }

    /// <summary>
    /// POST: /sitemap/refresh
    /// Admin endpoint to force sitemap regeneration
    /// </summary>
    [HttpPost]
    [Route("sitemap/refresh")]
    public IActionResult Refresh()
    {
        try
        {
            _logger.LogInformation("Manual sitemap refresh requested");
            _sitemapService.InvalidateCache();
            return Ok(new { success = true, message = "Sitemap cache invalidated. Will regenerate on next request." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing sitemap: {Message}", ex.Message);
            return StatusCode(500, new { success = false, message = "Error refreshing sitemap" });
        }
    }
}
