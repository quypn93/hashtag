using Microsoft.Playwright;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace HashTag.Services;

public class TikTokLiveSearchService : ITikTokLiveSearchService
{
    private readonly ILogger<TikTokLiveSearchService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IPlaywright? _playwright;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _browserLock = new(1, 1);

    public TikTokLiveSearchService(
        ILogger<TikTokLiveSearchService> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<LiveHashtagResult?> SearchTikTokCreativeCenterAsync(string hashtag)
    {
        try
        {
            // Normalize hashtag (remove # if present, lowercase)
            var normalizedTag = hashtag.TrimStart('#').ToLower();

            _logger.LogInformation("Live searching TikTok Creative Center for hashtag: {Tag}", normalizedTag);

            await _browserLock.WaitAsync();
            try
            {
                // Initialize Playwright if not already done
                if (_playwright == null)
                {
                    var playwright = await Playwright.CreateAsync();
                    _browser = await playwright.Chromium.LaunchAsync(new()
                    {
                        Headless = true,
                        Args = new[] { "--disable-blink-features=AutomationControlled" }
                    });
                }

                if (_browser == null)
                {
                    _logger.LogError("Browser not initialized");
                    return null;
                }

                // Load TikTok cookies from configuration
                var cookiesJson = _configuration["CrawlerSettings:TikTokCookies"];
                List<Cookie>? cookies = null;

                if (!string.IsNullOrEmpty(cookiesJson))
                {
                    try
                    {
                        cookies = JsonSerializer.Deserialize<List<Cookie>>(cookiesJson);
                        _logger.LogDebug("Loaded {Count} TikTok cookies from configuration", cookies?.Count ?? 0);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse TikTok cookies: {Message}", ex.Message);
                    }
                }

                var context = await _browser.NewContextAsync(new()
                {
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                    Locale = "en-US",
                    TimezoneId = "America/New_York"
                });

                // Add cookies to context if available
                if (cookies != null && cookies.Count > 0)
                {
                    await context.AddCookiesAsync(cookies);
                    _logger.LogDebug("Added {Count} cookies to browser context", cookies.Count);
                }

                var page = await context.NewPageAsync();

                // Navigate to TikTok Creative Center hashtag detail page
                var url = $"https://ads.tiktok.com/business/creativecenter/hashtag/{normalizedTag}/pc/en?countryCode=VN&period=7";

                _logger.LogDebug("Navigating to: {Url}", url);

                var response = await page.GotoAsync(url, new()
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = 30000
                });

                // Check if page loaded successfully
                if (response == null || !response.Ok)
                {
                    _logger.LogWarning("Failed to load page for hashtag {Tag}: HTTP {Status}", normalizedTag, response?.Status);
                    await context.CloseAsync();
                    return null;
                }

                // Wait for content to load
                await Task.Delay(3000);

                // Extract hashtag data from the page
                var result = await ExtractHashtagDataAsync(page, normalizedTag);

                await context.CloseAsync();

                if (result != null)
                {
                    _logger.LogInformation("Successfully found live data for #{Tag}: PostCount={PostCount}",
                        normalizedTag, result.PostCount);
                }
                else
                {
                    _logger.LogWarning("Hashtag #{Tag} not found or no data available", normalizedTag);
                }

                return result;
            }
            finally
            {
                _browserLock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during live search for hashtag {Tag}: {Message}", hashtag, ex.Message);
            return null;
        }
    }

    private async Task<LiveHashtagResult?> ExtractHashtagDataAsync(IPage page, string normalizedTag)
    {
        try
        {
            // Check if hashtag page exists (not a 404 or error page)
            var pageContent = await page.ContentAsync();

            if (pageContent.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                pageContent.Contains("doesn't exist", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Extract PostCount from the page
            long? postCount = null;

            // Try multiple patterns to find post count
            var postPatterns = new[]
            {
                @"posts?[:\s]+([0-9,.]+[KMB]?)",           // "Posts: 5.2K"
                @"([0-9,.]+[KMB]?)\s+posts?",              // "5.2K Posts"
                @"post\s+count[:\s]+([0-9,.]+[KMB]?)",     // "Post count: 5234"
                @"([0-9,.]+[KMB]?)\s*videos?",             // "5.2K videos"
                @"total[:\s]+([0-9,.]+[KMB]?)",            // "Total: 5234"
            };

            foreach (var pattern in postPatterns)
            {
                var match = Regex.Match(pageContent, pattern, RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    var countStr = match.Groups[1].Value.Trim();
                    postCount = ParseCountString(countStr);

                    if (postCount.HasValue)
                    {
                        _logger.LogDebug("Extracted PostCount: {Count} from pattern: {Pattern}", countStr, pattern);
                        break;
                    }
                }
            }

            // Try to get from specific elements (TikTok Creative Center structure)
            if (!postCount.HasValue)
            {
                try
                {
                    // Look for data-testid or specific class names
                    var statsElements = await page.QuerySelectorAllAsync("div[class*='statistic'], div[class*='metric'], span[class*='count']");

                    foreach (var element in statsElements)
                    {
                        var text = await element.InnerTextAsync();
                        var countMatch = Regex.Match(text, @"([0-9,.]+[KMB]?)", RegexOptions.IgnoreCase);

                        if (countMatch.Success)
                        {
                            var parsed = ParseCountString(countMatch.Groups[1].Value);
                            if (parsed.HasValue && parsed.Value > 0)
                            {
                                postCount = parsed;
                                _logger.LogDebug("Extracted PostCount from element: {Count}", countMatch.Groups[1].Value);
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("Could not extract from elements: {Message}", ex.Message);
                }
            }

            // If we found any data, return result
            if (postCount.HasValue || pageContent.Contains($"#{normalizedTag}", StringComparison.OrdinalIgnoreCase))
            {
                return new LiveHashtagResult
                {
                    Tag = normalizedTag,
                    TagDisplay = $"#{normalizedTag}",
                    PostCount = postCount,
                    ViewCount = null, // ViewCount not available in Creative Center
                    IsAvailable = true
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting hashtag data: {Message}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Parse count string like "5.2K", "1.5M", "234" to long
    /// </summary>
    private long? ParseCountString(string countStr)
    {
        try
        {
            countStr = countStr.Replace(",", "").Trim();

            if (string.IsNullOrWhiteSpace(countStr))
                return null;

            // Extract number and multiplier
            var match = Regex.Match(countStr, @"^([\d.]+)([KMB]?)$", RegexOptions.IgnoreCase);

            if (!match.Success)
                return null;

            if (!double.TryParse(match.Groups[1].Value, out var number))
                return null;

            var multiplier = match.Groups[2].Value.ToUpperInvariant();

            var result = multiplier switch
            {
                "K" => (long)(number * 1_000),
                "M" => (long)(number * 1_000_000),
                "B" => (long)(number * 1_000_000_000),
                _ => (long)number
            };

            return result;
        }
        catch
        {
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.CloseAsync();
            await _browser.DisposeAsync();
        }
    }
}
