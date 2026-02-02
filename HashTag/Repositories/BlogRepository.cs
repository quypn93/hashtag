using HashTag.Data;
using HashTag.Models;
using HashTag.Services;
using Microsoft.EntityFrameworkCore;

namespace HashTag.Repositories;

public class BlogRepository : IBlogRepository
{
    private readonly TrendTagDbContext _context;
    private readonly ILogger<BlogRepository> _logger;
    private readonly IStoredProcedureService _spService;

    public BlogRepository(
        TrendTagDbContext context,
        ILogger<BlogRepository> logger,
        IStoredProcedureService spService)
    {
        _context = context;
        _logger = logger;
        _spService = spService;
    }

    // ==================== Blog Posts ====================

    public async Task<IEnumerable<BlogPost>> GetPublishedPostsAsync(int pageNumber = 1, int pageSize = 10)
    {
        return await _context.BlogPosts
            .Where(p => p.Status == "Published" && p.PublishedAt <= DateTime.UtcNow)
            .Include(p => p.Category)
            .Include(p => p.BlogPostTags)
                .ThenInclude(pt => pt.BlogTag)
            .OrderByDescending(p => p.PublishedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<BlogPost>> GetPostsByCategoryAsync(string categorySlug, int pageNumber = 1, int pageSize = 10)
    {
        return await _context.BlogPosts
            .Where(p => p.Status == "Published"
                     && p.PublishedAt <= DateTime.UtcNow
                     && p.Category != null
                     && p.Category.Slug == categorySlug)
            .Include(p => p.Category)
            .Include(p => p.BlogPostTags)
                .ThenInclude(pt => pt.BlogTag)
            .OrderByDescending(p => p.PublishedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<BlogPost>> GetPostsByTagAsync(string tagSlug, int pageNumber = 1, int pageSize = 10)
    {
        return await _context.BlogPosts
            .Where(p => p.Status == "Published"
                     && p.PublishedAt <= DateTime.UtcNow
                     && p.BlogPostTags.Any(pt => pt.BlogTag.Slug == tagSlug))
            .Include(p => p.Category)
            .Include(p => p.BlogPostTags)
                .ThenInclude(pt => pt.BlogTag)
            .OrderByDescending(p => p.PublishedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<BlogPost?> GetPostBySlugAsync(string slug)
    {
        return await _context.BlogPosts
            .Where(p => p.Slug == slug)
            .Include(p => p.Category)
            .Include(p => p.BlogPostTags)
                .ThenInclude(pt => pt.BlogTag)
            .FirstOrDefaultAsync();
    }

    public async Task<BlogPost?> GetPostByEnglishSlugAsync(string slugEn)
    {
        return await _context.BlogPosts
            .Where(p => p.SlugEn == slugEn)
            .Include(p => p.Category)
            .Include(p => p.BlogPostTags)
                .ThenInclude(pt => pt.BlogTag)
            .FirstOrDefaultAsync();
    }

    public async Task<BlogPost?> GetPostByIdAsync(int id)
    {
        return await _context.BlogPosts
            .Where(p => p.Id == id)
            .Include(p => p.Category)
            .Include(p => p.BlogPostTags)
                .ThenInclude(pt => pt.BlogTag)
            .FirstOrDefaultAsync();
    }

    public async Task<int> GetTotalPublishedPostsCountAsync()
    {
        return await _context.BlogPosts
            .Where(p => p.Status == "Published" && p.PublishedAt <= DateTime.UtcNow)
            .CountAsync();
    }

    public async Task<int> GetPostCountByCategoryAsync(string categorySlug)
    {
        return await _context.BlogPosts
            .Where(p => p.Status == "Published"
                     && p.PublishedAt <= DateTime.UtcNow
                     && p.Category != null
                     && p.Category.Slug == categorySlug)
            .CountAsync();
    }

    public async Task<int> GetPostCountByTagAsync(string tagSlug)
    {
        return await _context.BlogPosts
            .Where(p => p.Status == "Published"
                     && p.PublishedAt <= DateTime.UtcNow
                     && p.BlogPostTags.Any(pt => pt.BlogTag.Slug == tagSlug))
            .CountAsync();
    }

    public async Task IncrementViewCountAsync(int postId)
    {
        var post = await _context.BlogPosts.FindAsync(postId);
        if (post != null)
        {
            post.ViewCount++;
            await _context.SaveChangesAsync();
        }
    }

    // ==================== Related & Popular Posts ====================

    public async Task<IEnumerable<BlogPost>> GetRelatedPostsAsync(int postId, int count = 3)
    {
        var post = await _context.BlogPosts
            .Include(p => p.BlogPostTags)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null)
            return Enumerable.Empty<BlogPost>();

        // Get posts with same category or tags
        var tagIds = post.BlogPostTags.Select(pt => pt.BlogTagId).ToList();

        return await _context.BlogPosts
            .Where(p => p.Id != postId
                     && p.Status == "Published"
                     && p.PublishedAt <= DateTime.UtcNow
                     && (p.CategoryId == post.CategoryId
                         || p.BlogPostTags.Any(pt => tagIds.Contains(pt.BlogTagId))))
            .Include(p => p.Category)
            .Include(p => p.BlogPostTags)
                .ThenInclude(pt => pt.BlogTag)
            .OrderByDescending(p => p.PublishedAt)
            .Take(count)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<BlogPost>> GetPopularPostsAsync(int count = 5)
    {
        return await _context.BlogPosts
            .Where(p => p.Status == "Published" && p.PublishedAt <= DateTime.UtcNow)
            .Include(p => p.Category)
            .OrderByDescending(p => p.ViewCount)
            .ThenByDescending(p => p.PublishedAt)
            .Take(count)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<BlogPost>> GetRecentPostsAsync(int count = 5)
    {
        try
        {
            // ⚡ PERFORMANCE BOOST: Use ADO.NET stored procedure instead of EF Core
            _logger.LogDebug("Calling stored procedure sp_GetRecentBlogPosts via ADO.NET");
            return await _spService.GetRecentBlogPostsAsync(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling stored procedure, falling back to EF Core: {Message}", ex.Message);

            // FALLBACK: Use original EF Core implementation
            return await _context.BlogPosts
                .Where(p => p.Status == "Published" && p.PublishedAt <= DateTime.UtcNow)
                .Include(p => p.Category)
                .OrderByDescending(p => p.PublishedAt)
                .Take(count)
                .AsNoTracking()
                .ToListAsync();
        }
    }

    // ==================== Categories ====================

    public async Task<IEnumerable<BlogCategory>> GetActiveCategoriesAsync()
    {
        return await _context.BlogCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<BlogCategory?> GetCategoryBySlugAsync(string slug)
    {
        return await _context.BlogCategories
            .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive);
    }

    public async Task<BlogCategory?> GetCategoryByIdAsync(int id)
    {
        return await _context.BlogCategories
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    // ==================== Tags ====================

    public async Task<IEnumerable<BlogTag>> GetAllTagsAsync()
    {
        return await _context.BlogTags
            .OrderBy(t => t.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<BlogTag>> GetPopularTagsAsync(int count = 20)
    {
        return await _context.BlogTags
            .Where(t => t.BlogPostTags.Any(pt => pt.BlogPost.Status == "Published"))
            .OrderByDescending(t => t.BlogPostTags.Count)
            .Take(count)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<BlogTag?> GetTagBySlugAsync(string slug)
    {
        return await _context.BlogTags
            .FirstOrDefaultAsync(t => t.Slug == slug);
    }

    public async Task<BlogTag?> GetTagByIdAsync(int id)
    {
        return await _context.BlogTags
            .FirstOrDefaultAsync(t => t.Id == id);
    }
}
