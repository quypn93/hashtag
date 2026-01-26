using HashTag.Models;
using HashTag.Repositories;
using HashTag.ViewModels;
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

            var posts = await _blogRepository.GetPublishedPostsAsync(page, pageSize);
            var totalPosts = await _blogRepository.GetTotalPublishedPostsCountAsync();
            var categories = await _blogRepository.GetActiveCategoriesAsync();
            var popularTags = await _blogRepository.GetPopularTagsAsync(15);
            var recentPosts = await _blogRepository.GetRecentPostsAsync(5);

            var viewModel = new BlogIndexViewModel
            {
                Posts = posts,
                Categories = categories,
                PopularTags = popularTags,
                RecentPosts = recentPosts,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPosts = totalPosts
            };

            // SEO metadata
            var seoMetadata = CreateBlogIndexSeoMetadata(page, totalPosts);
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

            var post = await _blogRepository.GetPostBySlugAsync(slug);

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

            _logger.LogInformation("Blog Details: Found post '{Title}' (ID: {Id}, Status: {Status})",
                post.Title, post.Id, post.Status);

            // Save post ID for background increment
            var postId = post.Id;

            // Load related data first (same DbContext)
            var relatedPosts = await _blogRepository.GetRelatedPostsAsync(post.Id, 3);
            var recentPosts = await _blogRepository.GetRecentPostsAsync(5);

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
                RecentPosts = recentPosts
            };

            // SEO metadata
            var seoMetadata = CreateBlogPostSeoMetadata(post);
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

            var posts = await _blogRepository.GetPostsByCategoryAsync(slug, page, pageSize);
            var totalPosts = await _blogRepository.GetPostCountByCategoryAsync(slug);
            var allCategories = await _blogRepository.GetActiveCategoriesAsync();
            var recentPosts = await _blogRepository.GetRecentPostsAsync(5);

            var viewModel = new BlogCategoryViewModel
            {
                Category = category,
                Posts = posts,
                AllCategories = allCategories,
                RecentPosts = recentPosts,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPosts = totalPosts
            };

            // SEO metadata
            var seoMetadata = CreateCategorySeoMetadata(category, page, totalPosts);
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

            var posts = await _blogRepository.GetPostsByTagAsync(slug, page, pageSize);
            var totalPosts = await _blogRepository.GetPostCountByTagAsync(slug);
            var popularTags = await _blogRepository.GetPopularTagsAsync(15);
            var recentPosts = await _blogRepository.GetRecentPostsAsync(5);

            var viewModel = new BlogTagViewModel
            {
                Tag = tag,
                Posts = posts,
                PopularTags = popularTags,
                RecentPosts = recentPosts,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPosts = totalPosts
            };

            // SEO metadata
            var seoMetadata = CreateTagSeoMetadata(tag, page, totalPosts);
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

    private SeoMetadata CreateBlogIndexSeoMetadata(int page, int totalPosts)
    {
        var title = page == 1
            ? "Blog TikTok Marketing | Mẹo & Chiến Lược Tăng View TikTok - TrendTag"
            : $"Blog TikTok Marketing - Trang {page} | TrendTag";

        var description = "Khám phá các mẹo, chiến lược và hướng dẫn tăng view TikTok hiệu quả. Blog cập nhật xu hướng hashtag trending, phân tích viral video và tips cho TikTok creator.";

        var keywords = "blog tiktok, mẹo tiktok, chiến lược hashtag, tăng view tiktok, hashtag trending, tiktok marketing, viral video tips";

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

    private SeoMetadata CreateBlogPostSeoMetadata(BlogPost post)
    {
        var title = !string.IsNullOrEmpty(post.MetaTitle)
            ? post.MetaTitle
            : $"{post.Title} | Blog TrendTag";

        var description = !string.IsNullOrEmpty(post.MetaDescription)
            ? post.MetaDescription
            : post.Excerpt ?? post.Title;

        var keywords = !string.IsNullOrEmpty(post.MetaKeywords)
            ? post.MetaKeywords
            : $"{post.Title}, tiktok, hashtag trending, viral tips";

        var canonicalUrl = $"https://viralhashtag.vn/blog/{post.Slug}";

        // Create Article structured data
        var structuredData = CreateArticleStructuredData(post, canonicalUrl);

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

    private SeoMetadata CreateCategorySeoMetadata(BlogCategory category, int page, int totalPosts)
    {
        var categoryName = !string.IsNullOrEmpty(category.DisplayNameVi)
            ? category.DisplayNameVi
            : category.Name;

        var title = page == 1
            ? $"Blog {categoryName} | TrendTag"
            : $"Blog {categoryName} - Trang {page} | TrendTag";

        var description = !string.IsNullOrEmpty(category.Description)
            ? category.Description
            : $"Tất cả bài viết về {categoryName}. Mẹo, chiến lược và hướng dẫn chi tiết cho TikTok creator.";

        var keywords = $"{categoryName}, blog {categoryName}, tiktok {categoryName}";

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

    private SeoMetadata CreateTagSeoMetadata(BlogTag tag, int page, int totalPosts)
    {
        var title = page == 1
            ? $"Bài Viết Về {tag.Name} | Blog TrendTag"
            : $"Bài Viết Về {tag.Name} - Trang {page} | Blog TrendTag";

        var description = $"Tất cả bài viết được gắn tag {tag.Name}. Khám phá mẹo, chiến lược và hướng dẫn chi tiết.";

        var keywords = $"{tag.Name}, blog {tag.Name}, tiktok {tag.Name}";

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

    private string CreateArticleStructuredData(BlogPost post, string canonicalUrl)
    {
        var categoryName = post.Category != null
            ? (!string.IsNullOrEmpty(post.Category.DisplayNameVi) ? post.Category.DisplayNameVi : post.Category.Name)
            : "TikTok Tips";

        var publishedDate = post.PublishedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var modifiedDate = post.UpdatedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? publishedDate;

        var imageUrl = !string.IsNullOrEmpty(post.FeaturedImage)
            ? post.FeaturedImage
            : "https://viralhashtag.vn/images/blog-default.png";

        return $@"{{
            ""@context"": ""https://schema.org"",
            ""@type"": ""Article"",
            ""headline"": ""{EscapeJson(post.Title)}"",
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
            ""description"": ""{EscapeJson(post.Excerpt ?? post.Title)}"",
            ""articleSection"": ""{EscapeJson(categoryName)}"",
            ""wordCount"": {post.Content?.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length ?? 0},
            ""url"": ""{canonicalUrl}""
        }}";
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
