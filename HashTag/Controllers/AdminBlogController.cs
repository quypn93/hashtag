using HashTag.Data;
using HashTag.Filters;
using HashTag.Helpers;
using HashTag.Models;
using HashTag.Services;
using HashTag.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HashTag.Controllers;

[Route("admin/blog")]
[ServiceFilter(typeof(AdminAuthFilter))]
public class AdminBlogController : Controller
{
    private readonly TrendTagDbContext _context;
    private readonly IBlogAutoGeneratorService _blogGenerator;
    private readonly ILogger<AdminBlogController> _logger;

    public AdminBlogController(
        TrendTagDbContext context,
        IBlogAutoGeneratorService blogGenerator,
        ILogger<AdminBlogController> logger)
    {
        _context = context;
        _blogGenerator = blogGenerator;
        _logger = logger;
    }

    // GET: /admin/blog
    [HttpGet("")]
    public async Task<IActionResult> Index(
        int page = 1,
        string? status = null,
        int? categoryId = null,
        string? q = null)
    {
        const int pageSize = 20;

        var query = _context.BlogPosts
            .Include(p => p.Category)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(p => p.Status == status);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId);
        }

        if (!string.IsNullOrEmpty(q))
        {
            query = query.Where(p => p.Title.Contains(q) || p.Slug.Contains(q));
        }

        var totalPosts = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalPosts / (double)pageSize);

        var posts = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        var categories = await _context.BlogCategories
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync();

        var viewModel = new AdminBlogListViewModel
        {
            Posts = posts,
            Categories = categories,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalPosts = totalPosts,
            StatusFilter = status,
            CategoryFilter = categoryId,
            SearchQuery = q
        };

        return View(viewModel);
    }

    // GET: /admin/blog/create
    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        var categories = await _context.BlogCategories
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync();

        var viewModel = new BlogPostEditViewModel
        {
            Categories = categories,
            Status = "Draft",
            Author = "TrendTag Team"
        };

        return View(viewModel);
    }

    // POST: /admin/blog/create
    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BlogPostEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Categories = await _context.BlogCategories.OrderBy(c => c.Name).ToListAsync();
            return View(model);
        }

        // Check slug uniqueness
        var slugExists = await _context.BlogPosts.AnyAsync(p => p.Slug == model.Slug);
        if (slugExists)
        {
            ModelState.AddModelError("Slug", "Slug này đã tồn tại");
            model.Categories = await _context.BlogCategories.OrderBy(c => c.Name).ToListAsync();
            return View(model);
        }

        var post = new BlogPost
        {
            Title = model.Title,
            Slug = model.Slug,
            Excerpt = model.Excerpt,
            Content = model.Content,
            FeaturedImage = model.FeaturedImage,
            MetaTitle = model.MetaTitle,
            MetaDescription = model.MetaDescription,
            MetaKeywords = model.MetaKeywords,
            Author = model.Author,
            CategoryId = model.CategoryId,
            Status = model.Status,
            PublishedAt = model.Status == "Published" ? (model.PublishedAt ?? DateTime.UtcNow) : model.PublishedAt,
            CreatedAt = DateTime.UtcNow
        };

        _context.BlogPosts.Add(post);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created blog post: {Title} (ID: {Id})", post.Title, post.Id);
        TempData["Success"] = "Đã tạo bài viết thành công!";

        return RedirectToAction(nameof(Index));
    }

    // GET: /admin/blog/edit/{id}
    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var post = await _context.BlogPosts.FindAsync(id);
        if (post == null)
        {
            return NotFound();
        }

        var categories = await _context.BlogCategories.OrderBy(c => c.Name).ToListAsync();
        var viewModel = BlogPostEditViewModel.FromPost(post);
        viewModel.Categories = categories;

        return View(viewModel);
    }

    // POST: /admin/blog/edit/{id}
    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BlogPostEditViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            model.Categories = await _context.BlogCategories.OrderBy(c => c.Name).ToListAsync();
            return View(model);
        }

        var post = await _context.BlogPosts.FindAsync(id);
        if (post == null)
        {
            return NotFound();
        }

        // Check slug uniqueness (excluding current post)
        var slugExists = await _context.BlogPosts.AnyAsync(p => p.Slug == model.Slug && p.Id != id);
        if (slugExists)
        {
            ModelState.AddModelError("Slug", "Slug này đã tồn tại");
            model.Categories = await _context.BlogCategories.OrderBy(c => c.Name).ToListAsync();
            return View(model);
        }

        // Auto-set PublishedAt when publishing
        if (model.Status == "Published" && post.Status != "Published")
        {
            model.PublishedAt ??= DateTime.UtcNow;
        }

        model.ApplyTo(post);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated blog post: {Title} (ID: {Id})", post.Title, post.Id);
        TempData["Success"] = "Đã cập nhật bài viết!";

        return RedirectToAction(nameof(Index));
    }

    // POST: /admin/blog/delete/{id}
    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var post = await _context.BlogPosts.FindAsync(id);
        if (post == null)
        {
            return NotFound();
        }

        _context.BlogPosts.Remove(post);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted blog post: {Title} (ID: {Id})", post.Title, id);
        TempData["Success"] = "Đã xóa bài viết!";

        return RedirectToAction(nameof(Index));
    }

    // POST: /admin/blog/toggle-publish/{id}
    [HttpPost("toggle-publish/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePublish(int id)
    {
        var post = await _context.BlogPosts.FindAsync(id);
        if (post == null)
        {
            return NotFound();
        }

        if (post.Status == "Published")
        {
            post.Status = "Draft";
            post.PublishedAt = null;
            TempData["Success"] = "Đã chuyển bài viết về bản nháp!";
        }
        else
        {
            post.Status = "Published";
            post.PublishedAt = DateTime.UtcNow;
            TempData["Success"] = "Đã xuất bản bài viết!";
        }

        post.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Toggled publish status for post: {Title} -> {Status}", post.Title, post.Status);

        return RedirectToAction(nameof(Index));
    }

    // GET: /admin/blog/generate
    [HttpGet("generate")]
    public async Task<IActionResult> Generate()
    {
        var blogCategories = await _context.BlogCategories.OrderBy(c => c.Name).ToListAsync();
        var hashtagCategories = await _context.HashtagCategories.OrderBy(c => c.Name).ToListAsync();

        var viewModel = new BlogGenerateViewModel
        {
            Categories = blogCategories,
            HashtagCategories = hashtagCategories
        };

        return View(viewModel);
    }

    // POST: /admin/blog/generate
    [HttpPost("generate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(GenerateContentRequest request)
    {
        try
        {
            BlogPost? generatedPost = null;

            switch (request.Type.ToLower())
            {
                case "monthly":
                    var month = request.Month ?? DateTime.Now.AddMonths(-1).Month;
                    var year = request.Year ?? DateTime.Now.Year;
                    if (month == 0) { month = 12; year--; }
                    generatedPost = await _blogGenerator.GenerateMonthlyTopHashtagsAsync(month, year);
                    break;

                case "weekly":
                    generatedPost = await _blogGenerator.GenerateWeeklyTrendingReportAsync();
                    break;

                case "category":
                    if (!request.CategoryId.HasValue)
                    {
                        TempData["Error"] = "Vui lòng chọn chủ đề hashtag!";
                        return RedirectToAction(nameof(Generate));
                    }
                    generatedPost = await _blogGenerator.GenerateCategoryTopHashtagsAsync(request.CategoryId.Value);
                    break;

                default:
                    TempData["Error"] = "Loại bài viết không hợp lệ!";
                    return RedirectToAction(nameof(Generate));
            }

            if (generatedPost != null)
            {
                TempData["Success"] = $"Đã tạo bài viết: {generatedPost.Title}";
                return RedirectToAction(nameof(Edit), new { id = generatedPost.Id });
            }
            else
            {
                TempData["Error"] = "Không thể tạo bài viết. Có thể không có đủ dữ liệu hashtag.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating blog content");
            TempData["Error"] = $"Lỗi: {ex.Message}";
        }

        return RedirectToAction(nameof(Generate));
    }

    // API: Generate slug from title
    [HttpGet("api/generate-slug")]
    public IActionResult GenerateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Json(new { slug = "" });
        }

        var slug = VietnameseHelper.ToUrlSlug(title);
        return Json(new { slug });
    }
}
