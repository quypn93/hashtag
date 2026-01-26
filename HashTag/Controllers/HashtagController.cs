using HashTag.Helpers;
using HashTag.Models;
using HashTag.Repositories;
using HashTag.Services;
using HashTag.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace HashTag.Controllers;

public class HashtagController : Controller
{
    private readonly IHashtagRepository _repository;
    private readonly ITikTokLiveSearchService _liveSearchService;
    private readonly IHashtagGeneratorService _generatorService;
    private readonly ILogger<HashtagController> _logger;

    public HashtagController(
        IHashtagRepository repository,
        ITikTokLiveSearchService liveSearchService,
        IHashtagGeneratorService generatorService,
        ILogger<HashtagController> logger)
    {
        _repository = repository;
        _liveSearchService = liveSearchService;
        _generatorService = generatorService;
        _logger = logger;
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index(HashtagFilterViewModel? filters, int page = 1)
    {
        try
        {
            const int pageSize = 50; // Show 50 hashtags per page

            // Get trending hashtags with filters
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

            // Get available sources for filter dropdown
            var sources = await _repository.GetActiveSourcesAsync();
            var sourceOptions = sources.Select(s => new SourceOption
            {
                Id = s.Id,
                Name = s.Name,
                IsSelected = filters?.SelectedSourceIds?.Contains(s.Id) ?? false
            }).ToList();

            // Get available categories
            var categories = await _repository.GetActiveCategoriesAsync();
            var categoryOptions = categories.Select(c => new CategoryOption
            {
                Id = c.Id,
                Name = c.Name,
                DisplayNameVi = c.DisplayNameVi
            }).ToList();

            // Calculate stats
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

    // SEO-friendly URL: /hashtag/sanbaylongthanh
    [Route("hashtag/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        try
        {
            // Try to parse as ID first (backward compatibility)
            Models.Hashtag? hashtag = null;
            if (int.TryParse(slug, out int id))
            {
                hashtag = await _repository.GetHashtagByIdAsync(id);
            }

            // If not an ID, treat as tag slug
            if (hashtag == null)
            {
                // Remove # prefix if present
                var tag = slug.TrimStart('#').ToLower();
                hashtag = await _repository.GetHashtagByTagAsync(tag);
            }

            if (hashtag == null)
            {
                return NotFound();
            }

            var history = await _repository.GetHashtagHistoryAsync(hashtag.Id, days: 30);
            // var relatedHashtags = await _repository.GetRelatedHashtagsAsync(hashtag.Id, limit: 10); // DISABLED: Using category-based instead
            var relatedHashtags = await _repository.GetHashtagsByCategoryAsync(hashtag.CategoryId, hashtag.Id, limit: 10);
            var metrics = await _repository.GetLatestMetricsAsync(hashtag.Id);

            var viewModel = new HashtagDetailsViewModel
            {
                Hashtag = hashtag,
                History = history,
                RelatedHashtags = relatedHashtags,
                Metrics = metrics  // Include metrics with ViewCount and predictions
            };

            // Create SEO metadata
            var seoMetadata = CreateHashtagSeoMetadata(hashtag, metrics, slug);
            ViewData["SeoMetadata"] = seoMetadata;

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading hashtag details for slug '{Slug}': {Message}", slug, ex.Message);
            return NotFound();
        }
    }

    [Route("Hashtag/Search")]
    public async Task<IActionResult> Search(string q, int page = 1)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return RedirectToAction(nameof(Index));
            }

            // Search in database first
            var result = await _repository.SearchHashtagsAsync(q, page, pageSize: 20);

            var viewModel = new HashtagSearchViewModel
            {
                Query = q,
                Results = result,
                CurrentPage = page
            };

            // If no results found in database, try live search from TikTok
            if (result.TotalCount == 0)
            {
                _logger.LogInformation("No results in database for '{Query}', attempting live search", q);

                var liveResult = await _liveSearchService.SearchTikTokCreativeCenterAsync(q);

                if (liveResult != null && liveResult.IsAvailable)
                {
                    viewModel.LiveResult = liveResult;
                    _logger.LogInformation("Found live result for '{Query}': PostCount={PostCount}",
                        q, liveResult.PostCount);
                }
                else
                {
                    _logger.LogInformation("No live results found for '{Query}'", q);
                }
            }

            // Create SEO metadata for search results
            var seoMetadata = CreateSearchSeoMetadata(q, result.TotalCount);
            ViewData["SeoMetadata"] = seoMetadata;

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching hashtags for query '{Query}': {Message}", q, ex.Message);
            return View(new HashtagSearchViewModel { Query = q });
        }
    }

    /// <summary>
    /// Add a hashtag to tracking list (from live search results)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddToTracking(string tag, long? postCount)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                TempData["ErrorMessage"] = "Invalid hashtag";
                return RedirectToAction(nameof(Index));
            }

