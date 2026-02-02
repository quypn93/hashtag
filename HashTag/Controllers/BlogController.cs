using HashTag.Models;
using HashTag.Repositories;
using HashTag.ViewModels;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace HashTag.Controllers;

public class BlogController : Controller
{
    private readonly IBlogRepository _blogRepository;
    private readonly ILogger<BlogController> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public BlogController(
        IBlogRepository blogRepository,
        ILogger<BlogController> logger,
        IServiceScopeFactory scopeFactory)
    {
        _blogRepository = blogRepository;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Check if current request is in English
    /// </summary>
    private bool IsEnglish
    {
        get
        {
            var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>();
            var currentCulture = requestCulture?.RequestCulture.UICulture.Name ?? "vi";
            return currentCulture.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Blog index page - List all published blog posts
    /// Route: /blog
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        try
        {
            const int pageSize = 9; // 3x3 grid
            page = Math.Max(1, page);

            var allPosts = await _blogRepository.GetPublishedPostsAsync(page, pageSize);
            var categories = await _blogRepository.GetActiveCategoriesAsync();
            var popularTags = await _blogRepository.GetPopularTagsAsync(15);
            var allRecentPosts = await _blogRepository.GetRecentPostsAsync(5);

            // Filter posts based on language: if English, only show posts with English translation
            IEnumerable<BlogPost> posts;
            IEnumerable<BlogPost> recentPosts;
            int totalPosts;

            if (IsEnglish)
            {
                posts = allPosts.Where(p => p.HasEnglishTranslation);
                recentPosts = allRecentPosts.Where(p => p.HasEnglishTranslation);
                // For accurate pagination, we need to count English posts only
                totalPosts = (await _blogRepository.GetPublishedPostsAsync(1, 1000))
                    .Count(p => p.HasEnglishTranslation);
            }
            else
            {
                posts = allPosts;
                recentPosts = allRecentPosts;
                totalPosts = await _blogRepository.GetTotalPublishedPostsCountAsync();
            }

            var viewModel = new BlogIndexViewModel
            {
                Posts = posts,
                Categories = categories,
                PopularTags = popularTags,
                RecentPosts = recentPosts,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPosts = totalPosts,
                IsEnglish = IsEnglish
            };

            // SEO metadata
            var seoMetadata = CreateBlogIndexSeoMetadata(page, totalPosts, IsEnglish);
            ViewData["SeoMetadata"] = seoMetadata;

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading blog index page: {Message}", ex.Message);
            return View(new BlogIndexViewModel());
        }
    }

    /// <summary>
    /// Single blog post details page
    /// Route: /blog/{slug}
    /// Supports both Vietnamese and English slugs
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(string slug)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                _logger.LogWarning("Blog Details: Empty slug provided");
                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation("Blog Details: Looking up post with slug '{Slug}'", slug);

            // Try to find by primary slug first
            var post = await _blogRepository.GetPostBySlugAsync(slug);

            // If not found and English, try to find by English slug
            if (post == null && IsEnglish)
            {
                post = await _blogRepository.GetPostByEnglishSlugAsync(slug);
            }

            if (post == null)
            {
                _logger.LogWarning("Blog Details: Post not found for slug '{Slug}'", slug);
                return NotFound();
            }

            if (!post.IsPublished)
            {
                _logger.LogWarning("Blog Details: Post '{Slug}' exists but is not published (Status: {Status}, PublishedAt: {PublishedAt})",
                    slug, post.Status, post.PublishedAt);
                return NotFound();
            }

            // If English region but no English translation, return 404
            if (IsEnglish && !post.HasEnglishTranslation)
            {
                _logger.LogWarning("Blog Details: Post '{Slug}' has no English translation, returning 404 for English users", slug);
                return NotFound();
            }

            _logger.LogInformation("Blog Details: Found post '{Title}' (ID: {Id}, Status: {Status})",
                post.GetLocalizedTitle(IsEnglish), post.Id, post.Status);

            // Save post ID for background increment
            var postId = post.Id;

            // Load related data first (same DbContext)
            var allRelatedPosts = await _blogRepository.GetRelatedPostsAsync(post.Id, 6);
            var allRecentPosts = await _blogRepository.GetRecentPostsAsync(10);

            // Filter by English availability if needed
            var relatedPosts = IsEnglish
                ? allRelatedPosts.Where(p => p.HasEnglishTranslation).Take(3)
                : allRelatedPosts.Take(3);
            var recentPosts = IsEnglish
                ? allRecentPosts.Where(p => p.HasEnglishTranslation).Take(5)
                : allRecentPosts.Take(5);

            // Increment view count in background after response is sent
            // Use IServiceScopeFactory to create new scope with fresh DbContext
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(100); // Small delay to ensure response is sent
                    using var scope = _scopeFactory.CreateScope();
                    var repository = scope.ServiceProvider.GetRequiredService<IBlogRepository>();
                    await repository.IncrementViewCountAsync(postId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to increment view count for post {PostId}", postId);
                }
            });

