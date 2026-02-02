using System.Text;
using HashTag.Data;
using HashTag.Helpers;
using HashTag.Models;
using Microsoft.EntityFrameworkCore;

namespace HashTag.Services;

public class BlogAutoGeneratorService : IBlogAutoGeneratorService
{
    private readonly TrendTagDbContext _context;
    private readonly ILogger<BlogAutoGeneratorService> _logger;

    // Default images for different content types (from Unsplash - free to use)
    private static readonly Dictionary<string, string> DefaultImages = new()
    {
        { "trending", "https://images.unsplash.com/photo-1611162616305-c69b3fa7fbe0?w=1200&q=80" },
        { "beauty", "https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=1200&q=80" },
        { "food", "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=1200&q=80" },
        { "travel", "https://images.unsplash.com/photo-1469854523086-cc02fe5d8800?w=1200&q=80" },
        { "fashion", "https://images.unsplash.com/photo-1558171813-4c088753af8f?w=1200&q=80" },
        { "tech", "https://images.unsplash.com/photo-1518770660439-4636190af475?w=1200&q=80" },
        { "gaming", "https://images.unsplash.com/photo-1542751371-adc38448a05e?w=1200&q=80" },
        { "fitness", "https://images.unsplash.com/photo-1517836357463-d25dfeac3438?w=1200&q=80" },
        { "education", "https://images.unsplash.com/photo-1503676260728-1c00da094a0b?w=1200&q=80" },
        { "entertainment", "https://images.unsplash.com/photo-1603190287605-e6ade32fa852?w=1200&q=80" },
        { "weekly", "https://images.unsplash.com/photo-1611162617474-5b21e879e113?w=1200&q=80" },
        { "monthly", "https://images.unsplash.com/photo-1611162618071-b39a2ec055fb?w=1200&q=80" },
        { "default", "https://images.unsplash.com/photo-1611162616305-c69b3fa7fbe0?w=1200&q=80" }
    };

    public BlogAutoGeneratorService(
        TrendTagDbContext context,
        ILogger<BlogAutoGeneratorService> logger)
    {
        _context = context;
        _logger = logger;
    }

    private static string GetImageForCategory(string? categoryName)
    {
        if (string.IsNullOrEmpty(categoryName))
            return DefaultImages["default"];

        var lowerName = categoryName.ToLower();

        if (lowerName.Contains("beauty") || lowerName.Contains("làm đẹp") || lowerName.Contains("skincare") || lowerName.Contains("personal care"))
            return DefaultImages["beauty"];
        if (lowerName.Contains("food") || lowerName.Contains("ẩm thực") || lowerName.Contains("cooking") || lowerName.Contains("beverage"))
            return DefaultImages["food"];
        if (lowerName.Contains("travel") || lowerName.Contains("du lịch"))
            return DefaultImages["travel"];
        if (lowerName.Contains("fashion") || lowerName.Contains("thời trang") || lowerName.Contains("apparel"))
            return DefaultImages["fashion"];
        if (lowerName.Contains("tech") || lowerName.Contains("công nghệ") || lowerName.Contains("electronics"))
            return DefaultImages["tech"];
        if (lowerName.Contains("game") || lowerName.Contains("gaming"))
            return DefaultImages["gaming"];
        if (lowerName.Contains("fitness") || lowerName.Contains("gym") || lowerName.Contains("sport") || lowerName.Contains("outdoor"))
            return DefaultImages["fitness"];
        if (lowerName.Contains("edu") || lowerName.Contains("học") || lowerName.Contains("education"))
            return DefaultImages["education"];
        if (lowerName.Contains("entertainment") || lowerName.Contains("giải trí") || lowerName.Contains("news"))
            return DefaultImages["entertainment"];

        return DefaultImages["default"];
    }

