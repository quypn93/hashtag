using HashTag.Models;
using HashTag.Repositories;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace HashTag.Services;

public class CrawlerService : ICrawlerService
{
    private readonly IHashtagRepository _repository;
    private readonly ILogger<CrawlerService> _logger;
    private readonly IConfiguration _configuration;
    private readonly ISitemapService _sitemapService;

    // TikTok Creative Center Industries
    private readonly Dictionary<string, string> _tiktokIndustries = new()
    {
        { "10000000000", "Education" },
        { "11000000000", "Vehicle & Transportation" },
        { "12000000000", "Baby, Kids & Maternity" },
        { "13000000000", "Financial Services" },
        { "14000000000", "Beauty & Personal Care" },
        { "15000000000", "Tech & Electronics" },
        { "17000000000", "Travel" },
        { "18000000000", "Household Products" },
        { "19000000000", "Pets" },
        { "21000000000", "Home Improvement" },
        { "22000000000", "Apparel & Accessories" },
        { "23000000000", "News & Entertainment" },
        { "24000000000", "Business Services" },
        { "25000000000", "Games" },
        { "26000000000", "Life Services" },
        { "27000000000", "Food & Beverage" },
        { "28000000000", "Sports & Outdoor" },
        { "29000000000", "Health" }
    };

    // Supported regions for regional crawling
    private readonly List<RegionInfo> _supportedRegions = new()
    {
        new RegionInfo { Code = "VN", Name = "Vietnam", NameVi = "Viet Nam" },
        new RegionInfo { Code = "US", Name = "United States", NameVi = "Hoa Ky" },
        new RegionInfo { Code = "GB", Name = "United Kingdom", NameVi = "Vuong quoc Anh" },
        new RegionInfo { Code = "BR", Name = "Brazil", NameVi = "Brazil" },
        new RegionInfo { Code = "IN", Name = "India", NameVi = "An Do" },
        new RegionInfo { Code = "ID", Name = "Indonesia", NameVi = "Indonesia" },
        new RegionInfo { Code = "PH", Name = "Philippines", NameVi = "Philippines" },
        new RegionInfo { Code = "TH", Name = "Thailand", NameVi = "Thai Lan" },
        new RegionInfo { Code = "MY", Name = "Malaysia", NameVi = "Malaysia" },
        new RegionInfo { Code = "DE", Name = "Germany", NameVi = "Duc" },
        new RegionInfo { Code = "FR", Name = "France", NameVi = "Phap" },
        new RegionInfo { Code = "JP", Name = "Japan", NameVi = "Nhat Ban" },
        new RegionInfo { Code = "KR", Name = "South Korea", NameVi = "Han Quoc" },
        new RegionInfo { Code = "MX", Name = "Mexico", NameVi = "Mexico" },
        new RegionInfo { Code = "AU", Name = "Australia", NameVi = "Uc" }
    };

    private const string DefaultCountryCode = "VN";

    public CrawlerService(
        IHashtagRepository repository,
        ILogger<CrawlerService> logger,
        IConfiguration configuration,
        ISitemapService sitemapService)
    {
        _repository = repository;
        _logger = logger;
        _configuration = configuration;
        _sitemapService = sitemapService;
    }

    public IReadOnlyList<RegionInfo> GetSupportedRegions() => _supportedRegions.AsReadOnly();

    public Task<CrawlSummary> CrawlAllSourcesAsync() => CrawlAllSourcesAsync(DefaultCountryCode);

