using HashTag.Models;

namespace HashTag.Repositories;

public interface IBlogRepository
{
    // Blog Posts
    Task<IEnumerable<BlogPost>> GetPublishedPostsAsync(int pageNumber = 1, int pageSize = 10);
    Task<IEnumerable<BlogPost>> GetPostsByCategoryAsync(string categorySlug, int pageNumber = 1, int pageSize = 10);
    Task<IEnumerable<BlogPost>> GetPostsByTagAsync(string tagSlug, int pageNumber = 1, int pageSize = 10);
    Task<BlogPost?> GetPostBySlugAsync(string slug);
    Task<BlogPost?> GetPostByIdAsync(int id);
    Task<int> GetTotalPublishedPostsCountAsync();
    Task<int> GetPostCountByCategoryAsync(string categorySlug);
    Task<int> GetPostCountByTagAsync(string tagSlug);
    Task IncrementViewCountAsync(int postId);

    // Related Posts
    Task<IEnumerable<BlogPost>> GetRelatedPostsAsync(int postId, int count = 3);

    // Popular Posts
    Task<IEnumerable<BlogPost>> GetPopularPostsAsync(int count = 5);

    // Recent Posts
    Task<IEnumerable<BlogPost>> GetRecentPostsAsync(int count = 5);

    // Categories
    Task<IEnumerable<BlogCategory>> GetActiveCategoriesAsync();
    Task<BlogCategory?> GetCategoryBySlugAsync(string slug);
    Task<BlogCategory?> GetCategoryByIdAsync(int id);

    // Tags
    Task<IEnumerable<BlogTag>> GetAllTagsAsync();
    Task<IEnumerable<BlogTag>> GetPopularTagsAsync(int count = 20);
    Task<BlogTag?> GetTagBySlugAsync(string slug);
    Task<BlogTag?> GetTagByIdAsync(int id);
}