    public async Task<BlogPost?> GenerateMonthlyTopHashtagsAsync(int month, int year)
    {
        _logger.LogInformation("Generating monthly top hashtags post for {Month}/{Year}", month, year);

        // Check if post already exists for this month
        var baseSlug = $"top-20-hashtag-tiktok-thang-{month}-{year}";
        var existingPost = await _context.BlogPosts.FirstOrDefaultAsync(p => p.Slug == baseSlug);
        if (existingPost != null)
        {
            _logger.LogInformation("Monthly post for {Month}/{Year} already exists (ID: {Id}), skipping generation",
                month, year, existingPost.Id);
            return existingPost;
        }

        var monthName = GetVietnameseMonthName(month);
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        // Get top hashtags for the month based on view count
        var topHashtags = await _context.Hashtags
            .Include(h => h.Category)
            .Where(h => h.IsActive && h.LatestViewCount > 0)
            .OrderByDescending(h => h.LatestViewCount)
            .Take(20)
            .ToListAsync();

        if (!topHashtags.Any())
        {
            _logger.LogWarning("No hashtags found for monthly report");
            return null;
        }

        var title = $"Top 20 Hashtag TikTok Hot Nhất Tháng {month}/{year}";
        var content = GenerateMonthlyContent(topHashtags, month, year, monthName);

        // Generate English content
        var titleEn = $"Top 20 Hottest TikTok Hashtags in {GetEnglishMonthName(month)} {year}";
        var contentEn = GenerateMonthlyContentEnglish(topHashtags, month, year);

        var post = new BlogPost
        {
            Title = title,
            Slug = baseSlug,
            Excerpt = $"Tổng hợp 20 hashtag TikTok được sử dụng nhiều nhất trong tháng {month}/{year}. Cập nhật xu hướng mới nhất giúp bạn tăng view và reach cho video.",
            Content = content,
            FeaturedImage = DefaultImages["monthly"],
            MetaTitle = $"Top 20 Hashtag TikTok Hot Nhất Tháng {month}/{year} | ViralHashtag",
            MetaDescription = $"Danh sách 20 hashtag TikTok trending tháng {month}/{year}. Xem ngay để biết hashtag nào đang hot và cách sử dụng hiệu quả nhất.",
            MetaKeywords = $"hashtag tiktok tháng {month}, trending hashtag {year}, top hashtag vietnam, viral hashtag",
            // English content
            TitleEn = titleEn,
            SlugEn = $"top-20-tiktok-hashtags-{GetEnglishMonthName(month).ToLower()}-{year}",
            ExcerptEn = $"Discover the top 20 most-used TikTok hashtags in {GetEnglishMonthName(month)} {year}. Stay updated with the latest trends to boost your video views and reach.",
            ContentEn = contentEn,
            MetaTitleEn = $"Top 20 Hottest TikTok Hashtags {GetEnglishMonthName(month)} {year} | ViralHashtag",
            MetaDescriptionEn = $"Complete list of top 20 trending TikTok hashtags for {GetEnglishMonthName(month)} {year}. Learn which hashtags are hot and how to use them effectively.",
            MetaKeywordsEn = $"tiktok hashtags {GetEnglishMonthName(month).ToLower()} {year}, trending hashtags, top hashtags vietnam, viral hashtag",
            Author = "TrendTag Team",
            Status = "Published",
            PublishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        // Find or create blog category
        var blogCategory = await _context.BlogCategories.FirstOrDefaultAsync(c => c.Slug == "phan-tich-trending");
        if (blogCategory != null)
        {
            post.CategoryId = blogCategory.Id;
        }

        _context.BlogPosts.Add(post);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Generated and published monthly post: {Title} (ID: {Id})", post.Title, post.Id);
        return post;
    }

    public async Task<BlogPost?> GenerateCategoryTopHashtagsAsync(int categoryId)
    {
        var category = await _context.HashtagCategories.FindAsync(categoryId);
        if (category == null)
        {
            _logger.LogWarning("Category {CategoryId} not found", categoryId);
            return null;
        }

        _logger.LogInformation("Generating category top hashtags post for: {Category}", category.Name);

        var displayName = !string.IsNullOrEmpty(category.DisplayNameVi) ? category.DisplayNameVi : category.Name;
        var year = DateTime.Now.Year;

        // Check if post already exists for this category and year
        var baseSlug = $"top-hashtag-{VietnameseHelper.ToUrlSlug(displayName)}-tiktok-{year}";
        var existingPost = await _context.BlogPosts.FirstOrDefaultAsync(p => p.Slug == baseSlug);
        if (existingPost != null)
        {
            _logger.LogInformation("Category post for {Category} {Year} already exists (ID: {Id}), skipping generation",
                displayName, year, existingPost.Id);
            return existingPost;
        }

        // Get top hashtags for this category
        var topHashtags = await _context.Hashtags
            .Where(h => h.IsActive && h.CategoryId == categoryId && h.LatestViewCount > 0)
            .OrderByDescending(h => h.LatestViewCount)
            .Take(15)
            .ToListAsync();

        if (!topHashtags.Any())
        {
            _logger.LogWarning("No hashtags found for category: {Category}", category.Name);
            return null;
        }

        var title = $"Top 15 Hashtag {displayName} Đang Viral Trên TikTok {year}";
        var content = GenerateCategoryContent(topHashtags, displayName, category.Name, year);

        // Generate English content
        var titleEn = $"Top 15 {category.Name} Hashtags Going Viral on TikTok {year}";
        var contentEn = GenerateCategoryContentEnglish(topHashtags, category.Name, year);

        var post = new BlogPost
        {
            Title = title,
            Slug = baseSlug,
            Excerpt = $"Tổng hợp 15 hashtag {displayName} hot nhất trên TikTok {year}. Sử dụng đúng hashtag để tăng view và tiếp cận đúng đối tượng.",
            Content = content,
            FeaturedImage = GetImageForCategory(category.Name),
            MetaTitle = $"Top 15 Hashtag {displayName} TikTok {year} | ViralHashtag",
            MetaDescription = $"Danh sách hashtag {displayName} trending trên TikTok. Xem ngay cách sử dụng và kết hợp hashtag hiệu quả nhất.",
            MetaKeywords = $"hashtag {displayName.ToLower()}, hashtag tiktok {category.Name.ToLower()}, trending hashtag {year}",
            // English content
            TitleEn = titleEn,
            SlugEn = $"top-{category.Name.ToLower().Replace(" ", "-")}-hashtags-tiktok-{year}",
            ExcerptEn = $"Discover the top 15 hottest {category.Name} hashtags on TikTok {year}. Use the right hashtags to boost views and reach your target audience.",
            ContentEn = contentEn,
            MetaTitleEn = $"Top 15 {category.Name} TikTok Hashtags {year} | ViralHashtag",
            MetaDescriptionEn = $"Complete list of trending {category.Name} hashtags on TikTok. Learn how to use and combine hashtags effectively.",
            MetaKeywordsEn = $"{category.Name.ToLower()} hashtags, tiktok {category.Name.ToLower()}, trending hashtags {year}",
            Author = "TrendTag Team",
            Status = "Published",
            PublishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        var blogCategory = await _context.BlogCategories.FirstOrDefaultAsync(c => c.Slug == "phan-tich-trending");
        if (blogCategory != null)
        {
            post.CategoryId = blogCategory.Id;
        }

        _context.BlogPosts.Add(post);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Generated and published category post: {Title} (ID: {Id})", post.Title, post.Id);
        return post;
    }

    public async Task<BlogPost?> GenerateWeeklyTrendingReportAsync()
    {
        var endDate = DateTime.Now;
        var startDate = endDate.AddDays(-7);

        _logger.LogInformation("Generating weekly trending report for {Start} - {End}", startDate.ToString("dd/MM"), endDate.ToString("dd/MM"));

        // Check if post already exists for this week
        var baseSlug = $"hashtag-trending-tuan-{startDate:dd-MM}-{endDate:dd-MM-yyyy}";
        var existingPost = await _context.BlogPosts.FirstOrDefaultAsync(p => p.Slug == baseSlug);
        if (existingPost != null)
        {
            _logger.LogInformation("Weekly post for {Start} - {End} already exists (ID: {Id}), skipping generation",
                startDate.ToString("dd/MM"), endDate.ToString("dd/MM"), existingPost.Id);
            return existingPost;
        }

        // Get hashtags with recent activity
        var trendingHashtags = await _context.Hashtags
            .Include(h => h.Category)
            .Where(h => h.IsActive && h.LastSeen >= startDate)
            .OrderByDescending(h => h.LatestViewCount)
            .Take(15)
            .ToListAsync();

        if (!trendingHashtags.Any())
        {
            _logger.LogWarning("No trending hashtags found for weekly report");
            return null;
        }

        var title = $"Hashtag Nổi Bật Tuần {startDate:dd/MM} - {endDate:dd/MM/yyyy}";
        var content = GenerateWeeklyContent(trendingHashtags, startDate, endDate);

        // Generate English content
        var titleEn = $"Top Trending Hashtags This Week ({startDate:MMM dd} - {endDate:MMM dd, yyyy})";
        var contentEn = GenerateWeeklyContentEnglish(trendingHashtags, startDate, endDate);

        var post = new BlogPost
        {
            Title = title,
            Slug = baseSlug,
            Excerpt = $"Tổng hợp hashtag TikTok nổi bật tuần {startDate:dd/MM} - {endDate:dd/MM}. Cập nhật xu hướng mới nhất để không bỏ lỡ trend hot.",
            Content = content,
            FeaturedImage = DefaultImages["weekly"],
            MetaTitle = $"Hashtag TikTok Trending Tuần {startDate:dd/MM} - {endDate:dd/MM} | ViralHashtag",
            MetaDescription = $"Báo cáo xu hướng hashtag TikTok tuần này. Xem ngay top hashtag hot và dự đoán trend tuần tới.",
            MetaKeywords = $"hashtag trending tuần này, xu hướng tiktok, viral hashtag vietnam, trend tiktok {DateTime.Now.Year}",
            // English content
            TitleEn = titleEn,
            SlugEn = $"trending-hashtags-week-{startDate:MMM-dd}-{endDate:MMM-dd-yyyy}".ToLower(),
            ExcerptEn = $"Weekly TikTok trending hashtags report ({startDate:MMM dd} - {endDate:MMM dd}). Stay updated with the latest trends and don't miss the hottest hashtags.",
            ContentEn = contentEn,
            MetaTitleEn = $"TikTok Trending Hashtags {startDate:MMM dd} - {endDate:MMM dd} | ViralHashtag",
            MetaDescriptionEn = $"Weekly TikTok hashtag trends report. Discover top trending hashtags and predictions for next week.",
            MetaKeywordsEn = $"trending hashtags this week, tiktok trends, viral hashtags, tiktok trends {DateTime.Now.Year}",
            Author = "TrendTag Team",
            Status = "Published",
            PublishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        var blogCategory = await _context.BlogCategories.FirstOrDefaultAsync(c => c.Slug == "phan-tich-trending");
        if (blogCategory != null)
        {
            post.CategoryId = blogCategory.Id;
        }

        _context.BlogPosts.Add(post);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Generated and published weekly post: {Title} (ID: {Id})", post.Title, post.Id);
        return post;
    }

    #region Content Generators

    private string GenerateMonthlyContent(List<Hashtag> hashtags, int month, int year, string monthName)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"<p>Tháng {month}/{year} đã chứng kiến nhiều xu hướng hashtag thú vị trên TikTok Việt Nam. Dưới đây là tổng hợp <strong>20 hashtag được sử dụng nhiều nhất</strong>, giúp bạn nắm bắt trend và tối ưu content của mình.</p>");

        sb.AppendLine("<h2>Bảng Xếp Hạng Top 20 Hashtag</h2>");
        sb.AppendLine("<div class=\"table-responsive\">");
        sb.AppendLine("<table class=\"table table-striped\">");
        sb.AppendLine("<thead><tr><th>#</th><th>Hashtag</th><th>Lượt xem</th><th>Bài đăng</th><th>Chủ đề</th></tr></thead>");
        sb.AppendLine("<tbody>");

        int rank = 1;
        foreach (var h in hashtags)
        {
            var viewsFormatted = VietnameseHelper.FormatNumber(h.LatestViewCount ?? 0);
            var postsFormatted = VietnameseHelper.FormatNumber(h.LatestPostCount ?? 0);
            var categoryName = h.Category?.DisplayNameVi ?? h.Category?.Name ?? "Khác";

            sb.AppendLine($"<tr>");
            sb.AppendLine($"<td><strong>{rank}</strong></td>");
            sb.AppendLine($"<td><a href=\"/hashtag/{VietnameseHelper.ToUrlSlug(h.Tag)}\">{h.TagDisplay}</a></td>");
            sb.AppendLine($"<td>{viewsFormatted}</td>");
            sb.AppendLine($"<td>{postsFormatted}</td>");
            sb.AppendLine($"<td>{categoryName}</td>");
            sb.AppendLine($"</tr>");
            rank++;
        }

        sb.AppendLine("</tbody></table></div>");

        sb.AppendLine("<h2>Phân Tích Xu Hướng</h2>");
        sb.AppendLine($"<p>Trong tháng {month}, các hashtag về <strong>giải trí, đời sống</strong> tiếp tục chiếm ưu thế. Đặc biệt, các hashtag liên quan đến trend viral đã thu hút hàng triệu lượt xem.</p>");

        sb.AppendLine("<h2>Tips Sử Dụng Hashtag Hiệu Quả</h2>");
        sb.AppendLine("<ul>");
        sb.AppendLine("<li><strong>Kết hợp hashtag hot và niche:</strong> Dùng 2-3 hashtag trending kèm 2-3 hashtag chuyên ngành của bạn.</li>");
        sb.AppendLine("<li><strong>Đăng vào khung giờ vàng:</strong> 12h-14h và 19h-22h là thời điểm nhiều người online nhất.</li>");
        sb.AppendLine("<li><strong>Theo dõi trend mới:</strong> Sử dụng <a href=\"/\">công cụ tra cứu hashtag</a> để cập nhật xu hướng real-time.</li>");
        sb.AppendLine("</ul>");

        sb.AppendLine($"<p><em>Dữ liệu cập nhật tháng {month}/{year} từ TikTok Việt Nam.</em></p>");

        return sb.ToString();
    }

    private string GenerateCategoryContent(List<Hashtag> hashtags, string displayName, string categoryName, int year)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"<p>Nếu bạn đang làm content về <strong>{displayName}</strong>, việc sử dụng đúng hashtag là chìa khóa để video được nhiều người xem hơn. Dưới đây là <strong>15 hashtag {displayName} hot nhất</strong> trên TikTok {year}.</p>");