    /// <summary>
    /// Crawl all active sources for ALL configured regions (from appsettings.json)
    /// </summary>
    public async Task<MultiRegionCrawlSummary> CrawlAllRegionsAsync()
    {
        var summary = new MultiRegionCrawlSummary
        {
            StartedAt = DateTime.UtcNow
        };

        // Get configured regions from appsettings.json
        var configuredRegions = _configuration.GetSection("CrawlerSettings:Regions")
            .Get<List<string>>() ?? new List<string> { DefaultCountryCode };

        // Filter to only valid regions
        var validRegions = configuredRegions
            .Where(r => _supportedRegions.Any(s => s.Code.Equals(r, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (!validRegions.Any())
        {
            _logger.LogWarning("No valid regions configured, using default: {Default}", DefaultCountryCode);
            validRegions = new List<string> { DefaultCountryCode };
        }

        summary.TotalRegions = validRegions.Count;
        _logger.LogInformation("Starting multi-region crawl for {Count} regions: {Regions}",
            validRegions.Count, string.Join(", ", validRegions));

        // Crawl each region sequentially
        foreach (var regionCode in validRegions)
        {
            try
            {
                _logger.LogInformation("=== Crawling region: {Region} ===", regionCode);
                var regionSummary = await CrawlAllSourcesAsync(regionCode);

                summary.RegionResults[regionCode] = regionSummary;
                summary.TotalHashtagsCollected += regionSummary.TotalHashtagsCollected;

                if (regionSummary.SuccessfulSources > 0)
                {
                    summary.SuccessfulRegions++;
                    _logger.LogInformation("Region {Region}: SUCCESS - {Hashtags} hashtags collected",
                        regionCode, regionSummary.TotalHashtagsCollected);
                }
                else
                {
                    summary.FailedRegions++;
                    _logger.LogWarning("Region {Region}: FAILED - No hashtags collected", regionCode);
                }

                // Brief pause between regions to avoid rate limiting
                if (regionCode != validRegions.Last())
                {
                    _logger.LogInformation("Waiting 5 seconds before next region...");
                    await Task.Delay(5000);
                }
            }
            catch (Exception ex)
            {
                summary.FailedRegions++;
                _logger.LogError(ex, "Error crawling region {Region}: {Message}", regionCode, ex.Message);
                summary.RegionResults[regionCode] = new CrawlSummary
                {
                    StartedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow,
                    TotalSources = 0,
                    SuccessfulSources = 0,
                    FailedSources = 1
                };
            }
        }

        summary.CompletedAt = DateTime.UtcNow;
        var duration = (summary.CompletedAt - summary.StartedAt).TotalMinutes;

        _logger.LogInformation(
            "Multi-region crawl completed in {Duration:F1} minutes. Regions: {Success}/{Total} successful, Total hashtags: {Hashtags}",
            duration, summary.SuccessfulRegions, summary.TotalRegions, summary.TotalHashtagsCollected);

        return summary;
    }

    public async Task<CrawlSummary> CrawlAllSourcesAsync(string countryCode)
    {
        // Validate country code
        if (!_supportedRegions.Any(r => r.Code.Equals(countryCode, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("Unsupported country code: {CountryCode}, falling back to {Default}", countryCode, DefaultCountryCode);
            countryCode = DefaultCountryCode;
        }

        var summary = new CrawlSummary
        {
            StartedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Starting crawl of all sources at {Time} for region {Region}", summary.StartedAt, countryCode);

        // Get active sources from database
        var sources = await _repository.GetActiveSourcesAsync();
        summary.TotalSources = sources.Count;

        // Get enabled sources from configuration
        var enabledSourceNames = _configuration.GetSection("CrawlerSettings:EnabledSources")
            .Get<List<string>>() ?? new List<string>();

        // Filter to only enabled sources
        var sourcesToCrawl = sources
            .Where(s => enabledSourceNames.Contains(s.Name))
            .ToList();

        _logger.LogInformation("Found {Count} enabled sources to crawl", sourcesToCrawl.Count);

        // Crawl each source with the specified country code
        foreach (var source in sourcesToCrawl)
        {
            var result = await CrawlSourceWithRetryAsync(source.Name, countryCode);
            summary.Results.Add(result);

            if (result.Success)
            {
                summary.SuccessfulSources++;
                summary.TotalHashtagsCollected += result.HashtagsCollected;
            }
            else
            {
                summary.FailedSources++;
            }
        }

        summary.CompletedAt = DateTime.UtcNow;
        _logger.LogInformation("Crawl completed. Success: {Success}, Failed: {Failed}, Total hashtags: {Total}",
            summary.SuccessfulSources, summary.FailedSources, summary.TotalHashtagsCollected);

        // Invalidate sitemap cache after crawl completes (new hashtags added)
        if (summary.TotalHashtagsCollected > 0)
        {
            _logger.LogInformation("Invalidating sitemap cache after successful crawl");
            _sitemapService.InvalidateCache();
        }

        return summary;
    }

    public Task<CrawlResult> CrawlSourceAsync(string sourceName) => CrawlSourceAsync(sourceName, DefaultCountryCode);

    public async Task<CrawlResult> CrawlSourceAsync(string sourceName, string countryCode)
    {
        // Validate country code
        if (!_supportedRegions.Any(r => r.Code.Equals(countryCode, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("Unsupported country code: {CountryCode}, falling back to {Default}", countryCode, DefaultCountryCode);
            countryCode = DefaultCountryCode;
        }

        return await CrawlSourceWithRetryAsync(sourceName, countryCode);
    }

    private async Task<CrawlResult> CrawlSourceWithRetryAsync(string sourceName, string countryCode)
    {
        var maxRetries = _configuration.GetValue<int>("CrawlerSettings:MaxRetries", 3);
        var logMessages = new List<string>();

        void AddLog(string message)
        {
            var logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            logMessages.Add(logEntry);
            _logger.LogInformation(message);
        }

        void AddLogError(string message, Exception? ex = null)
        {
            var logEntry = $"[{DateTime.Now:HH:mm:ss}] ERROR: {message}";
            if (ex != null) logEntry += $"\n  -> {ex.Message}";
            logMessages.Add(logEntry);
            if (ex != null)
                _logger.LogError(ex, message);
            else
                _logger.LogWarning(message);
        }

        var result = new CrawlResult
        {
            SourceName = sourceName,
            StartedAt = DateTime.UtcNow
        };

        AddLog($"Starting crawl for source: {sourceName}");

        // Get source from database
        var source = await _repository.GetSourceByNameAsync(sourceName);
        if (source == null)
        {
            result.Success = false;
            result.ErrorMessage = $"Source '{sourceName}' not found in database";
            result.CompletedAt = DateTime.UtcNow;
            AddLogError($"Source '{sourceName}' not found in database");
            return result;
        }

        // Create crawl log
        var crawlLog = new CrawlLog
        {
            SourceId = source.Id,
            StartedAt = result.StartedAt,
            Success = false,
            HashtagsCollected = 0
        };
        crawlLog = await _repository.AddCrawlLogAsync(crawlLog);

        // Retry logic
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                AddLog($"Attempt {attempt}/{maxRetries}: Crawling {sourceName} for region {countryCode}...");

                var hashtags = await CrawlSourceInternalAsync(sourceName, countryCode);

                if (hashtags.Count > 0)
                {
                    AddLog($"Collected {hashtags.Count} hashtags, saving to database...");
                    await SaveHashtagsAsync(source.Id, hashtags, countryCode);

                    result.Success = true;
                    result.HashtagsCollected = hashtags.Count;
                    result.CompletedAt = DateTime.UtcNow;

                    // Update source
                    source.LastCrawled = DateTime.UtcNow;
                    source.LastError = null;
                    await _repository.UpdateSourceAsync(source);

                    // Update crawl log
                    AddLog($"SUCCESS: Crawled {hashtags.Count} hashtags from {sourceName}");
                    crawlLog.Success = true;
                    crawlLog.HashtagsCollected = hashtags.Count;
                    crawlLog.CompletedAt = DateTime.UtcNow;
                    crawlLog.LogMessages = string.Join("\n", logMessages);
                    await _repository.UpdateCrawlLogAsync(crawlLog);

                    return result;
                }
                else
                {
                    AddLogError($"No hashtags collected from {sourceName} (attempt {attempt}/{maxRetries})");
                    result.ErrorMessage = $"No hashtags collected";
                }
            }
            catch (Exception ex)
            {
                AddLogError($"Error crawling {sourceName} on attempt {attempt}: {ex.Message}", ex);
                result.ErrorMessage = ex.Message;

                // Wait before retry (exponential backoff)
                if (attempt < maxRetries)
                {
                    var delaySeconds = Math.Pow(2, attempt);
                    AddLog($"Waiting {delaySeconds}s before retry...");
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                }
            }
        }

        // All retries failed
        result.Success = false;
        result.CompletedAt = DateTime.UtcNow;

        // Update source with error
        source.LastError = result.ErrorMessage;
        await _repository.UpdateSourceAsync(source);

        // Update crawl log with error
        AddLogError($"FAILED: All {maxRetries} attempts failed for {sourceName}");
        crawlLog.Success = false;
        crawlLog.ErrorMessage = result.ErrorMessage;
        crawlLog.CompletedAt = DateTime.UtcNow;
        crawlLog.LogMessages = string.Join("\n", logMessages);
        await _repository.UpdateCrawlLogAsync(crawlLog);

        return result;
    }

    private async Task<List<HashtagRaw>> CrawlSourceInternalAsync(string sourceName, string countryCode)
    {
        var timeout = _configuration.GetValue<int>("CrawlerSettings:TimeoutSeconds", 120) * 1000;

        _logger.LogInformation("CrawlSourceInternal: Starting for {Source} in region {Region}. Timeout: {Timeout}ms", sourceName, countryCode, timeout);

        IPlaywright? playwright = null;
        IBrowser? browser = null;

        try
        {
            // Step 1: Create Playwright instance
            _logger.LogInformation("CrawlSourceInternal: Step 1 - Creating Playwright instance...");
            try
            {
                playwright = await Playwright.CreateAsync();
                _logger.LogInformation("CrawlSourceInternal: Playwright instance created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "CrawlSourceInternal: FAILED to create Playwright instance!\n" +
                    "This usually means Playwright browsers are not installed.\n" +
                    "Run this command on the server: pwsh bin/Debug/net8.0/playwright.ps1 install\n" +
                    "Error: {Message}\n" +
                    "Type: {Type}",
                    ex.Message, ex.GetType().FullName);
                throw;
            }

            // Step 2: Launch browser
            _logger.LogInformation("CrawlSourceInternal: Step 2 - Launching Chromium browser...");
            try
            {
                browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true
                });
                _logger.LogInformation("CrawlSourceInternal: Browser launched successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "CrawlSourceInternal: FAILED to launch browser!\n" +
                    "Check if Chromium is installed and accessible.\n" +
                    "Error: {Message}\n" +
                    "Type: {Type}",
                    ex.Message, ex.GetType().FullName);
                throw;
            }

            // Step 3: Create page
            _logger.LogInformation("CrawlSourceInternal: Step 3 - Creating new page...");
            var page = await browser.NewPageAsync(new BrowserNewPageOptions
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
            });
            page.SetDefaultTimeout(timeout);
            _logger.LogInformation("CrawlSourceInternal: Page created with timeout {Timeout}ms", timeout);

            // Step 4: Execute crawl
            _logger.LogInformation("CrawlSourceInternal: Step 4 - Starting {Source} crawl for region {Region}...", sourceName, countryCode);
            var result = sourceName switch
            {
                "TikTok" => await CrawlTikTokHashtags(page, countryCode),
                _ => new List<HashtagRaw>()
            };

            _logger.LogInformation("CrawlSourceInternal: SUCCESS - Collected {Count} hashtags from {Source}", result.Count, sourceName);
            return result;
        }
        catch (Exception ex) when (ex is not PlaywrightException)
        {
            _logger.LogError(ex,
                "CrawlSourceInternal: Unexpected error for {Source}\n" +
                "Message: {Message}\n" +
                "Type: {Type}\n" +
                "InnerException: {Inner}\n" +
                "StackTrace: {Stack}",
                sourceName, ex.Message, ex.GetType().FullName,
                ex.InnerException?.Message ?? "None", ex.StackTrace);
            throw;
        }
        finally
        {
            try
            {
                if (browser != null)
                {
                    await browser.CloseAsync();
                    _logger.LogDebug("CrawlSourceInternal: Browser closed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CrawlSourceInternal: Error closing browser: {Message}", ex.Message);
            }

            try
            {
                playwright?.Dispose();
                _logger.LogDebug("CrawlSourceInternal: Playwright disposed");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CrawlSourceInternal: Error disposing Playwright: {Message}", ex.Message);
            }
        }
    }

    private async Task SaveHashtagsAsync(int sourceId, List<HashtagRaw> hashtags, string countryCode = "VN")
    {
        var startTime = DateTime.UtcNow;
        int newHashtagsCount = 0;
        int updatedHashtagsCount = 0;

        _logger.LogInformation("SaveHashtags: Starting bulk save for {Count} hashtags (region: {Region})", hashtags.Count, countryCode);

        // STEP 1: Batch load all existing data (1-3 queries instead of N queries)
        var allTags = hashtags.Select(h => h.Tag.TrimStart('#').ToLower()).Distinct().ToList();
        var allCategories = hashtags.Where(h => !string.IsNullOrWhiteSpace(h.Category))
            .Select(h => h.Category!)
            .Distinct()
            .ToList();
        var today = DateTime.UtcNow.Date;

        _logger.LogDebug("SaveHashtags: Loading existing data - {TagCount} unique tags, {CategoryCount} categories",
            allTags.Count, allCategories.Count);

        // Load all existing hashtags at once (filtered by region)
        var existingHashtags = await _repository.GetHashtagsByTagsAsync(allTags, countryCode);
        var existingHashtagDict = existingHashtags.ToDictionary(h => h.Tag.ToLower());

        // Load all existing categories at once
        var existingCategories = await _repository.GetCategoriesByNamesAsync(allCategories);
        var existingCategoryDict = existingCategories.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

        // Load all existing histories for today at once
        var existingHistories = await _repository.GetHashtagHistoriesForDateAsync(sourceId, today);
        var existingHistoryDict = existingHistories
            .GroupBy(h => $"{h.HashtagId}_{h.Category ?? ""}")
            .ToDictionary(g => g.Key, g => g.First());

        _logger.LogInformation("SaveHashtags: Loaded {Hashtags} hashtags, {Categories} categories, {Histories} histories",
            existingHashtagDict.Count, existingCategoryDict.Count, existingHistories.Count);

        // STEP 2: Prepare batches for insert/update
        var hashtagsToInsert = new List<Models.Hashtag>();
        var hashtagsToUpdate = new List<Models.Hashtag>();
        var categoriesToInsert = new List<HashtagCategory>();
        var historiesToInsert = new List<HashtagHistory>();
        var historiesToUpdate = new List<HashtagHistory>();

        // STEP 3: Process all hashtags in memory
        foreach (var raw in hashtags)
        {
            try
            {
                var normalizedTag = raw.Tag.TrimStart('#').ToLower();

                // Get or prepare hashtag
                Models.Hashtag hashtag;
                bool isNewHashtag = false;

                if (!existingHashtagDict.TryGetValue(normalizedTag, out hashtag!))
                {
                    // New hashtag - prepare for insert
                    hashtag = new Models.Hashtag
                    {
                        Tag = normalizedTag,
                        TagDisplay = raw.Tag.TrimStart('#'),
                        CountryCode = countryCode, // Set region for new hashtag
                        FirstSeen = DateTime.UtcNow,
                        LastSeen = DateTime.UtcNow,
                        TotalAppearances = 1,
                        LatestViewCount = raw.ViewCount,
                        LatestPostCount = raw.PostCount
                    };
                    hashtagsToInsert.Add(hashtag);
                    existingHashtagDict[normalizedTag] = hashtag; // Add to dict for history processing
                    isNewHashtag = true;
                    newHashtagsCount++;
                }
                else
                {
                    // Existing hashtag - prepare for update
                    if (!hashtagsToUpdate.Contains(hashtag))
                    {
                        hashtag.LastSeen = DateTime.UtcNow;
                        if (raw.PostCount.HasValue) hashtag.LatestPostCount = raw.PostCount;
                        if (raw.ViewCount.HasValue) hashtag.LatestViewCount = raw.ViewCount;
                        hashtagsToUpdate.Add(hashtag);
                    }
                }

                // Handle category
                if (!string.IsNullOrWhiteSpace(raw.Category))
                {
                    if (!existingCategoryDict.TryGetValue(raw.Category, out var category))
                    {
                        // New category - prepare for insert
                        category = new HashtagCategory { Name = raw.Category };
                        categoriesToInsert.Add(category);
                        existingCategoryDict[raw.Category] = category;
                    }

                    if (hashtag.CategoryId == null)
                    {
                        hashtag.CategoryId = category.Id;
                        if (category.Id == 0)
                        {
                            // Will be set after insert
                            hashtag.Category = category;
                        }
                    }
                }

                // Handle history
                var historyKey = $"{hashtag.Id}_{raw.Category ?? ""}";

                if (!existingHistoryDict.TryGetValue(historyKey, out var existingHistory))
                {
                    // New history - prepare for insert
                    var history = new HashtagHistory
                    {
                        Hashtag = hashtag,
                        HashtagId = hashtag.Id,
                        SourceId = sourceId,
                        Rank = raw.Rank,
                        CollectedDate = today,
                        CreatedAt = DateTime.UtcNow,
                        ViewCount = raw.ViewCount,
                        PostCount = raw.PostCount,
                        Category = raw.Category,
                        TrendScore = raw.TrendScore,
                        RankChange = raw.RankChange,
                        FeaturedCreatorsJson = raw.FeaturedCreatorsJson,
                        RankDiff = raw.RankDiff,
                        IsViral = raw.IsViral,
                        TrendDataJson = raw.TrendDataJson,
                        TrendMomentum = raw.TrendMomentum
                    };
                    historiesToInsert.Add(history);

                    if (!isNewHashtag)
                    {
                        hashtag.TotalAppearances++;
                    }
                }
                else
                {
                    // Update existing history
                    existingHistory.Rank = raw.Rank;
                    existingHistory.ViewCount = raw.ViewCount ?? existingHistory.ViewCount;
                    existingHistory.PostCount = raw.PostCount ?? existingHistory.PostCount;
                    existingHistory.TrendScore = raw.TrendScore ?? existingHistory.TrendScore;
                    existingHistory.RankChange = raw.RankChange ?? existingHistory.RankChange;
                    existingHistory.RankDiff = raw.RankDiff ?? existingHistory.RankDiff;
                    existingHistory.IsViral = raw.IsViral;
                    existingHistory.TrendDataJson = raw.TrendDataJson ?? existingHistory.TrendDataJson;
                    existingHistory.TrendMomentum = raw.TrendMomentum ?? existingHistory.TrendMomentum;
                    existingHistory.FeaturedCreatorsJson = raw.FeaturedCreatorsJson ?? existingHistory.FeaturedCreatorsJson;

                    if (!historiesToUpdate.Contains(existingHistory))
                    {
                        historiesToUpdate.Add(existingHistory);
                    }
                    updatedHashtagsCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing hashtag {Tag} (Category: {Category}): {Message}",
                    raw.Tag, raw.Category ?? "None", ex.Message);
            }
        }

        // STEP 4: Bulk insert/update (only 4-5 queries total!)
        _logger.LogInformation("SaveHashtags: Executing bulk operations - Insert: {NewHashtags} hashtags, {NewCategories} categories, {NewHistories} histories | Update: {UpdateHashtags} hashtags, {UpdateHistories} histories",
            hashtagsToInsert.Count, categoriesToInsert.Count, historiesToInsert.Count,
            hashtagsToUpdate.Count, historiesToUpdate.Count);

        try
        {
            await _repository.BulkSaveChangesAsync(
                hashtagsToInsert,
                hashtagsToUpdate,
                categoriesToInsert,
                historiesToInsert,
                historiesToUpdate);

            var elapsed = (DateTime.UtcNow - startTime).TotalSeconds;
            var overlapRate = hashtags.Count > 0 ? (double)updatedHashtagsCount / hashtags.Count * 100 : 0;

            _logger.LogInformation(
                "SaveHashtags completed in {Elapsed:F2}s: {Total} crawled, {New} new hashtags, {Updated} updated (overlap: {Overlap:F1}%)",
                elapsed, hashtags.Count, newHashtagsCount, updatedHashtagsCount, overlapRate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SaveHashtags: Bulk save failed: {Message}", ex.Message);
            throw;
        }
    }

    #region Crawler Methods

    private async Task<List<HashtagRaw>> CrawlTikTokHashtags(IPage page, string countryCode)
    {
        var allHashtags = new List<HashtagRaw>();
        var errors = new List<string>();

        // Load cookies once for all crawls
        await LoadTikTokCookies(page);

        // STEP 1: Crawl trending page (general, no industry filter) to get top hashtags with PostCount
        _logger.LogInformation("TikTok: Step 1 - Crawling general trending page for region {Region}", countryCode);

        try
        {
            var trendingHashtags = await CrawlTikTokTrendingPageAsync(page, countryCode);
            _logger.LogInformation("TikTok: Collected {Count} trending hashtags with PostCount", trendingHashtags.Count);
            allHashtags.AddRange(trendingHashtags);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Trending page failed: {ex.Message}";
            errors.Add(errorMsg);
            _logger.LogError(ex, "TikTok: Failed to crawl trending page: {Message}", ex.Message);
        }

        // STEP 2: Crawl all 18 industries sequentially (fast mode)
        _logger.LogInformation("TikTok: Step 2 - Crawling {Count} industries", _tiktokIndustries.Count);

        var industrySuccessCount = 0;
        var industryFailCount = 0;

        foreach (var industry in _tiktokIndustries)
        {
            try
            {
                _logger.LogInformation("TikTok: Crawling industry '{IndustryName}' (ID: {IndustryId})",
                    industry.Value, industry.Key);

                var industryHashtags = await CrawlTikTokIndustryHashtags(page, industry.Key, industry.Value, countryCode);

                _logger.LogInformation("TikTok: Collected {Count} hashtags from {Industry}",
                    industryHashtags.Count, industry.Value);

                if (industryHashtags.Count > 0)
                {
                    industrySuccessCount++;
                    allHashtags.AddRange(industryHashtags);
                }
                else
                {
                    industryFailCount++;
                    var errorMsg = $"{industry.Value}: No hashtags found (empty result)";
                    errors.Add(errorMsg);
                }

                // Brief pause between industries
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                industryFailCount++;
                var errorMsg = $"{industry.Value}: {ex.Message}";
                errors.Add(errorMsg);
                _logger.LogError(ex, "TikTok: Failed to crawl industry {Industry}: {Message}",
                    industry.Value, ex.Message);
            }
        }

        _logger.LogInformation("TikTok: Industry crawl summary - Success: {Success}, Failed: {Failed}",
            industrySuccessCount, industryFailCount);

        // Don't merge duplicates - keep hashtags from each industry separately
        // This allows same hashtag to appear in multiple industries with different categories
        _logger.LogInformation("TikTok: Total hashtags collected: {Total} (from trending + all industries, {WithPostCount} have PostCount)",
            allHashtags.Count, allHashtags.Count(h => h.PostCount.HasValue));

        // If no hashtags collected at all, throw exception with details
        if (allHashtags.Count == 0)
        {
            var errorSummary = errors.Count > 0
                ? string.Join(" | ", errors.Take(5)) // Show first 5 errors
                : "No specific errors logged";

            throw new Exception($"TikTok crawl failed: No hashtags collected from trending page or any of the 18 industries. Failed industries: {industryFailCount}/18. Errors: {errorSummary}. This may indicate cookie expiration, IP blocking, or site structure changes.");
        }

        // Group by industry for logging
        var byIndustry = allHashtags
            .Where(h => !string.IsNullOrEmpty(h.Category))
            .GroupBy(h => h.Category)
            .OrderByDescending(g => g.Count());

        foreach (var group in byIndustry)
        {
            _logger.LogInformation("TikTok: Industry '{Industry}': {Count} hashtags",
                group.Key, group.Count());
        }

        return allHashtags;
    }

    private async Task<List<HashtagRaw>> CrawlTikTokTrendingPageAsync(IPage page, string countryCode)
    {
        var result = new List<HashtagRaw>();

        // Navigate to general trending hashtag page with regional filter
        var url = $"https://ads.tiktok.com/business/creativecenter/inspiration/popular/hashtag/pc/en?countryCode={countryCode}&period=7";
        _logger.LogInformation("TikTok: Crawling trending page: {Url}", url);

        await page.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.Load,
            Timeout = 90000
        });

        await Task.Delay(3000);

        var hashtagSelector = "a[data-testid*='trend_hashtag_item']";

        try
        {
            await page.WaitForSelectorAsync(hashtagSelector, new PageWaitForSelectorOptions { Timeout = 20000 });
        }
        catch (Exception ex)
        {
            var errorMsg = ex is TimeoutException
                ? "Hashtag selector not found (timeout) - possible site structure change or access blocked"
                : $"Failed to find hashtags: {ex.Message}";
            _logger.LogWarning("TikTok: {Error}", errorMsg);
            throw new Exception($"Trending page: {errorMsg}", ex);
        }

        // Scroll to load more hashtags
        int scrollAttempts = 30; // More scrolls for trending page
        int noChangeCount = 0;

        for (int i = 0; i < scrollAttempts; i++)
        {
            var itemsBefore = await page.QuerySelectorAllAsync(hashtagSelector);
            var countBefore = itemsBefore.Count;

            await page.EvaluateAsync(@"async () => {
                const scrollStep = 800;
                for (let i = 0; i < 2; i++) {
                    window.scrollBy(0, scrollStep);
                    await new Promise(resolve => setTimeout(resolve, 200));
                }
            }");
            await Task.Delay(1500);

            var loadMoreButton = await page.QuerySelectorAsync(
                "div[data-testid='cc_contentArea_viewmore_btn'], div[class*='ViewMoreBtn']:visible"
            );

            if (loadMoreButton != null && await loadMoreButton.IsVisibleAsync())
            {
                await loadMoreButton.ScrollIntoViewIfNeededAsync();
                await Task.Delay(300);
                await loadMoreButton.ClickAsync();
                await Task.Delay(2000);
            }

            var itemsAfter = await page.QuerySelectorAllAsync(hashtagSelector);
            if (itemsAfter.Count == countBefore)
            {
                noChangeCount++;
                if (noChangeCount >= 5) break;
            }
            else
            {
                noChangeCount = 0;
            }
        }

        // Parse all hashtags
        var allItems = await page.QuerySelectorAllAsync(hashtagSelector);
        _logger.LogInformation("TikTok Trending: Found {Count} hashtags", allItems.Count);

        var seen = new HashSet<string>();
        int rank = 1;

        foreach (var item in allItems)
        {
            try
            {
                var tagEl = await item.QuerySelectorAsync("span[class*='titleText']");
                if (tagEl == null) continue;

                var tag = (await tagEl.InnerTextAsync()).Trim();
                if (string.IsNullOrWhiteSpace(tag)) continue;

                var normalizedTag = tag.StartsWith("#") ? tag.ToLower() : $"#{tag.ToLower()}";
                if (seen.Contains(normalizedTag)) continue;

                seen.Add(normalizedTag);

                // Extract PostCount from list view using specific selectors
                long? postCount = null;
                try
                {
                    // Method 1: Try to find Posts value using CSS selector (CardPc_itemValue with Posts label)
                    var postValueElements = await item.QuerySelectorAllAsync("span[class*='itemValue']");
                    foreach (var valueEl in postValueElements)
                    {
                        var parent = await valueEl.EvaluateHandleAsync("el => el.parentElement");
                        var parentText = await parent.AsElement()!.InnerTextAsync();

                        if (parentText.Contains("Posts", StringComparison.OrdinalIgnoreCase))
                        {
                            var valueText = await valueEl.InnerTextAsync();
                            postCount = ParseTikTokNumber(valueText.Trim());
                            if (postCount.HasValue && postCount.Value > 0)
                            {
                                break;
                            }
                        }
                    }

                    // Method 2: Fallback to text parsing if CSS selector doesn't work
                    if (!postCount.HasValue)
                    {
                        var itemText = await item.InnerTextAsync();
                        var postPatterns = new[]
                        {
                            @"Posts?\s+([0-9,.]+[KMB]?)",
                            @"([0-9,.]+[KMB]?)\s+Posts?",
                            @"Posts?[:\s]+([0-9,.]+[KMB]?)"
                        };

                        foreach (var pattern in postPatterns)
                        {
                            var match = Regex.Match(itemText, pattern, RegexOptions.IgnoreCase);
                            if (match.Success)
                            {
                                var numberGroup = match.Groups[1].Value;
                                postCount = ParseTikTokNumber(numberGroup);
                                if (postCount.HasValue && postCount.Value > 0)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // If parsing fails, just leave PostCount as null
                }

                result.Add(new HashtagRaw
                {
                    Tag = normalizedTag,
                    Rank = rank++,
                    CollectedDate = DateTime.Today,
                    PostCount = postCount,
                    ViewCount = null,
                    Category = null // Will be filled from industry crawl
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning("TikTok Trending: Error processing hashtag: {Message}", ex.Message);
            }
        }

        _logger.LogInformation("TikTok Trending: Collected {Total} hashtags", result.Count);

        return result;
    }

    private async Task LoadTikTokCookies(IPage page)
    {
        var cookiesJson = _configuration.GetValue<string>("CrawlerSettings:TikTokCookies");
        if (!string.IsNullOrWhiteSpace(cookiesJson))
        {
            try
            {
                // Use JsonDocument to manually parse and convert to Cookie objects
                using var doc = System.Text.Json.JsonDocument.Parse(cookiesJson);
                var cookies = new List<Cookie>();

                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    var cookie = new Cookie
                    {
                        Name = element.GetProperty("name").GetString() ?? "",
                        Value = element.GetProperty("value").GetString() ?? "",
                        Domain = element.GetProperty("domain").GetString(),
                        Path = element.GetProperty("path").GetString(),
                        Expires = element.TryGetProperty("expires", out var exp) ? exp.GetInt64() : default(long?),
                        HttpOnly = element.TryGetProperty("httpOnly", out var http) && http.GetBoolean(),
                        Secure = element.TryGetProperty("secure", out var sec) && sec.GetBoolean(),
                        SameSite = element.TryGetProperty("sameSite", out var same)
                            ? ParseSameSite(same.GetString())
                            : null
                    };
                    cookies.Add(cookie);
                }

                if (cookies.Count > 0)
                {
                    await page.Context.AddCookiesAsync(cookies);
                    _logger.LogInformation("TikTok: Added {Count} cookies for authenticated session", cookies.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("TikTok: Failed to load cookies: {Message}", ex.Message);
            }
        }
    }

    private async Task<List<HashtagRaw>> CrawlTikTokIndustryHashtags(IPage page, string industryId, string industryName, string countryCode)
    {
        var result = new List<HashtagRaw>();

        try
        {
            var seen = new HashSet<string>();

            // Set up response interceptor to capture ALL API responses
            var allResponses = new List<string>();
            EventHandler<IResponse> responseHandler = async (_, response) =>
            {
                try
                {
                    // Check if this is the hashtag list API
                    if (response.Url.Contains("/creative_radar_api/v1/popular_trend/hashtag/list") &&
                        response.Status == 200)
                    {
                        var body = await response.TextAsync();
                        if (!string.IsNullOrWhiteSpace(body))
                        {
                            // Log response URL to verify industry_id is present
                            _logger.LogInformation("TikTok {Industry}: Captured API response URL: {Url}",
                                industryName, response.Url);

                            // Check if response has industry_id parameter
                            if (response.Url.Contains($"industry_id={industryId}"))
                            {
                                allResponses.Add(body);
                                _logger.LogInformation("TikTok {Industry}: ✅ Captured API response WITH industry_id={IndustryId}, length: {Length}",
                                    industryName, industryId, body.Length);
                            }
                            else
                            {
                                _logger.LogWarning("TikTok {Industry}: ❌ API response WITHOUT industry_id (URL: {Url})",
                                    industryName, response.Url);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("TikTok {Industry}: Error capturing response: {Message}",
                        industryName, ex.Message);
                }
            };

            page.Response += responseHandler;

            try
            {
                // Navigate to base trending page with region (no industry filter yet)
                var baseUrl = $"https://ads.tiktok.com/business/creativecenter/inspiration/popular/hashtag/pc/en?country_code={countryCode}&period=7";
                _logger.LogInformation("TikTok {Industry}: Navigating to base page for region {Region}: {Url}", industryName, countryCode, baseUrl);
                await page.GotoAsync(baseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 30000 });
                await Task.Delay(3000);

                // Try to find and click Industry filter dropdown
                _logger.LogInformation("TikTok {Industry}: Looking for industry filter dropdown", industryName);

                // Based on DOM screenshot: div with class containing "byted-select"
                var industryFilterSelectors = new[]
                {
                    "div.byted-select-popover-panel-inner",  // From screenshot
                    "div[class*='byted-select']",
                    "button:has-text('Industry')",
                    "[aria-label*='Industry']",
                    "div:has-text('Industry'):not(:has(div))"  // Direct parent with "Industry" text
                };

                IElementHandle? filterButton = null;
                foreach (var selector in industryFilterSelectors)
                {
                    try
                    {
                        filterButton = await page.QuerySelectorAsync(selector);
                        if (filterButton != null && await filterButton.IsVisibleAsync())
                        {
                            _logger.LogInformation("TikTok {Industry}: Found industry filter with selector: {Selector}",
                                industryName, selector);
                            break;
                        }
                    }
                    catch
                    {
                        // Continue trying other selectors
                    }
                }

                if (filterButton == null)
                {
                    _logger.LogWarning("TikTok {Industry}: Could not find industry filter dropdown, returning empty result",
                        industryName);
                    return result;
                }

                // Click to open dropdown
                await filterButton.ClickAsync();
                _logger.LogInformation("TikTok {Industry}: Clicked industry filter dropdown", industryName);
                await Task.Delay(1500);

                // Find and click specific industry option
                // Based on screenshot: div[data-type="select-option"] with text matching industryName
                var industryOptionSelectors = new[]
                {
                    $"div[data-type='select-option']:has-text('{industryName}')",
                    $"[data-option-id*='Option']:has-text('{industryName}')",
                    $"li:has-text('{industryName}')",
                    $"div.byted-select-option:has-text('{industryName}')"
                };

                IElementHandle? industryOption = null;
                foreach (var selector in industryOptionSelectors)
                {
                    try
                    {
                        industryOption = await page.QuerySelectorAsync(selector);
                        if (industryOption != null && await industryOption.IsVisibleAsync())
                        {
                            _logger.LogInformation("TikTok {Industry}: Found industry option with selector: {Selector}",
                                industryName, selector);
                            break;
                        }
                    }
                    catch
                    {
                        // Continue trying other selectors
                    }
                }

                if (industryOption == null)
                {
                    _logger.LogWarning("TikTok {Industry}: Could not find industry option '{IndustryName}' in dropdown",
                        industryName, industryName);
                    return result;
                }

                // Click the industry option
                await industryOption.ClickAsync();
                _logger.LogInformation("TikTok {Industry}: Clicked industry option '{IndustryName}'", industryName, industryName);
                await Task.Delay(3000); // Wait for API call after selecting industry

                // Now scroll to trigger lazy loading and load more hashtags
                _logger.LogInformation("TikTok {Industry}: Scrolling to load more hashtags", industryName);
                for (int scrollAttempt = 0; scrollAttempt < 40; scrollAttempt++)
                {
                    await page.EvaluateAsync("window.scrollBy(0, 800)");
                    await Task.Delay(800);

                    // Try to click "View More" button
                    try
                    {
                        var viewMoreButton = await page.QuerySelectorAsync(
                            "div[data-testid='cc_contentArea_viewmore_btn'], button:has-text('View more'), button:has-text('Load more')"
                        );

                        if (viewMoreButton != null && await viewMoreButton.IsVisibleAsync())
                        {
                            await viewMoreButton.ClickAsync();
                            _logger.LogInformation("TikTok {Industry}: Clicked 'View More' button (attempt {Attempt})",
                                industryName, scrollAttempt + 1);
                            await Task.Delay(2500); // Wait for new API call
                        }
                    }
                    catch
                    {
                        // Continue scrolling
                    }
                }

                // Final scroll to bottom
                await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
                await Task.Delay(2000);

                _logger.LogInformation("TikTok {Industry}: Captured {Count} API responses total",
                    industryName, allResponses.Count);

                // Parse all captured responses
                foreach (var jsonContent in allResponses)
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(jsonContent);
                        var root = doc.RootElement;

                        // Check if response is successful
                        if (!root.TryGetProperty("code", out var code) || code.GetInt32() != 0)
                        {
                            continue;
                        }

                        // Parse hashtag list
                        if (root.TryGetProperty("data", out var data) &&
                            data.TryGetProperty("list", out var list))
                        {
                            foreach (var item in list.EnumerateArray())
                            {
                                try
                                {
                                    var hashtagName = item.GetProperty("hashtag_name").GetString();
                                    if (string.IsNullOrWhiteSpace(hashtagName))
                                        continue;

                                    var normalizedTag = $"#{hashtagName.ToLower()}";

                                    // Skip duplicates
                                    if (seen.Contains(normalizedTag))
                                        continue;

                                    seen.Add(normalizedTag);

                                    var rank = item.TryGetProperty("rank", out var rankProp) ? rankProp.GetInt32() : 0;
                                    var publishCount = item.TryGetProperty("publish_cnt", out var pubProp) ? (long?)pubProp.GetInt64() : null;
                                    var videoViews = item.TryGetProperty("video_views", out var viewProp) ? (long?)viewProp.GetInt64() : null;

                                    // NEW: Parse rank_diff and trending_type (Phase 1)
                                    var rankDiff = item.TryGetProperty("rank_diff", out var rdProp) ? (int?)rdProp.GetInt32() : null;
                                    var trendingType = item.TryGetProperty("trending_type", out var ttProp) ? ttProp.GetInt32() : 0;
                                    var isViral = trendingType == 1; // trending_type=1 means viral/explosive growth

                                    // NEW: Parse trend array (Phase 2)
                                    string? trendDataJson = null;
                                    decimal? trendMomentum = null;
                                    if (item.TryGetProperty("trend", out var trendArray) && trendArray.ValueKind == System.Text.Json.JsonValueKind.Array)
                                    {
                                        trendDataJson = trendArray.GetRawText();

                                        // Calculate momentum: (last - first) / first * 100
                                        var trendItems = trendArray.EnumerateArray().ToList();
                                        if (trendItems.Count >= 2)
                                        {
                                            var firstValue = trendItems.First().GetProperty("value").GetDouble();
                                            var lastValue = trendItems.Last().GetProperty("value").GetDouble();

                                            if (firstValue > 0)
                                            {
                                                trendMomentum = (decimal)((lastValue - firstValue) / firstValue * 100);
                                            }
                                        }
                                    }

                                    // NEW: Parse creators array (Phase 3)
                                    string? featuredCreatorsJson = null;
                                    if (item.TryGetProperty("creators", out var creatorsArray) && creatorsArray.ValueKind == System.Text.Json.JsonValueKind.Array)
                                    {
                                        featuredCreatorsJson = creatorsArray.GetRawText();
                                    }

                                    result.Add(new HashtagRaw
                                    {
                                        Tag = normalizedTag,
                                        Rank = rank,
                                        CollectedDate = DateTime.Today,
                                        PostCount = publishCount,
                                        ViewCount = videoViews,
                                        Category = industryName,
                                        RankDiff = rankDiff,
                                        IsViral = isViral,
                                        TrendDataJson = trendDataJson,
                                        TrendMomentum = trendMomentum,
                                        FeaturedCreatorsJson = featuredCreatorsJson
                                    });
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogDebug("TikTok {Industry}: Error parsing hashtag: {Message}",
                                        industryName, ex.Message);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("TikTok {Industry}: Error parsing JSON response: {Message}",
                            industryName, ex.Message);
                    }
                }

                _logger.LogInformation("TikTok {Industry}: Collected {Total} unique hashtags",
                    industryName, result.Count);
            }
            finally
            {
                // IMPORTANT: Unregister event handler to prevent memory leaks
                page.Response -= responseHandler;
            }

            return result;
        }
        catch (Exception ex)
        {
            // Determine error type for better diagnostics
            var errorType = ex switch
            {
                TimeoutException => "Timeout",
                PlaywrightException pwEx when pwEx.Message.Contains("Target closed") => "Page closed/crashed",
                PlaywrightException pwEx when pwEx.Message.Contains("Navigation") => "Navigation failed",
                PlaywrightException pwEx when pwEx.Message.Contains("waiting") => "Element not found",
                System.Text.Json.JsonException => "JSON parsing failed",
                _ => "Unknown error"
            };

            var detailedMessage = $"{errorType}: {ex.Message}";
            _logger.LogError(ex, "TikTok {Industry} API: {ErrorType} - {Message}", industryName, errorType, ex.Message);

            // Re-throw with more context
            throw new Exception(detailedMessage, ex);
        }
    }

    private async Task<List<HashtagRaw>> CrawlGoogleTrendsDailyHashtags(IPage page)
    {
        var result = new List<HashtagRaw>();

        try
        {
            await page.GotoAsync(
                "https://trends.google.com/trends/trendingsearches/daily?geo=VN",
                new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.NetworkIdle,
                    Timeout = 60000
                }
            );

            await page.WaitForFunctionAsync(@"() => {
                const sels = ['#trend-table table tr', 'table[role=grid] tr', 'tr[jsname=OkdM2c]', 'div.mZ3Rlc'];
                for (const s of sels) {
                    if (document.querySelectorAll(s).length > 0) return true;
                }
                return false;
            }", new PageWaitForFunctionOptions { Timeout = 20000 });

            // Click next page button multiple times to load more data
            _logger.LogInformation("GoogleTrends: Clicking through pages...");
            for (int pageNum = 0; pageNum < 5; pageNum++) // Get 5 pages of data for more hashtags
            {
                try
                {
                    // Scroll to load current page
                    for (int i = 0; i < 5; i++)
                    {
                        await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
                        await Task.Delay(800);
                    }

                    if (pageNum > 0)
                    {
                        // Find and click "Next page" button using the selector from DOM
                        // Button has jsname="ViaHrd" and aria-label="Go to next page"
                        var nextButton = await page.QuerySelectorAsync(
                            "button[jsname='ViaHrd'][aria-label='Go to next page'], " +
                            "button[aria-label='Go to next page']:not([disabled])"
                        );

                        if (nextButton != null && await nextButton.IsEnabledAsync())
                        {
                            await nextButton.ClickAsync();
                            _logger.LogInformation("GoogleTrends: Clicked next page button (page {Page})", pageNum + 1);
                            await Task.Delay(3000); // Wait for new page to load
                        }
                        else
                        {
                            _logger.LogInformation("GoogleTrends: No more pages available");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("GoogleTrends: Error navigating pages: {Message}", ex.Message);
                    break;
                }
            }

            var titles = await page.EvaluateAsync<string[]>(@"() => {
                const selectors = [
                    '#trend-table table tr',
                    'table[role=grid] tr',
                    'tr[jsname=OkdM2c]',
                    'div.mZ3Rlc',
                    '[class*=""trend""] td',
                    '[class*=""trending""] div',
                    'feed-item',
                    '.feed-item'
                ];
                const seen = new Set();
                const out = [];

                for (const sel of selectors) {
                    const nodes = Array.from(document.querySelectorAll(sel));
                    for (const n of nodes) {
                        let text = '';
                        if (n.tagName && n.tagName.toLowerCase() === 'tr') {
                            // Lấy từ cột 2 hoặc cột đầu tiên
                            const td = n.querySelector('td:nth-child(2)') || n.querySelector('td');
                            if (td) text = td.innerText;
                        } else {
                            text = n.innerText;
                        }
                        if (text) {
                            // Lấy dòng đầu tiên (tiêu đề chính)
                            text = text.split('\n')[0].trim();
                            // Loại bỏ các text quá ngắn hoặc là số
                            if (text && text.length > 2 && !seen.has(text)) {
                                seen.add(text);
                                out.push(text);
                            }
                        }
                    }
                    if (out.length >= 200) break; // Get up to 50 trends
                }
                return out;
            }");

            int rank = 1;
            var seen = new HashSet<string>();

            foreach (var title in titles ?? Array.Empty<string>())
            {
                try
                {
                    var t = (title ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(t))
                        continue;

                    // Normalize: giữ chữ và số, thay space bằng underscore
                    var normalized = Regex.Replace(t, @"[^\p{L}\p{Nd}\s]", "");
                    normalized = Regex.Replace(normalized, @"\s+", "_").Trim();

                    if (string.IsNullOrWhiteSpace(normalized) || normalized.Length < 2)
                        continue;

                    var tag = (normalized.StartsWith("#") ? normalized : $"#{normalized}").ToLower();

                    // Tránh duplicate
                    if (seen.Contains(tag))
                        continue;

                    seen.Add(tag);

                    result.Add(new HashtagRaw
                    {
                        Tag = tag,
                        Rank = rank++,
                        CollectedDate = DateTime.Today
                    });
                }
                catch
                {
                    // ignore per-item errors
                }
            }

            _logger.LogInformation("GoogleTrends: Collected {Count} unique hashtags", result.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google Trends crawl error: {Message}", ex.Message);
        }

        return result;
    }

    private async Task<List<HashtagRaw>> CrawlTokChartHashtags(IPage page)
    {
        var result = new List<HashtagRaw>();
        var seen = new HashSet<string>();

        // Crawl from both URLs
        var urls = new[]
        {
            "https://tokchart.com/dashboard/hashtags/most-views",
            "https://tokchart.com/dashboard/hashtags/growing"
        };

        foreach (var url in urls)
        {
            try
            {
                _logger.LogInformation("TokChart: Crawling {Url}", url);
                await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 90000 });
                await Task.Delay(3000);

                try
                {
                    await page.WaitForSelectorAsync("body", new PageWaitForSelectorOptions { Timeout = 10000 });
                }
                catch
                {
                    _logger.LogDebug("TokChart: Selector timeout, continuing...");
                }

                // Scroll để load thêm nội dung
                for (int i = 0; i < 20; i++)
                {
                    await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
                    await Task.Delay(2000);
                }

                // Extract hashtags
                var hashtags = await page.EvaluateAsync<string[]>(@"() => {
                    const seen = new Set();
                    const out = [];
                    const allElements = document.querySelectorAll('*');
                    const hashtagRegex = /#[a-zA-Z0-9_]+/g;

                    for (const el of allElements) {
                        const href = el.getAttribute('href') || '';
                        if (href.includes('/hashtag/')) {
                            const match = href.match(/\/hashtag\/([^\/\?]+)/);
                            if (match && match[1]) {
                                const tag = '#' + decodeURIComponent(match[1]).toLowerCase();
                                if (!seen.has(tag) && tag.length > 2) {
                                    seen.add(tag);
                                    out.push(tag);
                                }
                            }
                        }

                        const text = el.innerText || el.textContent || '';
                        if (text.length < 5000) {
                            const matches = text.match(hashtagRegex);
                            if (matches) {
                                for (const tag of matches) {
                                    const normalized = tag.toLowerCase();
                                    if (!seen.has(normalized) && normalized.length > 2) {
                                        seen.add(normalized);
                                        out.push(normalized);
                                    }
                                }
                            }
                        }
                    }
                    return out;
                }");

                foreach (var tag in hashtags ?? Array.Empty<string>())
                {
                    var normalized = tag.Trim();
                    if (!seen.Contains(normalized))
                    {
                        seen.Add(normalized);
                        result.Add(new HashtagRaw
                        {
                            Tag = normalized,
                            Rank = result.Count + 1,
                            CollectedDate = DateTime.Today
                        });
                    }
                }

                _logger.LogInformation("TokChart: Collected {Count} hashtags from {Url}", hashtags?.Length ?? 0, url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TokChart crawl error for {Url}: {Message}", url, ex.Message);
            }
        }

        _logger.LogInformation("TokChart: Total unique hashtags: {Count}", result.Count);
        return result;
    }

    private async Task<List<HashtagRaw>> CrawlCountikHashtags(IPage page)
    {
        var result = new List<HashtagRaw>();
        try
        {
            _logger.LogDebug("Countik: Starting navigation...");
            await page.GotoAsync("https://countik.com/popular/hashtags",
                new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 90000 });

            _logger.LogDebug("Countik: Page loaded, waiting...");
            await Task.Delay(3000);

            try
            {
                await page.WaitForSelectorAsync("body", new PageWaitForSelectorOptions { Timeout = 10000 });
            }
            catch
            {
                _logger.LogDebug("Countik: Selector timeout, continuing...");
            }

            // Scroll nhiều lần để load thêm hashtags
            _logger.LogDebug("Countik: Scrolling...");
            for (int i = 0; i < 20; i++)
            {
                await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
                await Task.Delay(2000);
            }

            _logger.LogDebug("Countik: Extracting hashtags...");
            var hashtags = await page.EvaluateAsync<string[]>(@"() => {
                const seen = new Set();
                const out = [];

                // Tìm tất cả elements
                const allElements = document.querySelectorAll('*');
                const hashtagRegex = /#[a-zA-Z0-9_]+/g;

                for (const el of allElements) {
                    // Check href first
                    const href = el.getAttribute('href') || '';
                    if (href.includes('/hashtag/')) {
                        const match = href.match(/\/hashtag\/([^\/\?]+)/);
                        if (match && match[1]) {
                            const tag = '#' + decodeURIComponent(match[1]).toLowerCase();
                            if (!seen.has(tag) && tag.length > 2) {
                                seen.add(tag);
                                out.push(tag);
                            }
                        }
                    }

                    // Then check text content
                    const text = el.innerText || el.textContent || '';
                    if (text.length < 5000) { // Avoid very large nodes
                        const matches = text.match(hashtagRegex);
                        if (matches) {
                            for (const tag of matches) {
                                const normalized = tag.toLowerCase();
                                if (!seen.has(normalized) && normalized.length > 2) {
                                    seen.add(normalized);
                                    out.push(normalized);
                                }
                            }
                        }
                    }
                }

                return out;
            }");

            int rank = 1;
            foreach (var tag in hashtags ?? Array.Empty<string>())
            {
                result.Add(new HashtagRaw
                {
                    Tag = tag.Trim(),
                    Rank = rank++,
                    CollectedDate = DateTime.Today
                });
            }

            _logger.LogInformation("Countik: Collected {Count} unique hashtags", result.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Countik crawl error: {Message}\nStack: {Stack}", ex.Message, ex.StackTrace);
        }
        return result;
    }

    private async Task<List<HashtagRaw>> CrawlBufferHashtags(IPage page)
    {
        var result = new List<HashtagRaw>();
        try
        {
            await page.GotoAsync("https://buffer.com/resources/tiktok-hashtags/",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60000 });

            await page.WaitForSelectorAsync("p, article, main", new PageWaitForSelectorOptions { Timeout = 20000 });

            // Scroll nhiều lần để load toàn bộ content
            for (int i = 0; i < 20; i++)
            {
                await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
                await Task.Delay(1500);
            }

            var hashtags = await page.EvaluateAsync<string[]>(@"() => {
                // Tìm hashtags trong tất cả text elements
                const elements = Array.from(document.querySelectorAll('p, li, h1, h2, h3, h4, h5, h6, span, div, article, section, td, strong, em, code'));
                const seen = new Set();
                const out = [];
                const hashtagRegex = /#[a-zA-Z0-9_]+/g;

                for (const el of elements) {
                    const text = el.innerText || el.textContent || '';
                    const matches = text.match(hashtagRegex);
                    if (matches) {
                        for (const tag of matches) {
                            const normalized = tag.toLowerCase();
                            if (!seen.has(normalized)) {
                                seen.add(normalized);
                                out.push(normalized);
                            }
                        }
                    }
                }
                return out;
            }");

            int rank = 1;
            foreach (var tag in hashtags ?? Array.Empty<string>())
            {
                result.Add(new HashtagRaw
                {
                    Tag = tag.Trim(),
                    Rank = rank++,
                    CollectedDate = DateTime.Today
                });
            }

            _logger.LogInformation("Buffer: Collected {Count} unique hashtags", result.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Buffer crawl error: {Message}", ex.Message);
        }
        return result;
    }

    private async Task<List<HashtagRaw>> CrawlTrollishlyHashtags(IPage page)
    {
        var result = new List<HashtagRaw>();
        try
        {
            await page.GotoAsync("https://www.trollishly.com/tiktok-trending-hashtags/",
                new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60000 });

            await page.WaitForSelectorAsync("body", new PageWaitForSelectorOptions { Timeout = 20000 });

            // Scroll nhiều lần để load thêm content
            for (int i = 0; i < 20; i++)
            {
                await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
                await Task.Delay(1500);
            }

            var hashtags = await page.EvaluateAsync<string[]>(@"() => {
                const seen = new Set();
                const out = [];

                // Tìm trong tất cả elements
                const elements = Array.from(document.querySelectorAll('p, li, div, span, h1, h2, h3, h4, h5, h6, article, section, td, strong, a'));
                const hashtagRegex = /#[a-zA-Z0-9_]+/g;

                for (const el of elements) {
                    const text = el.innerText || el.textContent || '';
                    const matches = text.match(hashtagRegex);
                    if (matches) {
                        for (const tag of matches) {
                            const normalized = tag.toLowerCase();
                            if (!seen.has(normalized)) {
                                seen.add(normalized);
                                out.push(normalized);
                            }
                        }
                    }
                }
                return out;
            }");

            int rank = 1;
            foreach (var tag in hashtags ?? Array.Empty<string>())
            {
                result.Add(new HashtagRaw
                {
                    Tag = tag.Trim(),
                    Rank = rank++,
                    CollectedDate = DateTime.Today
                });
            }

            _logger.LogInformation("Trollishly: Collected {Count} unique hashtags", result.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trollishly crawl error: {Message}", ex.Message);
        }
        return result;
    }

    private async Task<List<HashtagRaw>> CrawlCapcutHashtags(IPage page)
    {
        var result = new List<HashtagRaw>();
        try
        {
            _logger.LogDebug("CapCut: Starting navigation...");
            await page.GotoAsync("https://www.capcut.com/resource/tiktok-hashtag-guide",
                new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = 90000 });

            _logger.LogDebug("CapCut: Page loaded, waiting for content...");
            await Task.Delay(3000); // Wait for any client-side rendering

            // Try multiple wait strategies
            try
            {
                await page.WaitForSelectorAsync("body", new PageWaitForSelectorOptions { Timeout = 10000 });
            }
            catch
            {
                _logger.LogDebug("CapCut: No specific selector found, continuing anyway...");
            }

            // Scroll nhiều lần để load toàn bộ content
            _logger.LogDebug("CapCut: Scrolling to load content...");
            for (int i = 0; i < 20; i++)
            {
                await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
                await Task.Delay(2000);
            }

            _logger.LogDebug("CapCut: Extracting hashtags...");
            var hashtags = await page.EvaluateAsync<string[]>(@"() => {
                // Tìm trong tất cả text elements
                const elements = Array.from(document.querySelectorAll('*'));
                const seen = new Set();
                const out = [];
                const hashtagRegex = /#[a-zA-Z0-9_]+/g;

                for (const el of elements) {
                    const text = el.innerText || el.textContent || '';
                    if (text.length < 10000) { // Skip very large text nodes
                        const matches = text.match(hashtagRegex);
                        if (matches) {
                            for (const tag of matches) {
                                const normalized = tag.toLowerCase();
                                if (!seen.has(normalized)) {
                                    seen.add(normalized);
                                    out.push(normalized);
                                }
                            }
                        }
                    }
                }
                return out;
            }");

            int rank = 1;
            foreach (var tag in hashtags ?? Array.Empty<string>())
            {
                result.Add(new HashtagRaw
                {
                    Tag = tag.Trim(),
                    Rank = rank++,
                    CollectedDate = DateTime.Today
                });
            }

            _logger.LogInformation("CapCut: Collected {Count} unique hashtags", result.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CapCut crawl error: {Message}\nStack: {Stack}", ex.Message, ex.StackTrace);
        }
        return result;
    }

    private Task<List<HashtagRaw>> CrawlPicukiHashtags(IPage page)
    {
        var result = new List<HashtagRaw>();
        _logger.LogWarning("Picuki.com is no longer available.");
        return Task.FromResult(result);
    }

    /// <summary>
    /// Parse SameSite attribute from string to enum
    /// </summary>
    private static SameSiteAttribute? ParseSameSite(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.ToLowerInvariant() switch
        {
            "strict" => SameSiteAttribute.Strict,
            "lax" => SameSiteAttribute.Lax,
            "none" => SameSiteAttribute.None,
            _ => null
        };
    }

    /// <summary>
    /// Parse TikTok numbers like "161.98M", "222K", "161,981,859" to long
    /// </summary>
    private static long? ParseTikTokNumber(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        try
        {
            // Remove common non-numeric characters except . and ,
            var cleaned = Regex.Replace(text, @"[^\d.,KMB]", "", RegexOptions.IgnoreCase);

            if (string.IsNullOrWhiteSpace(cleaned))
                return null;

            // Handle K, M, B suffixes
            double multiplier = 1;
            if (cleaned.EndsWith("K", StringComparison.OrdinalIgnoreCase))
            {
                multiplier = 1_000;
                cleaned = cleaned[..^1];
            }
            else if (cleaned.EndsWith("M", StringComparison.OrdinalIgnoreCase))
            {
                multiplier = 1_000_000;
                cleaned = cleaned[..^1];
            }
            else if (cleaned.EndsWith("B", StringComparison.OrdinalIgnoreCase))
            {
                multiplier = 1_000_000_000;
                cleaned = cleaned[..^1];
            }

            // Remove commas and parse
            cleaned = cleaned.Replace(",", "");

            if (double.TryParse(cleaned, out var number))
            {
                return (long)(number * multiplier);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    #endregion
}

// DTO class for raw hashtag data from crawlers
public class HashtagRaw
{
    public required string Tag { get; set; }
    public int Rank { get; set; }
    public DateTime CollectedDate { get; set; }

    // TikTok Creative Center metadata
    public long? ViewCount { get; set; }
    public long? PostCount { get; set; }
    public string? Category { get; set; }
    public decimal? TrendScore { get; set; }
    public string? RankChange { get; set; }
    public string? FeaturedCreatorsJson { get; set; }

    // NEW: Phase 1 - Rank momentum fields
    public int? RankDiff { get; set; }  // From rank_diff field (e.g., +15, -5)
    public bool IsViral { get; set; }   // From trending_type==1 (viral/explosive growth)

    // NEW: Phase 2 - Trend analysis fields
    public string? TrendDataJson { get; set; }  // JSON array of 7-day trend data
    public decimal? TrendMomentum { get; set; }  // Calculated momentum score
}

// Helper class for TikTok detail page metadata
internal class TikTokHashtagMetadata
{
    public long? ViewCount { get; set; }
    public long? PostCount { get; set; }
    public string? Category { get; set; }
}
