using HashTag.Models;
using HashTag.Repositories;

namespace HashTag.ViewModels;

public class HomeIndexViewModel
{
    public List<TrendingHashtagDto> TopHashtags { get; set; } = new();
    public List<CategoryOption> Categories { get; set; } = new();
    public int? SelectedCategoryId { get; set; }
    public List<BlogPost> RecentBlogPosts { get; set; } = new();
}