            var normalizedTag = tag.TrimStart('#').ToLower();

            // Check if already exists
            var existing = await _repository.GetHashtagByTagAsync(normalizedTag);
            if (existing != null)
            {
                TempData["InfoMessage"] = $"#{normalizedTag} is already being tracked!";
                return RedirectToAction(nameof(Details), new { slug = normalizedTag });
            }

            // Create new hashtag entry
            var hashtag = await _repository.GetOrCreateHashtagAsync(normalizedTag);

            // Set initial values
            hashtag.LatestPostCount = postCount;
            hashtag.IsActive = true;
            await _repository.UpdateHashtagAsync(hashtag);

            TempData["SuccessMessage"] = $"Added #{normalizedTag} to tracking list! It will be included in the next crawl.";

            _logger.LogInformation("Added hashtag #{Tag} to tracking from live search", normalizedTag);

            return RedirectToAction(nameof(Details), new { slug = normalizedTag });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding hashtag to tracking: {Message}", ex.Message);
            TempData["ErrorMessage"] = "Failed to add hashtag to tracking";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Hashtag Generator page - Public facing feature
    /// </summary>
    [Route("hashtag/tao-hashtag-ai")]
    public IActionResult Generator()
    {
        return View();
    }

    /// <summary>
    /// Generate hashtags via AJAX - API endpoint
    /// </summary>
    [HttpPost]
    [Route("Hashtag/Generate")]
    public async Task<IActionResult> Generate([FromBody] HashtagGeneratorRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Description))
            {
                return Json(new HashtagGeneratorResponse
                {
                    Success = false,
                    ErrorMessage = "Vui lòng nhập mô tả video."
                });
            }

            // Get user IP for rate limiting
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // TODO: Get userId from authentication when implemented
            int? userId = null;

            // Use V2 optimized approach (73% token reduction)
            var response = await _generatorService.GenerateHashtagsV2Async(
                request.Description,
                userId,
                ipAddress);

            // FALLBACK: Use V1 if needed (comment V2 above and uncomment below)
            // var response = await _generatorService.GenerateHashtagsAsync(
            //     request.Description,
            //     userId,
            //     ipAddress);

            // Log response details
            if (response.Success && response.Recommendation != null)
            {
                _logger.LogInformation($"Returning recommendation - Trending: {response.Recommendation.TrendingHashtags?.Count ?? 0}, Niche: {response.Recommendation.NicheHashtags?.Count ?? 0}, Ultra: {response.Recommendation.UltraNicheHashtags?.Count ?? 0}");

                // Debug log ExistsInDatabase values
                var firstHashtag = response.Recommendation.TrendingHashtags?.FirstOrDefault()
                    ?? response.Recommendation.NicheHashtags?.FirstOrDefault()
                    ?? response.Recommendation.UltraNicheHashtags?.FirstOrDefault();

                if (firstHashtag != null)
                {
                    _logger.LogInformation($"DEBUG: First hashtag '{firstHashtag.Tag}' - ExistsInDatabase: {firstHashtag.ExistsInDatabase}");
                }
            }
            else
            {
                _logger.LogWarning($"Response failed: {response.ErrorMessage}");
            }

            return Json(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Generate endpoint: {Message}", ex.Message);
            return Json(new HashtagGeneratorResponse
            {
                Success = false,
                ErrorMessage = "Đã xảy ra lỗi. Vui lòng thử lại sau."
            });
        }
    }

    /// <summary>
    /// Check remaining generations for current user/IP
    /// </summary>
    [HttpGet]
    [Route("Hashtag/CheckLimit")]
    public async Task<IActionResult> CheckLimit()
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            int? userId = null; // TODO: Get from authentication

            var (isAllowed, remaining) = await _generatorService.CheckRateLimitAsync(userId, ipAddress);

            return Json(new
            {
                success = true,
                isAllowed,
                remaining
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit: {Message}", ex.Message);
            return Json(new { success = false });
        }
    }

    #region SEO Helper Methods

    /// <summary>
    /// Create comprehensive SEO metadata for hashtag detail pages
    /// </summary>
    private SeoMetadata CreateHashtagSeoMetadata(Models.Hashtag hashtag, HashtagMetrics? metrics, string slug)
    {
        var viewCount = metrics?.ViewCount ?? hashtag.LatestViewCount ?? 0;
        var postCount = metrics?.PostCount ?? hashtag.LatestPostCount ?? 0;

        var viewCountFormatted = VietnameseHelper.FormatNumber(viewCount);
        var postCountFormatted = VietnameseHelper.FormatNumber(postCount);

        // Create title with metrics - optimized for CTR
        var currentYear = DateTime.Now.Year;
        var title = viewCount > 0
            ? $"{hashtag.TagDisplay} - {viewCountFormatted} Views | Trending TikTok {currentYear} [Cập Nhật]"
            : $"{hashtag.TagDisplay} | Hashtag TikTok Hot {currentYear}";

        // Create description - with CTA and value proposition
        var description = viewCount > 0 && postCount > 0
            ? $"🔥 {hashtag.TagDisplay} đang hot với {viewCountFormatted} views! Xem ngay: dữ liệu trending real-time, {postCountFormatted} bài đăng, hashtag liên quan hot nhất. Miễn phí 100%."
            : $"🔥 Khám phá {hashtag.TagDisplay} - hashtag TikTok đang viral. Xem dữ liệu trending, tips sử dụng, và hashtag liên quan hot nhất. Cập nhật real-time!";

        // Create OG title with metrics
        var ogTitle = viewCount > 0
            ? $"{hashtag.TagDisplay} 🔥 {viewCountFormatted} views | TikTok Trending {currentYear}"
            : $"{hashtag.TagDisplay} | Hashtag TikTok Trending {currentYear}";

        // Keywords
        var keywords = $"{hashtag.Tag}, hashtag {hashtag.Tag}, {hashtag.Tag} tiktok, trending hashtag, viral hashtag vietnam";
        if (!string.IsNullOrEmpty(hashtag.Category?.Name))
        {
            keywords += $", hashtag {hashtag.Category.Name.ToLower()}, {hashtag.Category.Name.ToLower()} tiktok";
        }

        // Canonical URL
        var baseUrl = "https://viralhashtag.vn"; // TODO: Get from configuration
        var canonicalSlug = VietnameseHelper.ToUrlSlug(hashtag.Tag);
        var canonicalUrl = $"{baseUrl}/hashtag/{canonicalSlug}";

        // Create structured data
        var structuredData = CreateHashtagStructuredData(hashtag, metrics, canonicalUrl);

        return new SeoMetadata
        {
            Title = title,
            Description = description,
            Keywords = keywords,
            CanonicalUrl = canonicalUrl,
            OgTitle = ogTitle,
            OgDescription = description,
            OgType = "article",
            PageType = "hashtag-detail",
            StructuredDataJson = structuredData
        };
    }

    /// <summary>
    /// Create SEO metadata for search results pages
    /// </summary>
    private SeoMetadata CreateSearchSeoMetadata(string query, int totalResults)
    {
        var cleanQuery = query.TrimStart('#');

        var title = totalResults > 0
            ? $"Kết Quả Tìm Kiếm '{cleanQuery}' | {totalResults} Hashtag TikTok"
            : $"Tìm Kiếm Hashtag '{cleanQuery}' TikTok Việt Nam";

        var description = totalResults > 0
            ? $"Tìm thấy {totalResults} hashtag liên quan đến '{cleanQuery}'. Xem dữ liệu trending, lượt xem, và phân tích chi tiết các hashtag TikTok tại Việt Nam."
            : $"Tìm kiếm hashtag '{cleanQuery}' TikTok. Khám phá các hashtag trending, viral, và phân tích dữ liệu real-time cho creator Việt Nam.";

        var keywords = $"{cleanQuery}, hashtag {cleanQuery}, {cleanQuery} tiktok, tìm kiếm hashtag tiktok, trending hashtag vietnam";

        return new SeoMetadata
        {
            Title = title,
            Description = description,
            Keywords = keywords,
            OgTitle = title,
            OgDescription = description,
            PageType = "search-results"
        };
    }

    /// <summary>
    /// Create Schema.org structured data for hashtag pages (Article + FAQ for rich snippets)
    /// </summary>
    private string CreateHashtagStructuredData(Models.Hashtag hashtag, HashtagMetrics? metrics, string url)
    {
        var viewCount = metrics?.ViewCount ?? hashtag.LatestViewCount ?? 0;
        var postCount = metrics?.PostCount ?? hashtag.LatestPostCount ?? 0;
        var viewCountFormatted = VietnameseHelper.FormatNumber(viewCount);
        var postCountFormatted = VietnameseHelper.FormatNumber(postCount);

        // Article schema
        var articleSchema = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Article",
            ["headline"] = $"{hashtag.TagDisplay} - Hashtag TikTok Trending",
            ["description"] = $"Dữ liệu trending, lượt xem, và hashtag liên quan cho {hashtag.TagDisplay}",
            ["inLanguage"] = "vi-VN",
            ["author"] = new Dictionary<string, object>
            {
                ["@type"] = "Organization",
                ["name"] = "TrendTag",
                ["url"] = "https://viralhashtag.vn"
            },
            ["publisher"] = new Dictionary<string, object>
            {
                ["@type"] = "Organization",
                ["name"] = "TrendTag",
                ["logo"] = new Dictionary<string, object>
                {
                    ["@type"] = "ImageObject",
                    ["url"] = "https://viralhashtag.vn/images/logo.png"
                }
            },
            ["datePublished"] = hashtag.FirstSeen.ToString("yyyy-MM-dd"),
            ["dateModified"] = DateTime.Now.ToString("yyyy-MM-dd"),
            ["url"] = url,
            ["mainEntityOfPage"] = new Dictionary<string, object>
            {
                ["@type"] = "WebPage",
                ["@id"] = url
            }
        };

        // FAQ schema for rich snippets
        var faqItems = new List<object>
        {
            new Dictionary<string, object>
            {
                ["@type"] = "Question",
                ["name"] = $"Hashtag {hashtag.TagDisplay} có bao nhiêu lượt xem trên TikTok?",
                ["acceptedAnswer"] = new Dictionary<string, object>
                {
                    ["@type"] = "Answer",
                    ["text"] = viewCount > 0
                        ? $"Hashtag {hashtag.TagDisplay} hiện có {viewCountFormatted} lượt xem trên TikTok, với {postCountFormatted} bài đăng sử dụng hashtag này. Dữ liệu được cập nhật real-time từ TikTok."
                        : $"Hashtag {hashtag.TagDisplay} đang được theo dõi trên TikTok. Dữ liệu lượt xem sẽ được cập nhật sớm."
                }
            },
            new Dictionary<string, object>
            {
                ["@type"] = "Question",
                ["name"] = $"Cách sử dụng hashtag {hashtag.TagDisplay} hiệu quả?",
                ["acceptedAnswer"] = new Dictionary<string, object>
                {
                    ["@type"] = "Answer",
                    ["text"] = $"Để sử dụng {hashtag.TagDisplay} hiệu quả: 1) Đặt hashtag trong 3-5 hashtag đầu tiên của video, 2) Kết hợp với các hashtag liên quan có cùng chủ đề, 3) Đăng video vào khung giờ cao điểm (19h-22h), 4) Tạo nội dung phù hợp với xu hướng của hashtag."
                }
            },
            new Dictionary<string, object>
            {
                ["@type"] = "Question",
                ["name"] = $"Hashtag {hashtag.TagDisplay} có đang trending không?",
                ["acceptedAnswer"] = new Dictionary<string, object>
                {
                    ["@type"] = "Answer",
                    ["text"] = viewCount > 1000000000
                        ? $"Có! {hashtag.TagDisplay} là hashtag rất hot với hơn {viewCountFormatted} views. Đây là thời điểm tốt để sử dụng hashtag này cho video TikTok của bạn."
                        : viewCount > 100000000
                            ? $"{hashtag.TagDisplay} đang trending với {viewCountFormatted} views. Hashtag này có tiềm năng giúp video của bạn tiếp cận nhiều người xem hơn."
                            : $"{hashtag.TagDisplay} là hashtag đang phát triển. Sử dụng hashtag này kết hợp với các hashtag phổ biến khác để tăng reach cho video."
                }
            }
        };

        var faqSchema = new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "FAQPage",
            ["mainEntity"] = faqItems
        };

        // Return array of schemas
        var schemas = new object[] { articleSchema, faqSchema };

        return JsonSerializer.Serialize(schemas, new JsonSerializerOptions
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    #endregion
}
