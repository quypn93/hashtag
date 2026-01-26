using HashTag.Models;

namespace HashTag.ViewModels;

/// <summary>
/// ViewModel for blog index/listing page
/// </summary>
public class BlogIndexViewModel
{
    public IEnumerable<BlogPost> Posts { get; set; } = new List<BlogPost>();
    public IEnumerable<BlogCategory> Categories { get; set; } = new List<BlogCategory>();
    public IEnumerable<BlogTag> PopularTags { get; set; } = new List<BlogTag>();
    public IEnumerable<BlogPost> RecentPosts { get; set; } = new List<BlogPost>();

    // Pagination
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPosts { get; set; }

    public int TotalPages => (int)Math.Ceiling((double)TotalPosts / PageSize);
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    // Filter info
    public string? FilterType { get; set; } // null, "category", "tag"
    public string? FilterSlug { get; set; }
    public string? FilterName { get; set; }
}

/// <summary>
/// ViewModel for single blog post details page
/// </summary>
public class BlogDetailsViewModel
{
    public required BlogPost Post { get; set; }
    public IEnumerable<BlogPost> RelatedPosts { get; set; } = new List<BlogPost>();
    public IEnumerable<BlogPost> RecentPosts { get; set; } = new List<BlogPost>();
    public SeoMetadata? SeoMetadata { get; set; }
}

/// <summary>
/// ViewModel for blog category page
/// </summary>
public class BlogCategoryViewModel
{
    public required BlogCategory Category { get; set; }
    public IEnumerable<BlogPost> Posts { get; set; } = new List<BlogPost>();
    public IEnumerable<BlogCategory> AllCategories { get; set; } = new List<BlogCategory>();
    public IEnumerable<BlogPost> RecentPosts { get; set; } = new List<BlogPost>();

    // Pagination
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPosts { get; set; }

    public int TotalPages => (int)Math.Ceiling((double)TotalPosts / PageSize);
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}

/// <summary>
/// ViewModel for blog tag page
/// </summary>
public class BlogTagViewModel
{
    public required BlogTag Tag { get; set; }
    public IEnumerable<BlogPost> Posts { get; set; } = new List<BlogPost>();
    public IEnumerable<BlogTag> PopularTags { get; set; } = new List<BlogTag>();
    public IEnumerable<BlogPost> RecentPosts { get; set; } = new List<BlogPost>();

    // Pagination
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPosts { get; set; }

    public int TotalPages => (int)Math.Ceiling((double)TotalPosts / PageSize);
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}