        sb.AppendLine($"<h2>Top 15 Hashtag {displayName}</h2>");
        sb.AppendLine("<div class=\"table-responsive\">");
        sb.AppendLine("<table class=\"table table-striped\">");
        sb.AppendLine("<thead><tr><th>#</th><th>Hashtag</th><th>Lượt xem</th><th>Bài đăng</th></tr></thead>");
        sb.AppendLine("<tbody>");

        int rank = 1;
        foreach (var h in hashtags)
        {
            var viewsFormatted = VietnameseHelper.FormatNumber(h.LatestViewCount ?? 0);
            var postsFormatted = VietnameseHelper.FormatNumber(h.LatestPostCount ?? 0);

            sb.AppendLine($"<tr>");
            sb.AppendLine($"<td><strong>{rank}</strong></td>");
            sb.AppendLine($"<td><a href=\"/hashtag/{VietnameseHelper.ToUrlSlug(h.Tag)}\">{h.TagDisplay}</a></td>");
            sb.AppendLine($"<td>{viewsFormatted}</td>");
            sb.AppendLine($"<td>{postsFormatted}</td>");
            sb.AppendLine($"</tr>");
            rank++;
        }

        sb.AppendLine("</tbody></table></div>");

        sb.AppendLine("<h2>Cách Kết Hợp Hashtag Hiệu Quả</h2>");
        sb.AppendLine($"<p>Khi làm video về {displayName}, bạn nên kết hợp:</p>");
        sb.AppendLine("<ul>");
        sb.AppendLine($"<li><strong>2-3 hashtag ngành:</strong> Chọn từ danh sách trên như {hashtags.FirstOrDefault()?.TagDisplay ?? "#hashtag"}</li>");
        sb.AppendLine("<li><strong>1-2 hashtag trending chung:</strong> #fyp, #xuhuong, #viral</li>");
        sb.AppendLine("<li><strong>1 hashtag riêng:</strong> Tạo hashtag brand của bạn</li>");
        sb.AppendLine("</ul>");

        sb.AppendLine($"<h2>Ý Tưởng Content {displayName}</h2>");
        sb.AppendLine("<p>Một số ý tưởng content phù hợp với các hashtag trên:</p>");
        sb.AppendLine("<ul>");
        sb.AppendLine("<li>Review sản phẩm/dịch vụ</li>");
        sb.AppendLine("<li>Tutorial/Hướng dẫn</li>");
        sb.AppendLine("<li>Before & After</li>");
        sb.AppendLine("<li>Day in my life</li>");
        sb.AppendLine("<li>Tips & Tricks</li>");
        sb.AppendLine("</ul>");

        sb.AppendLine($"<p><em>Tra cứu thêm hashtag {displayName} tại <a href=\"/chu-de/{VietnameseHelper.ToUrlSlug(categoryName)}\">đây</a>.</em></p>");

        return sb.ToString();
    }