            var viewModel = new BlogDetailsViewModel
            {
                Post = post,
                RelatedPosts = relatedPosts,
                RecentPosts = recentPosts,
                IsEnglish = IsEnglish
            };

            // SEO metadata (localized)
            var seoMetadata = CreateBlogPostSeoMetadata(post, IsEnglish);
            ViewData["SeoMetadata"] = seoMetadata;
            viewModel.SeoMetadata = seoMetadata;

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading blog post '{Slug}': {Message}", slug, ex.Message);
            return NotFound();
        }
    }

    /// <summary>
    /// Blog posts by category
    /// Route: /blog/category/{slug}
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Category(string slug, int page = 1)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return RedirectToAction(nameof(Index));
            }

            var category = await _blogRepository.GetCategoryBySlugAsync(slug);

            if (category == null)
            {
                return NotFound();
            }

            const int pageSize = 9;
            page = Math.Max(1, page);

            var allPosts = await _blogRepository.GetPostsByCategoryAsync(slug, page, pageSize);
            var allCategories = await _blogRepository.GetActiveCategoriesAsync();
            var allRecentPosts = await _blogRepository.GetRecentPostsAsync(10);

            // Filter posts based on language: if English, only show posts with English translation
            IEnumerable<BlogPost> posts;
            IEnumerable<BlogPost> recentPosts;
            int totalPosts;

            if (IsEnglish)
            {
                posts = allPosts.Where(p => p.HasEnglishTranslation);
                recentPosts = allRecentPosts.Where(p => p.HasEnglishTranslation).Take(5);
                totalPosts = (await _blogRepository.GetPostsByCategoryAsync(slug, 1, 1000))
                    .Count(p => p.HasEnglishTranslation);
            }
            else
            {
                posts = allPosts;
                recentPosts = allRecentPosts.Take(5);
                totalPosts = await _blogRepository.GetPostCountByCategoryAsync(slug);
            }

            var viewModel = new BlogCategoryViewModel
            {
                Category = category,
                Posts = posts,
                AllCategories = allCategories,
                RecentPosts = recentPosts,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPosts = totalPosts,
                IsEnglish = IsEnglish
            };

            // SEO metadata
            var seoMetadata = CreateCategorySeoMetadata(category, page, totalPosts, IsEnglish);
            ViewData["SeoMetadata"] = seoMetadata;

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading blog category '{Slug}': {Message}", slug, ex.Message);
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Blog posts by tag
    /// Route: /blog/tag/{slug}
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Tag(string slug, int page = 1)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return RedirectToAction(nameof(Index));
            }

            var tag = await _blogRepository.GetTagBySlugAsync(slug);

            if (tag == null)
            {
                return NotFound();
            }

            const int pageSize = 9;
            page = Math.Max(1, page);

            var allPosts = await _blogRepository.GetPostsByTagAsync(slug, page, pageSize);
            var popularTags = await _blogRepository.GetPopularTagsAsync(15);
            var allRecentPosts = await _blogRepository.GetRecentPostsAsync(10);

            // Filter posts based on language: if English, only show posts with English translation
            IEnumerable<BlogPost> posts;
            IEnumerable<BlogPost> recentPosts;
            int totalPosts;

            if (IsEnglish)
            {
                posts = allPosts.Where(p => p.HasEnglishTranslation);
                recentPosts = allRecentPosts.Where(p => p.HasEnglishTranslation).Take(5);
                totalPosts = (await _blogRepository.GetPostsByTagAsync(slug, 1, 1000))
                    .Count(p => p.HasEnglishTranslation);
            }
            else
            {
                posts = allPosts;
                recentPosts = allRecentPosts.Take(5);
                totalPosts = await _blogRepository.GetPostCountByTagAsync(slug);
            }

            var viewModel = new BlogTagViewModel
            {
                Tag = tag,
                Posts = posts,
                PopularTags = popularTags,
                RecentPosts = recentPosts,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPosts = totalPosts,
                IsEnglish = IsEnglish
            };

            // SEO metadata
            var seoMetadata = CreateTagSeoMetadata(tag, page, totalPosts, IsEnglish);
            ViewData["SeoMetadata"] = seoMetadata;

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading blog tag '{Slug}': {Message}", slug, ex.Message);
            return RedirectToAction(nameof(Index));
        }
    }

    // ==================== SEO Metadata Generators ====================

    private SeoMetadata CreateBlogIndexSeoMetadata(int page, int totalPosts, bool isEnglish = false)
    {
        string title;
        string description;
        string keywords;

        if (isEnglish)
        {
            title = page == 1
                ? "TikTok Marketing Blog | Tips & Strategies to Boost TikTok Views - TrendTag"
                : $"TikTok Marketing Blog - Page {page} | TrendTag";
            description = "Discover tips, strategies and guides to effectively increase TikTok views. Blog updates on trending hashtags, viral video analysis and tips for TikTok creators.";
            keywords = "tiktok blog, tiktok tips, hashtag strategy, boost tiktok views, trending hashtag, tiktok marketing, viral video tips";
        }
        else
        {
            title = page == 1
                ? "Blog TikTok Marketing | Mẹo & Chiến Lược Tăng View TikTok - TrendTag"
                : $"Blog TikTok Marketing - Trang {page} | TrendTag";
            description = "Khám phá các mẹo, chiến lược và hướng dẫn tăng view TikTok hiệu quả. Blog cập nhật xu hướng hashtag trending, phân tích viral video và tips cho TikTok creator.";
            keywords = "blog tiktok, mẹo tiktok, chiến lược hashtag, tăng view tiktok, hashtag trending, tiktok marketing, viral video tips";
        }

        return new SeoMetadata
        {
            Title = title,
            Description = description,
            Keywords = keywords,
            CanonicalUrl = page == 1 ? "https://viralhashtag.vn/blog" : $"https://viralhashtag.vn/blog?page={page}",
            OgTitle = title,
            OgDescription = description,
            OgType = "website",
            PageType = "blog-index"
        };
    }

    private SeoMetadata CreateBlogPostSeoMetadata(BlogPost post, bool isEnglish = false)
    {
        var localizedTitle = post.GetLocalizedTitle(isEnglish);
        var localizedMetaTitle = post.GetLocalizedMetaTitle(isEnglish);
        var localizedMetaDescription = post.GetLocalizedMetaDescription(isEnglish);
        var localizedMetaKeywords = post.GetLocalizedMetaKeywords(isEnglish);
        var localizedExcerpt = post.GetLocalizedExcerpt(isEnglish);
        var localizedSlug = post.GetLocalizedSlug(isEnglish);

        var title = !string.IsNullOrEmpty(localizedMetaTitle)
            ? localizedMetaTitle
            : $"{localizedTitle} | Blog TrendTag";

        var description = !string.IsNullOrEmpty(localizedMetaDescription)
            ? localizedMetaDescription
            : localizedExcerpt ?? localizedTitle;

        var keywords = !string.IsNullOrEmpty(localizedMetaKeywords)
            ? localizedMetaKeywords
            : isEnglish
                ? $"{localizedTitle}, tiktok, hashtag trending, viral tips"
                : $"{localizedTitle}, tiktok, hashtag trending, viral tips";

        var canonicalUrl = $"https://viralhashtag.vn/blog/{localizedSlug}";

        // Create Article structured data
        var structuredData = CreateArticleStructuredData(post, canonicalUrl, isEnglish);

        return new SeoMetadata
        {
            Title = title,
            Description = description,
            Keywords = keywords,
            CanonicalUrl = canonicalUrl,
            OgTitle = title,
            OgDescription = description,
            OgType = "article",
            OgImage = post.FeaturedImage,
            PageType = "blog-post",
            StructuredDataJson = structuredData
        };
    }

    private SeoMetadata CreateCategorySeoMetadata(BlogCategory category, int page, int totalPosts, bool isEnglish = false)
    {
        var categoryName = category.GetLocalizedName(isEnglish);
        var categoryDescription = category.GetLocalizedDescription(isEnglish);

        string title;
        string description;
        string keywords;

        if (isEnglish)
        {
            title = page == 1
                ? $"{categoryName} Blog | TrendTag"
                : $"{categoryName} Blog - Page {page} | TrendTag";

            description = !string.IsNullOrEmpty(categoryDescription)
                ? categoryDescription
                : $"All articles about {categoryName}. Tips, strategies and detailed guides for TikTok creators.";

            keywords = $"{categoryName}, {categoryName} blog, tiktok {categoryName}";
        }
        else
        {
            title = page == 1
                ? $"Blog {categoryName} | TrendTag"
                : $"Blog {categoryName} - Trang {page} | TrendTag";

            description = !string.IsNullOrEmpty(categoryDescription)
                ? categoryDescription
                : $"Tất cả bài viết về {categoryName}. Mẹo, chiến lược và hướng dẫn chi tiết cho TikTok creator.";

            keywords = $"{categoryName}, blog {categoryName}, tiktok {categoryName}";
        }

        var canonicalUrl = page == 1
            ? $"https://viralhashtag.vn/blog/category/{category.Slug}"
            : $"https://viralhashtag.vn/blog/category/{category.Slug}?page={page}";

        return new SeoMetadata
        {
            Title = title,
            Description = description,
            Keywords = keywords,
            CanonicalUrl = canonicalUrl,
            OgTitle = title,
            OgDescription = description,
            OgType = "website",
            PageType = "blog-category"
        };
    }

    private SeoMetadata CreateTagSeoMetadata(BlogTag tag, int page, int totalPosts, bool isEnglish = false)
    {
        string title;
        string description;
        string keywords;

        if (isEnglish)
        {
            title = page == 1
                ? $"Posts About {tag.Name} | TrendTag Blog"
                : $"Posts About {tag.Name} - Page {page} | TrendTag Blog";

            description = $"All posts tagged with {tag.Name}. Discover tips, strategies and detailed guides.";

            keywords = $"{tag.Name}, {tag.Name} blog, tiktok {tag.Name}";
        }
        else
        {
            title = page == 1
                ? $"Bài Viết Về {tag.Name} | Blog TrendTag"
                : $"Bài Viết Về {tag.Name} - Trang {page} | Blog TrendTag";

            description = $"Tất cả bài viết được gắn tag {tag.Name}. Khám phá mẹo, chiến lược và hướng dẫn chi tiết.";

            keywords = $"{tag.Name}, blog {tag.Name}, tiktok {tag.Name}";
        }

        var canonicalUrl = page == 1
            ? $"https://viralhashtag.vn/blog/tag/{tag.Slug}"
            : $"https://viralhashtag.vn/blog/tag/{tag.Slug}?page={page}";

        return new SeoMetadata
        {
            Title = title,
            Description = description,
            Keywords = keywords,
            CanonicalUrl = canonicalUrl,
            OgTitle = title,
            OgDescription = description,
            OgType = "website",
            PageType = "blog-tag"
        };
    }

    private string CreateArticleStructuredData(BlogPost post, string canonicalUrl, bool isEnglish = false)
    {
        var localizedTitle = post.GetLocalizedTitle(isEnglish);
        var localizedExcerpt = post.GetLocalizedExcerpt(isEnglish);
        var localizedContent = post.GetLocalizedContent(isEnglish);

        var categoryName = post.Category != null
            ? (!string.IsNullOrEmpty(post.Category.DisplayNameVi) ? post.Category.DisplayNameVi : post.Category.Name)
            : "TikTok Tips";

        var publishedDate = post.PublishedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var modifiedDate = post.UpdatedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? publishedDate;

        var imageUrl = !string.IsNullOrEmpty(post.FeaturedImage)
            ? post.FeaturedImage
            : "https://viralhashtag.vn/images/blog-default.png";

        var categoryUrl = post.Category != null
            ? $"https://viralhashtag.vn/blog/category/{post.Category.Slug}"
            : "https://viralhashtag.vn/blog";

        var categoryDisplayName = post.Category != null
            ? (!string.IsNullOrEmpty(post.Category.DisplayNameVi) ? post.Category.DisplayNameVi : post.Category.Name)
            : "Blog";

        var homeName = isEnglish ? "Home" : "Trang chủ";

        // Combine Article schema with BreadcrumbList for better SEO
        var schemas = new List<string>();

        // Article Schema
        schemas.Add($@"{{
            ""@context"": ""https://schema.org"",
            ""@type"": ""Article"",
            ""headline"": ""{EscapeJson(localizedTitle)}"",
            ""image"": ""{imageUrl}"",
            ""datePublished"": ""{publishedDate}"",
            ""dateModified"": ""{modifiedDate}"",
            ""author"": {{
                ""@type"": ""Person"",
                ""name"": ""{EscapeJson(post.Author)}""
            }},
            ""publisher"": {{
                ""@type"": ""Organization"",
                ""name"": ""TrendTag"",
                ""logo"": {{
                    ""@type"": ""ImageObject"",
                    ""url"": ""https://viralhashtag.vn/images/logo.png""
                }}
            }},
            ""description"": ""{EscapeJson(localizedExcerpt ?? localizedTitle)}"",
            ""articleSection"": ""{EscapeJson(categoryName)}"",
            ""wordCount"": {localizedContent?.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length ?? 0},
            ""url"": ""{canonicalUrl}""
        }}");

        // BreadcrumbList Schema
        schemas.Add($@"{{
            ""@context"": ""https://schema.org"",
            ""@type"": ""BreadcrumbList"",
            ""itemListElement"": [
                {{
                    ""@type"": ""ListItem"",
                    ""position"": 1,
                    ""name"": ""{homeName}"",
                    ""item"": ""https://viralhashtag.vn""
                }},
                {{
                    ""@type"": ""ListItem"",
                    ""position"": 2,
                    ""name"": ""Blog"",
                    ""item"": ""https://viralhashtag.vn/blog""
                }},
                {{
                    ""@type"": ""ListItem"",
                    ""position"": 3,
                    ""name"": ""{EscapeJson(categoryDisplayName)}"",
                    ""item"": ""{categoryUrl}""
                }},
                {{
                    ""@type"": ""ListItem"",
                    ""position"": 4,
                    ""name"": ""{EscapeJson(localizedTitle)}""
                }}
            ]
        }}");

        // HowTo Schema - detect if post is a guide/tutorial
        if (IsHowToPost(localizedTitle))
        {
            var howToSchema = CreateHowToSchema(post, imageUrl, isEnglish);
            if (!string.IsNullOrEmpty(howToSchema))
            {
                schemas.Add(howToSchema);
            }
        }

        return "[" + string.Join(",", schemas) + "]";
    }

    /// <summary>
    /// Detect if a blog post is a how-to/guide based on title patterns
    /// </summary>
    private bool IsHowToPost(string title)
    {
        if (string.IsNullOrEmpty(title))
            return false;

        var howToPatterns = new[]
        {
            "cách ", "hướng dẫn ", "làm sao ", "làm thế nào ",
            "bước ", "tips ", "mẹo ", "bí quyết ",
            "how to ", "guide ", "tutorial "
        };

        var lowerTitle = title.ToLowerInvariant();
        return howToPatterns.Any(pattern => lowerTitle.Contains(pattern));
    }

    /// <summary>
    /// Create HowTo schema for tutorial/guide posts
    /// </summary>
    private string CreateHowToSchema(BlogPost post, string imageUrl, bool isEnglish = false)
    {
        var localizedTitle = post.GetLocalizedTitle(isEnglish);
        var localizedExcerpt = post.GetLocalizedExcerpt(isEnglish);
        var localizedContent = post.GetLocalizedContent(isEnglish);

        // Extract steps from content (look for numbered lists or h2/h3 headers)
        var steps = ExtractStepsFromContent(localizedContent ?? "");

        if (steps.Count < 2)
            return string.Empty; // Need at least 2 steps for HowTo schema

        var stepsJson = string.Join(",", steps.Select((step, index) => $@"
            {{
                ""@type"": ""HowToStep"",
                ""position"": {index + 1},
                ""name"": ""{EscapeJson(step.Name)}"",
                ""text"": ""{EscapeJson(step.Text)}""
            }}"));

        return $@"{{
            ""@context"": ""https://schema.org"",
            ""@type"": ""HowTo"",
            ""name"": ""{EscapeJson(localizedTitle)}"",
            ""description"": ""{EscapeJson(localizedExcerpt ?? localizedTitle)}"",
            ""image"": ""{imageUrl}"",
            ""step"": [{stepsJson}]
        }}";
    }

    /// <summary>
    /// Extract steps from blog content for HowTo schema
    /// Looks for patterns like "Bước 1:", "1.", numbered lists, or headers
    /// </summary>
    private List<(string Name, string Text)> ExtractStepsFromContent(string content)
    {
        var steps = new List<(string Name, string Text)>();

        if (string.IsNullOrEmpty(content))
            return steps;

        // Remove HTML tags for analysis
        var plainText = System.Text.RegularExpressions.Regex.Replace(content, "<[^>]+>", " ");

        // Pattern 1: "Bước X:" or "Step X:"
        var stepPattern = new System.Text.RegularExpressions.Regex(
            @"(?:Bước|Step)\s*(\d+)\s*[:\.]?\s*([^\n]+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        var matches = stepPattern.Matches(plainText);
        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            var stepName = $"Bước {match.Groups[1].Value}";
            var stepText = match.Groups[2].Value.Trim();
            if (stepText.Length > 10) // Minimum meaningful content
            {
                steps.Add((stepName, stepText.Length > 200 ? stepText.Substring(0, 200) + "..." : stepText));
            }
        }

        // Pattern 2: Numbered lists "1.", "2.", etc.
        if (steps.Count < 2)
        {
            var numberedPattern = new System.Text.RegularExpressions.Regex(
                @"(?:^|\n)\s*(\d+)\.\s+([^\n]+)",
                System.Text.RegularExpressions.RegexOptions.Multiline);

            matches = numberedPattern.Matches(plainText);
            steps.Clear();
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var stepName = $"Bước {match.Groups[1].Value}";
                var stepText = match.Groups[2].Value.Trim();
                if (stepText.Length > 10)
                {
                    steps.Add((stepName, stepText.Length > 200 ? stepText.Substring(0, 200) + "..." : stepText));
                }
            }
        }

        return steps.Take(10).ToList(); // Max 10 steps for schema
    }

    private string EscapeJson(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    /// <summary>
    /// DEBUG ENDPOINT: Check all blog posts in database
    /// Route: /blog/debug/list-all
    /// </summary>
    [HttpGet("debug/list-all")]
    public async Task<IActionResult> DebugListAll()
    {
        try
        {
            var allPosts = await _blogRepository.GetPublishedPostsAsync(1, 100);
            var result = allPosts.Select(p => new
            {
                p.Id,
                p.Title,
                p.Slug,
                p.Status,
                p.PublishedAt,
                IsPublished = p.IsPublished,
                p.CreatedAt,
                CategoryName = p.Category?.Name
            }).ToList();

            _logger.LogInformation("DEBUG: Found {Count} blog posts in database", result.Count);

            return Json(new
            {
                TotalPosts = result.Count,
                Posts = result,
                CurrentTime = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DEBUG: Error listing all posts");
            return Json(new { Error = ex.Message });
        }
    }
}
