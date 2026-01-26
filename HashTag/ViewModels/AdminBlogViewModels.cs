using System.ComponentModel.DataAnnotations;
using HashTag.Models;

namespace HashTag.ViewModels;

public class AdminBlogListViewModel
{
    public List<BlogPost> Posts { get; set; } = new();
    public List<BlogCategory> Categories { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalPosts { get; set; }
    public string? StatusFilter { get; set; }
    public int? CategoryFilter { get; set; }
    public string? SearchQuery { get; set; }
}

public class BlogPostEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [MaxLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự")]
    [Display(Name = "Tiêu đề")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Slug không được để trống")]
    [MaxLength(250, ErrorMessage = "Slug tối đa 250 ký tự")]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", ErrorMessage = "Slug chỉ chứa chữ thường, số và dấu gạch ngang")]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Mô tả ngắn tối đa 500 ký tự")]
    [Display(Name = "Mô tả ngắn")]
    public string? Excerpt { get; set; }

    [Required(ErrorMessage = "Nội dung không được để trống")]
    [Display(Name = "Nội dung")]
    public string Content { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "Ảnh đại diện (URL)")]
    public string? FeaturedImage { get; set; }

    // SEO Fields
    [MaxLength(200)]
    [Display(Name = "Meta Title")]
    public string? MetaTitle { get; set; }

    [MaxLength(500)]
    [Display(Name = "Meta Description")]
    public string? MetaDescription { get; set; }

    [MaxLength(500)]
    [Display(Name = "Meta Keywords")]
    public string? MetaKeywords { get; set; }

    // Author & Category
    [MaxLength(100)]
    [Display(Name = "Tác giả")]
    public string Author { get; set; } = "TrendTag Team";

    [Display(Name = "Danh mục")]
    public int? CategoryId { get; set; }

    // Status
    [Display(Name = "Trạng thái")]
    public string Status { get; set; } = "Draft";

    [Display(Name = "Ngày đăng")]
    public DateTime? PublishedAt { get; set; }

    // For dropdown
    public List<BlogCategory> Categories { get; set; } = new();

    public static BlogPostEditViewModel FromPost(BlogPost post)
    {
        return new BlogPostEditViewModel
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Excerpt = post.Excerpt,
            Content = post.Content,
            FeaturedImage = post.FeaturedImage,
            MetaTitle = post.MetaTitle,
            MetaDescription = post.MetaDescription,
            MetaKeywords = post.MetaKeywords,
            Author = post.Author,
            CategoryId = post.CategoryId,
            Status = post.Status,
            PublishedAt = post.PublishedAt
        };
    }

    public void ApplyTo(BlogPost post)
    {
        post.Title = Title;
        post.Slug = Slug;
        post.Excerpt = Excerpt;
        post.Content = Content;
        post.FeaturedImage = FeaturedImage;
        post.MetaTitle = MetaTitle;
        post.MetaDescription = MetaDescription;
        post.MetaKeywords = MetaKeywords;
        post.Author = Author;
        post.CategoryId = CategoryId;
        post.Status = Status;
        post.PublishedAt = PublishedAt;
        post.UpdatedAt = DateTime.UtcNow;
    }
}

public class BlogGenerateViewModel
{
    public List<BlogCategory> Categories { get; set; } = new();
    public List<HashtagCategory> HashtagCategories { get; set; } = new();
    public string? LastGeneratedMessage { get; set; }
}

public class GenerateContentRequest
{
    public string Type { get; set; } = string.Empty; // monthly, weekly, category
    public int? CategoryId { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
}
