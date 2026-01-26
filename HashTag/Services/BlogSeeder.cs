using HashTag.Data;
using HashTag.Models;
using Microsoft.EntityFrameworkCore;

namespace HashTag.Services;

public class BlogSeeder
{
    private readonly TrendTagDbContext _context;
    private readonly ILogger<BlogSeeder> _logger;

    public BlogSeeder(TrendTagDbContext context, ILogger<BlogSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedBlogPostsAsync()
    {
        try
        {
            _logger.LogInformation("BlogSeeder: Checking if blog posts need to be seeded...");

            // Check if our specific seeded posts already exist by checking for a unique slug
            var seedMarkerSlug = "5-chien-luoc-hashtag-tiktok-tang-views";
            var alreadySeeded = await _context.BlogPosts.AnyAsync(p => p.Slug == seedMarkerSlug);
            if (alreadySeeded)
            {
                _logger.LogInformation("BlogSeeder: Seeded blog posts already exist, skipping");
                return;
            }

            var existingPostCount = await _context.BlogPosts.CountAsync();
            _logger.LogInformation("BlogSeeder: Found {Count} existing posts, will add 4 new seeded posts...", existingPostCount);

            // Seed Categories - check if they exist first
            var categoryDefinitions = new List<(string Name, string DisplayNameVi, string Slug, string Description)>
            {
                ("Hashtag Trending", "Phân Tích Trending", "phan-tich-trending", "Phân tích các hashtag trending mới nhất"),
                ("TikTok Tips", "Mẹo TikTok", "meo-tiktok", "Tips và chiến lược tăng trưởng trên TikTok"),
                ("Creator Guide", "Hướng Dẫn Creator", "huong-dan-creator", "Hướng dẫn cho TikTok creators")
            };

            var addedCategories = new List<BlogCategory>();
            foreach (var (name, displayNameVi, slug, description) in categoryDefinitions)
            {
                var existingCat = await _context.BlogCategories.FirstOrDefaultAsync(c => c.Slug == slug);
                if (existingCat == null)
                {
                    var newCat = new BlogCategory
                    {
                        Name = name,
                        DisplayNameVi = displayNameVi,
                        Slug = slug,
                        Description = description,
                        IsActive = true
                    };
                    _context.BlogCategories.Add(newCat);
                    addedCategories.Add(newCat);
                    _logger.LogInformation("BlogSeeder: Adding new category '{Name}'", name);
                }
                else
                {
                    addedCategories.Add(existingCat);
                    _logger.LogInformation("BlogSeeder: Category '{Name}' already exists, reusing", name);
                }
            }

            await _context.SaveChangesAsync();

            // Seed Blog Posts - get category references
            var tiktokTipsCat = addedCategories.First(c => c.Name == "TikTok Tips");
            var hashtagTrendingCat = addedCategories.First(c => c.Name == "Hashtag Trending");

            var posts = new List<BlogPost>
            {
//                 new BlogPost
//                 {
//                     Title = "Top 100 Hashtag TikTok Trending Tháng 12/2025",
//                     Slug = "top-100-hashtag-tiktok-trending-thang-12-2025",
//                     Excerpt = "Danh sách đầy đủ top 100 hashtag TikTok đang trending nhất tháng 12/2025. Phân tích chuyên sâu từng hashtag với metrics và tips sử dụng hiệu quả.",
//                     Content = @"<h2>Danh Sách Top 100 Hashtag TikTok Trending</h2>
// <p>Danh sách đầy đủ top 100 hashtag TikTok đang trending nhất tháng 12/2025. Phân tích chuyên sâu từng hashtag với metrics và tips sử dụng hiệu quả.</p>

// <p>Sử dụng công cụ <a href='/'>TrendTag</a> để xem danh sách cập nhật real-time và phân tích chi tiết từng hashtag!</p>

// <h2>Trending Hashtags Overview</h2>
// <p>Tháng 12/2025 chứng kiến sự bùng nổ của các hashtag về end-of-year content và holiday themes. Hashtag trending thay đổi liên tục, việc update thường xuyên là chìa khóa để viral.</p>",
//                     MetaTitle = "Top 100 Hashtag TikTok Trending Tháng 12/2025 | TrendTag",
//                     MetaDescription = "Danh sách top 100 hashtag TikTok trending tháng 12/2025 với metrics chính xác. Update hàng ngày.",
//                     MetaKeywords = "hashtag trending, top 100 hashtag, tiktok trending, hashtag tháng 12",
//                     Author = "TrendTag Team",
//                     CategoryId = hashtagTrendingCat.Id,
//                     Status = "Published",
//                     PublishedAt = DateTime.UtcNow.AddDays(-7),
//                     ViewCount = 234,
//                     CreatedAt = DateTime.UtcNow.AddDays(-7)
//                 },
                new BlogPost
                {
                    Title = "5 Chiến Lược Hashtag TikTok Giúp Tăng Views Nhanh Chóng",
                    Slug = "5-chien-luoc-hashtag-tiktok-tang-views",
                    Excerpt = "Khám phá 5 chiến lược hashtag TikTok hiệu quả nhất để tăng lượt xem và tương tác cho video của bạn.",
                    FeaturedImage = "https://images.unsplash.com/photo-1611162618071-b39a2ec055fb?w=1200&q=80",
                    Content = @"<h2>Tại Sao Hashtag Quan Trọng Trên TikTok?</h2>
<p>Hashtag là yếu tố quyết định giúp video của bạn tiếp cận đúng đối tượng và tăng khả năng viral trên TikTok. Một chiến lược hashtag tốt có thể tăng lượt xem lên 10-20 lần so với không sử dụng hashtag.</p>

<h2>1. Kết Hợp Hashtag Trending Và Niche</h2>
<p>Đừng chỉ sử dụng toàn hashtag trending! Hãy kết hợp:</p>
<ul>
<li><strong>1-2 hashtag trending</strong>: Để tăng reach (ví dụ: #fyp, #viral)</li>
<li><strong>2-3 hashtag niche</strong>: Liên quan trực tiếp đến nội dung (ví dụ: #makeup, #tutorial)</li>
<li><strong>1-2 hashtag ultra-niche</strong>: Rất cụ thể (ví dụ: #koreanskincare, #glowyskin)</li>
</ul>

<h2>2. Theo Dõi Hashtag Trending Hàng Ngày</h2>
<p>Sử dụng <a href='/'>TrendTag</a> để theo dõi top 100 hashtag trending mỗi ngày. Hashtag trending thay đổi liên tục, việc update thường xuyên giúp bạn không bỏ lỡ cơ hội viral.</p>

<h2>3. Phân Tích Hashtag Của Đối Thủ</h2>
<p>Tìm những creators thành công trong niche của bạn và phân tích:</p>
<ul>
<li>Họ sử dụng hashtag nào?</li>
<li>Hashtag nào mang lại nhiều views nhất?</li>
<li>Thời điểm nào họ đăng video?</li>
</ul>

<h2>4. Tạo Hashtag Riêng (Branded Hashtag)</h2>
<p>Nếu bạn có thương hiệu hoặc series video, hãy tạo hashtag riêng như #TenBrand hoặc #TenSeries. Điều này giúp xây dựng cộng đồng và tăng độ nhận diện.</p>

<h2>5. Kiểm Tra Độ Khó Của Hashtag</h2>
<p>Sử dụng công cụ <a href='/hashtag-generator'>Tạo Hashtag AI</a> để biết độ khó của hashtag. Đừng chỉ chọn hashtag có hàng tỷ views vì cạnh tranh rất cao. Hãy tìm hashtag có 1M-10M views để dễ viral hơn.</p>

<h2>Kết Luận</h2>
<p>Hashtag là công cụ miễn phí và cực kỳ hiệu quả để tăng reach trên TikTok. Hãy thử nghiệm các chiến lược trên và theo dõi kết quả để tìm ra công thức phù hợp nhất với niche của bạn!</p>",
                    MetaTitle = "5 Chiến Lược Hashtag TikTok Tăng Views 2025",
                    MetaDescription = "Khám phá 5 chiến lược hashtag TikTok hiệu quả nhất để tăng lượt xem và tương tác. Cập nhật mới nhất 2025.",
                    MetaKeywords = "hashtag tiktok, tăng views tiktok, chiến lược hashtag, viral tiktok, tiktok tips",
                    Author = "TrendTag Team",
                    CategoryId = tiktokTipsCat.Id,
                    Status = "Published",
                    PublishedAt = DateTime.UtcNow.AddDays(-5),
                    ViewCount = 145,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new BlogPost
                {
                    Title = "Cách Tìm Hashtag TikTok Trending Theo Từng Ngành",
                    Slug = "cach-tim-hashtag-tiktok-trending-theo-nganh",
                    Excerpt = "Hướng dẫn chi tiết cách tìm và sử dụng hashtag trending phù hợp với từng ngành nghề và lĩnh vực trên TikTok.",
                    FeaturedImage = "https://images.unsplash.com/photo-1611162618071-b39a2ec055fb?w=1200&q=80",
                    Content = @"<h2>Tại Sao Cần Hashtag Theo Ngành?</h2>
<p>Mỗi ngành nghề trên TikTok có cộng đồng riêng và hashtag riêng. Việc sử dụng đúng hashtag ngành giúp bạn:</p>
<ul>
<li>Tiếp cận đúng đối tượng quan tâm</li>
<li>Tăng tỷ lệ tương tác (engagement rate)</li>
<li>Xây dựng cộng đồng followers chất lượng</li>
</ul>

<h2>Các Ngành Phổ Biến Trên TikTok</h2>

<h3>1. Làm Đẹp & Skincare</h3>
<p>Hashtag phổ biến: #makeup, #skincare, #beauty, #kbeauty, #glowyskin</p>
<p>Mẹo: Kết hợp hashtag sản phẩm cụ thể như #serum, #cushion với hashtag trending.</p>

<h3>2. Ẩm Thực</h3>
<p>Hashtag phổ biến: #food, #cooking, #recipe, #asmr, #foodtiktok</p>
<p>Mẹo: Thêm hashtag về món ăn cụ thể như #pho, #banhmi, #streetfood.</p>

<h3>3. Du Lịch</h3>
<p>Hashtag phổ biến: #travel, #traveling, #vacation, #wanderlust</p>
<p>Mẹo: Thêm hashtag địa điểm như #dalat, #phuquoc, #vietnam.</p>

<h3>4. Giáo Dục & Học Tập</h3>
<p>Hashtag phổ biến: #learntiktok, #edutok, #study, #english</p>
<p>Mẹo: Sử dụng hashtag về môn học hoặc kỹ năng cụ thể.</p>

<h2>Cách Tìm Hashtag Trending Theo Ngành</h2>

<h3>Bước 1: Sử dụng TrendTag</h3>
<p>Truy cập <a href='/'>TrendTag</a> và lọc hashtag theo ngành. Tool sẽ hiển thị top 100 hashtag trending trong từng ngành với metrics chính xác.</p>

<h3>Bước 2: Theo Dõi Top Creators</h3>
<p>Tìm top 10 creators trong ngành của bạn và phân tích hashtag họ sử dụng. Lưu lại những hashtag xuất hiện thường xuyên.</p>

<h3>Bước 3: Kiểm Tra Độ Khó</h3>
<p>Sử dụng <a href='/hashtag-generator'>Tạo Hashtag AI</a> để kiểm tra độ khó. Chọn mix giữa hashtag dễ (Easy), trung bình (Medium) và khó (Hard).</p>

<h2>Lời Khuyên Quan Trọng</h2>
<ul>
<li>Đừng copy 100% hashtag của người khác</li>
<li>Update hashtag hàng tuần vì trending thay đổi</li>
<li>Test A/B với nhiều bộ hashtag khác nhau</li>
<li>Theo dõi analytics để biết hashtag nào hiệu quả</li>
</ul>",
                    MetaTitle = "Cách Tìm Hashtag TikTok Trending Theo Ngành 2025",
                    MetaDescription = "Hướng dẫn tìm hashtag TikTok trending theo từng ngành nghề: làm đẹp, ẩm thực, du lịch. Cập nhật 2025.",
                    MetaKeywords = "hashtag theo ngành, tiktok niche, trending hashtag, phân loại hashtag",
                    Author = "TrendTag Team",
                    CategoryId = hashtagTrendingCat.Id,
                    Status = "Published",
                    PublishedAt = DateTime.UtcNow.AddDays(-3),
                    ViewCount = 98,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new BlogPost
                {
                    Title = "Top 10 Sai Lầm Khi Sử Dụng Hashtag TikTok",
                    Slug = "top-10-sai-lam-khi-su-dung-hashtag-tiktok",
                    Excerpt = "Tránh những sai lầm phổ biến này để tăng hiệu quả sử dụng hashtag và tăng reach cho video TikTok của bạn.",
                    FeaturedImage = "https://images.unsplash.com/photo-1611162616475-46b635cb6868?w=1200&q=80",
                    Content = @"<h2>Hashtag Có Thể Giết Chết Video Của Bạn!</h2>
<p>Nhiều creators mắc phải những sai lầm nghiêm trọng khi sử dụng hashtag, khiến video không được TikTok đẩy lên FYP. Hãy tránh 10 sai lầm sau:</p>

<h2>1. Sử Dụng Quá Nhiều Hashtag</h2>
<p><strong>Sai lầm:</strong> Nhồi nhét 20-30 hashtag vào mô tả</p>
<p><strong>Đúng:</strong> Chỉ dùng 5-7 hashtag có liên quan</p>
<p>TikTok algorithm không thích spam. 5-7 hashtag chất lượng hiệu quả hơn 30 hashtag vô nghĩa.</p>

<h2>2. Chỉ Dùng Hashtag Viral</h2>
<p><strong>Sai lầm:</strong> #fyp #viral #foryou trong mọi video</p>
<p><strong>Đúng:</strong> Kết hợp trending + niche + ultra-niche</p>
<p>Hashtag quá phổ biến = cạnh tranh cực cao. Video của bạn sẽ chìm ngay lập tức.</p>

<h2>Kết Luận</h2>
<p>Tránh 10 sai lầm trên và bạn sẽ thấy video reach tăng đáng kể! Sử dụng <a href='/hashtag-generator'>Tạo Hashtag AI</a> để có bộ hashtag tối ưu cho từng video.</p>",
                    MetaTitle = "Top 10 Sai Lầm Hashtag TikTok Creator Thường Mắc",
                    MetaDescription = "10 sai lầm phổ biến khi dùng hashtag TikTok và cách khắc phục. Tăng reach hiệu quả hơn.",
                    MetaKeywords = "sai lầm hashtag, lỗi hashtag tiktok, tối ưu hashtag, tiktok tips",
                    Author = "TrendTag Team",
                    CategoryId = tiktokTipsCat.Id,
                    Status = "Published",
                    PublishedAt = DateTime.UtcNow.AddDays(-1),
                    ViewCount = 67,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            };

            _logger.LogInformation("BlogSeeder: Adding {PostCount} posts to database...",
                posts.Count);

            _context.BlogPosts.AddRange(posts);

            var savedCount = await _context.SaveChangesAsync();
            _logger.LogInformation("BlogSeeder: SaveChanges completed, affected {Count} rows", savedCount);

            // Verify posts were actually saved
            var verifyCount = await _context.BlogPosts.CountAsync(p => p.Slug == seedMarkerSlug);
            _logger.LogInformation("BlogSeeder: Verification - Found {Count} posts with marker slug", verifyCount);

            _logger.LogInformation("BlogSeeder: ✅ Successfully seeded {Count} blog posts!", posts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BlogSeeder: ❌ Error seeding blog posts: {Message}", ex.Message);
            // Don't throw - allow app to continue starting even if seeding fails
        }
    }
}