    private string GenerateWeeklyContent(List<Hashtag> hashtags, DateTime startDate, DateTime endDate)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"<p>Báo cáo xu hướng hashtag TikTok từ <strong>{startDate:dd/MM}</strong> đến <strong>{endDate:dd/MM/yyyy}</strong>. Cùng xem những hashtag nào đang \"làm mưa làm gió\" trên TikTok tuần này!</p>");

        sb.AppendLine("<h2>Top Hashtag Nổi Bật Tuần Này</h2>");
        sb.AppendLine("<div class=\"table-responsive\">");
        sb.AppendLine("<table class=\"table table-striped\">");
        sb.AppendLine("<thead><tr><th>#</th><th>Hashtag</th><th>Lượt xem</th><th>Chủ đề</th></tr></thead>");
        sb.AppendLine("<tbody>");

        int rank = 1;
        foreach (var h in hashtags.Take(10))
        {
            var viewsFormatted = VietnameseHelper.FormatNumber(h.LatestViewCount ?? 0);
            var categoryName = h.Category?.DisplayNameVi ?? h.Category?.Name ?? "Khác";

            sb.AppendLine($"<tr>");
            sb.AppendLine($"<td><strong>{rank}</strong></td>");
            sb.AppendLine($"<td><a href=\"/hashtag/{VietnameseHelper.ToUrlSlug(h.Tag)}\">{h.TagDisplay}</a></td>");
            sb.AppendLine($"<td>{viewsFormatted}</td>");
            sb.AppendLine($"<td>{categoryName}</td>");
            sb.AppendLine($"</tr>");
            rank++;
        }

        sb.AppendLine("</tbody></table></div>");

        sb.AppendLine("<h2>Nhận Xét</h2>");
        sb.AppendLine("<p>Tuần này, các xu hướng nổi bật bao gồm:</p>");
        sb.AppendLine("<ul>");
        sb.AppendLine("<li>Các hashtag giải trí tiếp tục dẫn đầu</li>");
        sb.AppendLine("<li>Content lifestyle và daily vlog được yêu thích</li>");
        sb.AppendLine("<li>Trend mới bắt đầu xuất hiện từ cuối tuần</li>");
        sb.AppendLine("</ul>");

        sb.AppendLine("<h2>Dự Đoán Tuần Tới</h2>");
        sb.AppendLine("<p>Dựa trên phân tích dữ liệu, các hashtag sau có thể sẽ hot trong tuần tới:</p>");
        sb.AppendLine("<ul>");

        foreach (var h in hashtags.Skip(10).Take(5))
        {
            sb.AppendLine($"<li><a href=\"/hashtag/{VietnameseHelper.ToUrlSlug(h.Tag)}\">{h.TagDisplay}</a></li>");
        }

        sb.AppendLine("</ul>");

        sb.AppendLine("<p><strong>Theo dõi xu hướng real-time:</strong> Sử dụng <a href=\"/\">công cụ tra cứu hashtag</a> để cập nhật trend mới nhất!</p>");

        return sb.ToString();
    }

    // ============ English Content Generators ============

    private string GenerateMonthlyContentEnglish(List<Hashtag> hashtags, int month, int year)
    {
        var sb = new StringBuilder();
        var monthName = GetEnglishMonthName(month);

        sb.AppendLine($"<p>{monthName} {year} has seen many exciting hashtag trends on TikTok Vietnam. Below is a compilation of the <strong>top 20 most-used hashtags</strong> to help you stay on trend and optimize your content.</p>");

        sb.AppendLine("<h2>Top 20 Hashtag Rankings</h2>");
        sb.AppendLine("<div class=\"table-responsive\">");
        sb.AppendLine("<table class=\"table table-striped\">");
        sb.AppendLine("<thead><tr><th>#</th><th>Hashtag</th><th>Views</th><th>Posts</th><th>Category</th></tr></thead>");
        sb.AppendLine("<tbody>");

        int rank = 1;
        foreach (var h in hashtags)
        {
            var viewsFormatted = VietnameseHelper.FormatNumber(h.LatestViewCount ?? 0);
            var postsFormatted = VietnameseHelper.FormatNumber(h.LatestPostCount ?? 0);
            var categoryName = h.Category?.Name ?? "Other";

            sb.AppendLine($"<tr>");
            sb.AppendLine($"<td><strong>{rank}</strong></td>");
            sb.AppendLine($"<td><a href=\"/hashtag/{VietnameseHelper.ToUrlSlug(h.Tag)}\">{h.TagDisplay}</a></td>");
            sb.AppendLine($"<td>{viewsFormatted}</td>");
            sb.AppendLine($"<td>{postsFormatted}</td>");
            sb.AppendLine($"<td>{categoryName}</td>");
            sb.AppendLine($"</tr>");
            rank++;
        }

        sb.AppendLine("</tbody></table></div>");

        sb.AppendLine("<h2>Trend Analysis</h2>");
        sb.AppendLine($"<p>In {monthName}, hashtags about <strong>entertainment and lifestyle</strong> continue to dominate. Notably, hashtags related to viral trends have attracted millions of views.</p>");

        sb.AppendLine("<h2>Tips for Using Hashtags Effectively</h2>");
        sb.AppendLine("<ul>");
        sb.AppendLine("<li><strong>Combine hot and niche hashtags:</strong> Use 2-3 trending hashtags along with 2-3 industry-specific hashtags.</li>");
        sb.AppendLine("<li><strong>Post during peak hours:</strong> 12pm-2pm and 7pm-10pm are when most users are online.</li>");
        sb.AppendLine("<li><strong>Track new trends:</strong> Use our <a href=\"/\">hashtag lookup tool</a> to stay updated with real-time trends.</li>");
        sb.AppendLine("</ul>");

        sb.AppendLine($"<p><em>Data updated for {monthName} {year} from TikTok Vietnam.</em></p>");

        return sb.ToString();
    }

    private string GenerateCategoryContentEnglish(List<Hashtag> hashtags, string categoryName, int year)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"<p>If you're creating content about <strong>{categoryName}</strong>, using the right hashtags is key to getting more views. Here are the <strong>top 15 hottest {categoryName} hashtags</strong> on TikTok {year}.</p>");

        sb.AppendLine($"<h2>Top 15 {categoryName} Hashtags</h2>");
        sb.AppendLine("<div class=\"table-responsive\">");
        sb.AppendLine("<table class=\"table table-striped\">");
        sb.AppendLine("<thead><tr><th>#</th><th>Hashtag</th><th>Views</th><th>Posts</th></tr></thead>");
        sb.AppendLine("<tbody>");

        int rank = 1;
        foreach (var h in hashtags)
        {
            var viewsFormatted = VietnameseHelper.FormatNumber(h.LatestViewCount ?? 0);
            var postsFormatted = VietnameseHelper.FormatNumber(h.LatestPostCount ?? 0);

            sb.AppendLine($"<tr>");
            sb.AppendLine($"<td><strong>{rank}</strong></td>");
            sb.AppendLine($"<td><a href=\"/hashtag/{VietnameseHelper.ToUrlSlug(h.Tag)}\">{h.TagDisplay}</a></td>");
            sb.AppendLine($"<td>{viewsFormatted}</td>");
            sb.AppendLine($"<td>{postsFormatted}</td>");
            sb.AppendLine($"</tr>");
            rank++;
        }

        sb.AppendLine("</tbody></table></div>");

        sb.AppendLine("<h2>How to Combine Hashtags Effectively</h2>");
        sb.AppendLine($"<p>When creating {categoryName} videos, you should combine:</p>");
        sb.AppendLine("<ul>");
        sb.AppendLine($"<li><strong>2-3 industry hashtags:</strong> Choose from the list above like {hashtags.FirstOrDefault()?.TagDisplay ?? "#hashtag"}</li>");
        sb.AppendLine("<li><strong>1-2 general trending hashtags:</strong> #fyp, #trending, #viral</li>");
        sb.AppendLine("<li><strong>1 branded hashtag:</strong> Create your own brand hashtag</li>");
        sb.AppendLine("</ul>");

        sb.AppendLine($"<h2>{categoryName} Content Ideas</h2>");
        sb.AppendLine("<p>Some content ideas that work well with these hashtags:</p>");
        sb.AppendLine("<ul>");
        sb.AppendLine("<li>Product/Service reviews</li>");
        sb.AppendLine("<li>Tutorials/How-to guides</li>");
        sb.AppendLine("<li>Before & After transformations</li>");
        sb.AppendLine("<li>Day in my life vlogs</li>");
        sb.AppendLine("<li>Tips & Tricks</li>");
        sb.AppendLine("</ul>");

        sb.AppendLine($"<p><em>Find more {categoryName} hashtags <a href=\"/\">here</a>.</em></p>");

        return sb.ToString();
    }

    private string GenerateWeeklyContentEnglish(List<Hashtag> hashtags, DateTime startDate, DateTime endDate)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"<p>TikTok hashtag trends report from <strong>{startDate:MMM dd}</strong> to <strong>{endDate:MMM dd, yyyy}</strong>. Let's see which hashtags are dominating TikTok this week!</p>");

        sb.AppendLine("<h2>Top Trending Hashtags This Week</h2>");
        sb.AppendLine("<div class=\"table-responsive\">");
        sb.AppendLine("<table class=\"table table-striped\">");
        sb.AppendLine("<thead><tr><th>#</th><th>Hashtag</th><th>Views</th><th>Category</th></tr></thead>");
        sb.AppendLine("<tbody>");

        int rank = 1;
        foreach (var h in hashtags.Take(10))
        {
            var viewsFormatted = VietnameseHelper.FormatNumber(h.LatestViewCount ?? 0);
            var categoryName = h.Category?.Name ?? "Other";

            sb.AppendLine($"<tr>");
            sb.AppendLine($"<td><strong>{rank}</strong></td>");
            sb.AppendLine($"<td><a href=\"/hashtag/{VietnameseHelper.ToUrlSlug(h.Tag)}\">{h.TagDisplay}</a></td>");
            sb.AppendLine($"<td>{viewsFormatted}</td>");
            sb.AppendLine($"<td>{categoryName}</td>");
            sb.AppendLine($"</tr>");
            rank++;
        }

        sb.AppendLine("</tbody></table></div>");

        sb.AppendLine("<h2>Analysis</h2>");
        sb.AppendLine("<p>This week's notable trends include:</p>");
        sb.AppendLine("<ul>");
        sb.AppendLine("<li>Entertainment hashtags continue to lead</li>");
        sb.AppendLine("<li>Lifestyle and daily vlog content remains popular</li>");
        sb.AppendLine("<li>New trends started emerging towards the weekend</li>");
        sb.AppendLine("</ul>");

        sb.AppendLine("<h2>Predictions for Next Week</h2>");
        sb.AppendLine("<p>Based on data analysis, these hashtags may trend next week:</p>");
        sb.AppendLine("<ul>");

        foreach (var h in hashtags.Skip(10).Take(5))
        {
            sb.AppendLine($"<li><a href=\"/hashtag/{VietnameseHelper.ToUrlSlug(h.Tag)}\">{h.TagDisplay}</a></li>");
        }

        sb.AppendLine("</ul>");

        sb.AppendLine("<p><strong>Track real-time trends:</strong> Use our <a href=\"/\">hashtag lookup tool</a> to stay updated with the latest trends!</p>");

        return sb.ToString();
    }

    #endregion

    #region Helper Methods

    private static string GetEnglishMonthName(int month)
    {
        return month switch
        {
            1 => "January",
            2 => "February",
            3 => "March",
            4 => "April",
            5 => "May",
            6 => "June",
            7 => "July",
            8 => "August",
            9 => "September",
            10 => "October",
            11 => "November",
            12 => "December",
            _ => month.ToString()
        };
    }

    private async Task<string> GetUniqueSlugAsync(string baseSlug)
    {
        var slug = baseSlug;
        int counter = 1;

        while (await _context.BlogPosts.AnyAsync(p => p.Slug == slug))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }

        return slug;
    }

    private static string GetVietnameseMonthName(int month)
    {
        return month switch
        {
            1 => "Một",
            2 => "Hai",
            3 => "Ba",
            4 => "Tư",
            5 => "Năm",
            6 => "Sáu",
            7 => "Bảy",
            8 => "Tám",
            9 => "Chín",
            10 => "Mười",
            11 => "Mười Một",
            12 => "Mười Hai",
            _ => month.ToString()
        };
    }

    #endregion
}
