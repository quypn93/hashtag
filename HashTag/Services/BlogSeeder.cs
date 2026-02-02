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
            var categoryDefinitions = new List<(string Name, string NameEn, string DisplayNameVi, string Slug, string Description, string DescriptionEn)>
            {
                ("Phân Tích Trending", "Hashtag Trending", "Phân Tích Trending", "phan-tich-trending", "Phân tích các hashtag trending mới nhất", "Analysis of the latest trending hashtags"),
                ("Mẹo TikTok", "TikTok Tips", "Mẹo TikTok", "meo-tiktok", "Tips và chiến lược tăng trưởng trên TikTok", "Tips and strategies for TikTok growth"),
                ("Hướng Dẫn Creator", "Creator Guide", "Hướng Dẫn Creator", "huong-dan-creator", "Hướng dẫn cho TikTok creators", "Guides for TikTok creators")
            };

            var addedCategories = new List<BlogCategory>();
            foreach (var (name, nameEn, displayNameVi, slug, description, descriptionEn) in categoryDefinitions)
            {
                var existingCat = await _context.BlogCategories.FirstOrDefaultAsync(c => c.Slug == slug);
                if (existingCat == null)
                {
                    var newCat = new BlogCategory
                    {
                        Name = name,
                        NameEn = nameEn,
                        DisplayNameVi = displayNameVi,
                        Slug = slug,
                        Description = description,
                        DescriptionEn = descriptionEn,
                        IsActive = true
                    };
                    _context.BlogCategories.Add(newCat);
                    addedCategories.Add(newCat);
                    _logger.LogInformation("BlogSeeder: Adding new category '{Name}'", name);
                }
                else
                {
                    // Update existing category with English fields if missing
                    if (string.IsNullOrEmpty(existingCat.NameEn))
                    {
                        existingCat.NameEn = nameEn;
                        existingCat.DescriptionEn = descriptionEn;
                        _logger.LogInformation("BlogSeeder: Updated category '{Name}' with English translations", name);
                    }
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
                },
                // === NEW HIGH-VALUE SEO POSTS ===
                new BlogPost
                {
                    Title = "Cách Lên Xu Hướng TikTok 2026: 10 Bước Từ 0 Đến Triệu View",
                    Slug = "cach-len-xu-huong-tiktok-2026-10-buoc",
                    Excerpt = "Hướng dẫn chi tiết 10 bước để video TikTok của bạn lên xu hướng và đạt triệu view. Áp dụng ngay để viral!",
                    FeaturedImage = "https://images.unsplash.com/photo-1611162617474-5b21e879e113?w=1200&q=80",
                    Content = @"<h2>Bí Quyết Lên Xu Hướng TikTok 2026</h2>
<p>Bạn muốn video TikTok của mình viral và đạt hàng triệu view? Đây là 10 bước chi tiết mà các TikToker triệu follow đang áp dụng để liên tục lên xu hướng.</p>

<h2>Bước 1: Nghiên Cứu Trend Hiện Tại</h2>
<p>Trước khi quay video, hãy dành 15-20 phút lướt FYP (For You Page) để nắm bắt những trend đang hot. Chú ý đến:</p>
<ul>
<li>Âm nhạc đang được sử dụng nhiều</li>
<li>Hiệu ứng (effects) đang viral</li>
<li>Format video đang được ưa chuộng</li>
<li>Hashtag trending</li>
</ul>
<p>Sử dụng <a href='/'>TrendTag</a> để xem danh sách hashtag trending cập nhật mỗi 6 giờ.</p>

<h2>Bước 2: Chọn Niche Phù Hợp</h2>
<p>Đừng cố gắng làm mọi thứ! Chọn 1-2 chủ đề chính và tập trung vào đó:</p>
<ul>
<li><strong>Làm đẹp:</strong> Skincare, makeup tutorials</li>
<li><strong>Ẩm thực:</strong> Nấu ăn, review đồ ăn</li>
<li><strong>Giáo dục:</strong> Học tiếng Anh, kiến thức</li>
<li><strong>Giải trí:</strong> Hài hước, skits, reactions</li>
<li><strong>Lifestyle:</strong> Vlog, daily routine</li>
</ul>

<h2>Bước 3: Tối Ưu 3 Giây Đầu Tiên</h2>
<p>TikTok algorithm đánh giá video dựa trên retention rate. <strong>3 giây đầu tiên quyết định 80% thành công!</strong></p>
<p>Mẹo tối ưu 3 giây đầu:</p>
<ul>
<li>Bắt đầu với một câu gây sốc hoặc câu hỏi</li>
<li>Hiển thị kết quả cuối cùng trước (kiểu before-after ngược)</li>
<li>Sử dụng text hook trên màn hình</li>
<li>Âm thanh phải thu hút ngay từ đầu</li>
</ul>

<h2>Bước 4: Sử Dụng Hashtag Thông Minh</h2>
<p>Đây là bước quan trọng nhất! Sử dụng công thức 5-7 hashtag:</p>
<ul>
<li><strong>2 hashtag trending:</strong> #xuhuong #viral</li>
<li><strong>2-3 hashtag niche:</strong> Liên quan đến nội dung cụ thể</li>
<li><strong>1-2 hashtag có mức độ cạnh tranh thấp:</strong> Để dễ viral hơn</li>
</ul>
<p>Sử dụng <a href='/hashtag/tao-hashtag-ai'>Tạo Hashtag AI</a> để nhận gợi ý hashtag thông minh dựa trên nội dung video.</p>

<h2>Bước 5: Đăng Vào Giờ Vàng</h2>
<p>Thời điểm đăng video ảnh hưởng lớn đến reach. Giờ vàng tại Việt Nam:</p>
<ul>
<li><strong>Sáng:</strong> 7:00 - 9:00 (trước khi đi làm/học)</li>
<li><strong>Trưa:</strong> 11:30 - 13:30 (giờ nghỉ trưa)</li>
<li><strong>Tối:</strong> 19:00 - 22:00 (giờ vàng - cao điểm)</li>
<li><strong>Đêm khuya:</strong> 23:00 - 01:00 (ít cạnh tranh)</li>
</ul>

<h2>Bước 6: Tương Tác Ngay Sau Khi Đăng</h2>
<p>30 phút đầu sau khi đăng video là critical time. Hãy:</p>
<ul>
<li>Reply tất cả comments ngay lập tức</li>
<li>Pin comment hay nhất</li>
<li>Like comments của người xem</li>
<li>Tự comment với nội dung bổ sung</li>
</ul>
<p>Điều này báo hiệu cho algorithm rằng video đang có engagement cao.</p>

<h2>Bước 7: Sử Dụng Âm Thanh Trending</h2>
<p>Video sử dụng âm thanh trending có cơ hội lên FYP cao hơn 40%!</p>
<ul>
<li>Check Creative Center của TikTok để xem nhạc trending</li>
<li>Lưu âm thanh từ các video viral bạn thấy trên FYP</li>
<li>Sử dụng nhạc trong 24-48 giờ đầu khi nhạc mới trending</li>
</ul>

<h2>Bước 8: Tạo Series Và Hooks</h2>
<p>Tạo series video khiến người xem muốn follow để xem tiếp:</p>
<ul>
<li>""Phần 1/5: Bí mật skincare của tôi""</li>
<li>""Ngày 1 học tiếng Anh trong 30 ngày""</li>
<li>""POV: ..."" stories theo từng tập</li>
</ul>
<p>Kết thúc mỗi video với cliffhanger để người xem tò mò xem phần tiếp.</p>

<h2>Bước 9: Cross-Promote Trên Các Nền Tảng Khác</h2>
<p>Đừng chỉ đăng trên TikTok! Chia sẻ video lên:</p>
<ul>
<li>Instagram Reels</li>
<li>YouTube Shorts</li>
<li>Facebook Reels</li>
<li>Zalo stories</li>
</ul>
<p>Traffic từ các nền tảng khác giúp boost video trên TikTok.</p>

<h2>Bước 10: Phân Tích Và Tối Ưu Liên Tục</h2>
<p>Sử dụng TikTok Analytics để theo dõi:</p>
<ul>
<li><strong>Watch time:</strong> Người xem dừng lại ở đâu?</li>
<li><strong>Traffic sources:</strong> Views đến từ đâu?</li>
<li><strong>Hashtag performance:</strong> Hashtag nào mang lại views?</li>
<li><strong>Follower demographics:</strong> Đối tượng người xem là ai?</li>
</ul>
<p>Dùng <a href='/phan-tich/theo-doi-tang-truong'>Theo Dõi Tăng Trưởng</a> để phân tích hashtag nào đang trending và hiệu quả nhất.</p>

<h2>Kết Luận: Kiên Trì Là Chìa Khóa</h2>
<p>Không có video nào viral ngay từ đầu! Hầu hết TikToker triệu follow đều đăng 100-200 video trước khi có video đầu tiên viral.</p>
<p><strong>Công thức thành công = Nội dung chất lượng + Hashtag đúng + Thời điểm đăng tốt + Kiên trì</strong></p>
<p>Bắt đầu hành trình của bạn ngay hôm nay với <a href='/'>TrendTag</a> - công cụ tìm hashtag trending hàng đầu Việt Nam!</p>",
                    MetaTitle = "Cách Lên Xu Hướng TikTok 2026: 10 Bước Viral Triệu View",
                    MetaDescription = "Hướng dẫn chi tiết 10 bước để video TikTok lên xu hướng và viral triệu view. Bí quyết của TikToker chuyên nghiệp, áp dụng ngay 2026!",
                    MetaKeywords = "cách lên xu hướng tiktok, viral tiktok, triệu view tiktok, xu hướng tiktok 2026, cách làm video viral, tiktok trending",
                    Author = "TrendTag Team",
                    CategoryId = tiktokTipsCat.Id,
                    Status = "Published",
                    PublishedAt = DateTime.UtcNow,
                    ViewCount = 0,
                    CreatedAt = DateTime.UtcNow
                },
                new BlogPost
                {
                    Title = "Algorithm TikTok 2026: Cách FYP Hoạt Động Và Bí Quyết Lên For You Page",
                    Slug = "algorithm-tiktok-2026-cach-fyp-hoat-dong",
                    Excerpt = "Giải mã thuật toán TikTok 2026: Hiểu cách For You Page hoạt động để tối ưu video và tăng reach một cách khoa học.",
                    FeaturedImage = "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?w=1200&q=80",
                    Content = @"<h2>Giải Mã Algorithm TikTok 2026</h2>
<p>Thuật toán TikTok là ""bí ẩn"" mà mọi creator đều muốn hiểu. Bài viết này sẽ giải thích chi tiết cách TikTok algorithm hoạt động dựa trên các nghiên cứu và thử nghiệm thực tế.</p>

<h2>TikTok Algorithm Là Gì?</h2>
<p>Algorithm TikTok là hệ thống AI phức tạp quyết định video nào xuất hiện trên For You Page (FYP) của mỗi người dùng. Mục tiêu của thuật toán là:</p>
<ul>
<li>Giữ người dùng ở lại app lâu nhất có thể</li>
<li>Hiển thị nội dung phù hợp với sở thích từng người</li>
<li>Khuyến khích creator tạo nội dung chất lượng</li>
</ul>

<h2>4 Yếu Tố Chính Algorithm Đánh Giá</h2>

<h3>1. User Interactions (Tương Tác Người Dùng)</h3>
<p>TikTok theo dõi mọi hành động của người xem:</p>
<ul>
<li><strong>Likes:</strong> Video được like nhiều = chất lượng cao</li>
<li><strong>Comments:</strong> Nhiều comments = nội dung gây tranh luận/hấp dẫn</li>
<li><strong>Shares:</strong> Được chia sẻ = nội dung đáng xem</li>
<li><strong>Follows:</strong> Người xem follow sau khi xem = content hay</li>
<li><strong>Watch time:</strong> Xem hết video = nội dung thu hút</li>
<li><strong>Re-watches:</strong> Xem lại nhiều lần = viral potential cao</li>
</ul>

<h3>2. Video Information (Thông Tin Video)</h3>
<p>Algorithm phân tích nội dung video:</p>
<ul>
<li><strong>Captions:</strong> Nội dung mô tả, CTA</li>
<li><strong>Hashtags:</strong> Phân loại nội dung và chủ đề</li>
<li><strong>Sounds:</strong> Âm thanh/nhạc sử dụng</li>
<li><strong>Effects:</strong> Hiệu ứng và filters</li>
<li><strong>Video content:</strong> AI nhận dạng nội dung trong video</li>
</ul>

<h3>3. Device & Account Settings</h3>
<p>Các yếu tố cố định từ thiết bị:</p>
<ul>
<li>Ngôn ngữ cài đặt</li>
<li>Quốc gia/vùng địa lý</li>
<li>Loại thiết bị</li>
<li>Categories đã chọn khi đăng ký</li>
</ul>

<h3>4. Content Freshness (Độ Mới Của Nội Dung)</h3>
<p>Video mới được ưu tiên hơn video cũ. Đây là lý do TikTok khác biệt so với YouTube - nơi video cũ vẫn có thể viral.</p>

<h2>Cách FYP (For You Page) Hoạt Động</h2>

<h3>Giai Đoạn 1: Initial Push (30 phút đầu)</h3>
<p>Khi bạn đăng video, TikTok sẽ:</p>
<ul>
<li>Hiển thị video cho ~300-500 người dùng ngẫu nhiên</li>
<li>Đo lường các metrics: watch time, likes, comments, shares</li>
<li>Nếu performance tốt → chuyển sang giai đoạn 2</li>
</ul>

<h3>Giai Đoạn 2: Expanded Reach (1-4 giờ)</h3>
<p>Nếu video có engagement tốt:</p>
<ul>
<li>Mở rộng đến 1,000-5,000 viewers</li>
<li>Bắt đầu target theo interests/demographics</li>
<li>Tiếp tục đo lường performance</li>
</ul>

<h3>Giai Đoạn 3: Viral Phase (4-48 giờ)</h3>
<p>Video tiếp tục perform tốt:</p>
<ul>
<li>Push đến 10,000-100,000+ viewers</li>
<li>Xuất hiện trên FYP của nhiều quốc gia</li>
<li>Có thể viral lên đến hàng triệu views</li>
</ul>

<h2>Metrics Quan Trọng Nhất</h2>

<h3>Watch Time (Thời Gian Xem)</h3>
<p>Đây là metric QUAN TRỌNG NHẤT! TikTok ưu tiên video có:</p>
<ul>
<li><strong>Average watch time cao:</strong> Người xem xem hết hoặc gần hết video</li>
<li><strong>Completion rate > 100%:</strong> Người xem xem lại video</li>
</ul>
<p><em>Mẹo: Video ngắn (15-30s) dễ có completion rate cao hơn video dài.</em></p>

<h3>Engagement Rate</h3>
<p>Tỷ lệ tương tác = (Likes + Comments + Shares) / Views</p>
<ul>
<li><strong>> 10%:</strong> Xuất sắc, viral potential cao</li>
<li><strong>5-10%:</strong> Tốt</li>
<li><strong>2-5%:</strong> Trung bình</li>
<li><strong>< 2%:</strong> Cần cải thiện</li>
</ul>

<h2>Vai Trò Của Hashtag Trong Algorithm</h2>
<p>Hashtag giúp TikTok:</p>
<ul>
<li><strong>Phân loại nội dung:</strong> Biết video thuộc chủ đề gì</li>
<li><strong>Target đúng audience:</strong> Hiển thị cho người quan tâm đến chủ đề đó</li>
<li><strong>Tăng discoverability:</strong> Người dùng có thể tìm video qua hashtag</li>
</ul>
<p>Sử dụng <a href='/'>TrendTag</a> để tìm hashtag trending với mức độ cạnh tranh phù hợp.</p>

<h2>10 Mẹo Tối Ưu Cho Algorithm 2026</h2>

<h3>1. Hook Trong 1-3 Giây Đầu</h3>
<p>Algorithm đo lường drop-off rate. Nếu người xem scroll ngay trong 3 giây đầu, video sẽ không được đẩy tiếp.</p>

<h3>2. Độ Dài Video Tối Ưu</h3>
<ul>
<li><strong>15-30 giây:</strong> Tốt nhất cho người mới</li>
<li><strong>30-60 giây:</strong> Tốt cho storytelling</li>
<li><strong>1-3 phút:</strong> Cần nội dung rất hấp dẫn</li>
</ul>

<h3>3. Đăng Consistent</h3>
<p>Algorithm ưu tiên creators đăng đều đặn. Tối thiểu 1 video/ngày.</p>

<h3>4. Respond Nhanh Comments</h3>
<p>Reply comments trong 30 phút đầu giúp boost engagement.</p>

<h3>5. Sử Dụng Trending Sounds</h3>
<p>Videos với trending audio được push nhiều hơn 40%.</p>

<h3>6. Tránh Spam Hashtags</h3>
<p>5-7 hashtag có liên quan > 30 hashtag random.</p>

<h3>7. Tận Dụng Duet & Stitch</h3>
<p>Tương tác với videos viral giúp ""bám"" vào reach của video gốc.</p>

<h3>8. Đăng Đúng Giờ Vàng</h3>
<p>19:00-22:00 là peak time tại Việt Nam.</p>

<h3>9. Tạo Content Cho Rewatches</h3>
<p>Video có chi tiết ẩn hoặc twist cuối khiến người xem xem lại.</p>

<h3>10. A/B Test Liên Tục</h3>
<p>Thử nghiệm nhiều format, thời lượng, hashtags để tìm công thức hiệu quả.</p>

<h2>Kết Luận</h2>
<p>Algorithm TikTok 2026 ưu tiên <strong>watch time</strong> và <strong>engagement</strong>. Tập trung vào 2 yếu tố này, kết hợp với <a href='/hashtag/tao-hashtag-ai'>hashtag đúng</a>, sẽ giúp video của bạn reach cao hơn đáng kể.</p>
<p>Theo dõi hashtag trending mỗi ngày tại <a href='/'>TrendTag</a> để luôn cập nhật xu hướng mới nhất!</p>",
                    MetaTitle = "Algorithm TikTok 2026: Cách FYP Hoạt Động - Bí Quyết Viral",
                    MetaDescription = "Giải mã thuật toán TikTok 2026: Hiểu cách For You Page (FYP) hoạt động để tối ưu video và tăng triệu view một cách khoa học.",
                    MetaKeywords = "algorithm tiktok, thuật toán tiktok, fyp tiktok, for you page, cách lên fyp, tiktok 2026, viral tiktok",
                    Author = "TrendTag Team",
                    CategoryId = tiktokTipsCat.Id,
                    Status = "Published",
                    PublishedAt = DateTime.UtcNow.AddHours(-12),
                    ViewCount = 0,
                    CreatedAt = DateTime.UtcNow.AddHours(-12)
                },
                new BlogPost
                {
                    Title = "Hashtag TikTok Việt Nam: Danh Sách 500+ Hashtag Theo Chủ Đề [Cập Nhật 2026]",
                    Slug = "hashtag-tiktok-viet-nam-danh-sach-500-hashtag",
                    Excerpt = "Tổng hợp 500+ hashtag TikTok phổ biến nhất tại Việt Nam, phân loại theo 16 chủ đề. Copy và sử dụng ngay!",
                    FeaturedImage = "https://images.unsplash.com/photo-1611162618071-b39a2ec055fb?w=1200&q=80",
                    Content = @"<h2>Danh Sách Hashtag TikTok Việt Nam Đầy Đủ Nhất</h2>
<p>Đây là bộ sưu tập 500+ hashtag TikTok phổ biến nhất tại Việt Nam, được phân loại theo 16 chủ đề. Bookmark trang này để sử dụng mỗi khi đăng video!</p>
<p><strong>Mẹo:</strong> Sử dụng <a href='/'>TrendTag</a> để xem hashtag nào đang trending NGAY LÚC NÀY với metrics chi tiết.</p>

<h2>1. Hashtag Chung (General)</h2>
<p>Sử dụng cho mọi loại video:</p>
<p><code>#fyp #foryou #viral #xuhuong #trending #tiktokvietnam #tiktok #fypシ #foryoupage #viraltiktok #xuhuongtiktok #vietnam #viralvideo #fyppage #trend</code></p>

<h2>2. Hashtag Làm Đẹp & Skincare</h2>
<p>Dành cho content beauty:</p>
<p><code>#lamdep #skincare #makeup #beauty #skincareroutine #beautytiktok #reviewmypham #mypham #duongda #serum #cushion #sonnuoc #trangdiem #kbeauty #beautyhacks #skincaretips #makeuptutorial #beautyvietnam #chamsocda #glowyskin</code></p>
<p><a href='/chu-de/lam-dep-cham-soc-ca-nhan'>Xem hashtag Làm Đẹp trending →</a></p>

<h2>3. Hashtag Ẩm Thực & Nấu Ăn</h2>
<p>Dành cho food content:</p>
<p><code>#amthuc #nauanuon #food #cooking #recipe #foodtiktok #reviewdoan #ancungtiktok #nauan #monngon #foodie #asmrfood #streetfood #doannhanh #banhmi #pho #comtam #monviet #foodreview #nauanchongoi</code></p>
<p><a href='/chu-de/do-an-thuc-pham'>Xem hashtag Ẩm Thực trending →</a></p>

<h2>4. Hashtag Du Lịch</h2>
<p>Dành cho travel content:</p>
<p><code>#dulich #travel #dulichtiktok #vietnam #traveling #vacation #dulichvietnam #travelvietnam #phuquoc #dalat #danang #nhatrang #saigon #hanoi #wanderlust #travelgram #dulichbui #checkin #khamspha #cantho</code></p>
<p><a href='/chu-de/du-lich'>Xem hashtag Du Lịch trending →</a></p>

<h2>5. Hashtag Thời Trang</h2>
<p>Dành cho fashion content:</p>
<p><code>#thoitrang #fashion #ootd #style #outfitoftheday #fashiontiktok #streetstyle #outfit #thoitrangnam #thoitrangnu #phukien #giay #tui #vaydep #aodep #fashionvietnam #mixdo #fashionstyle #lookbook #styleinspiration</code></p>
<p><a href='/chu-de/thoi-trang-phu-kien'>Xem hashtag Thời Trang trending →</a></p>

<h2>6. Hashtag Giáo Dục & Học Tập</h2>
<p>Dành cho educational content:</p>
<p><code>#giaoduc #hoctap #learntiktok #edutok #hoctienganh #english #study #knowledge #hocbai #kienthuc #hoctienghan #learnenglish #studytok #hocthuattinh #mathematic #hocgioi #onthi #luyenthi #tips #studywithme</code></p>
<p><a href='/chu-de/giao-duc'>Xem hashtag Giáo Dục trending →</a></p>

<h2>7. Hashtag Công Nghệ</h2>
<p>Dành cho tech content:</p>
<p><code>#congnghe #tech #technology #iphone #samsung #laptop #smartphone #review #unboxing #techtiktok #gadget #apple #android #gametech #dienthoai #maytinhxachtay #phuliencongnghe #setup #techreview #tinhocvanphong</code></p>
<p><a href='/chu-de/cong-nghe'>Xem hashtag Công Nghệ trending →</a></p>

<h2>8. Hashtag Thể Thao & Gym</h2>
<p>Dành cho fitness content:</p>
<p><code>#thethao #gym #fitness #workout #tapgym #sport #bongda #football #basketball #gymtiktok #fitnesstiktok #motivation #gymmotivation #tapluyen #healthylifestyle #bodybuilding #cardio #weightloss #giamcan #yogaeveryday</code></p>
<p><a href='/chu-de/the-thao-ngoai-troi'>Xem hashtag Thể Thao trending →</a></p>

<h2>9. Hashtag Giải Trí & Hài Hước</h2>
<p>Dành cho entertainment:</p>
<p><code>#giaitri #hai #funny #comedy #haihuoc #troll #meme #cuoi #haivl #entertainment #funnyvideo #sketch #reaction #prank #skit #trending #viral #tiktokhaihuoc #haivietnam #comevn</code></p>
<p><a href='/chu-de/tin-tuc-giai-tri'>Xem hashtag Giải Trí trending →</a></p>

<h2>10. Hashtag Âm Nhạc</h2>
<p>Dành cho music content:</p>
<p><code>#nhac #music #cover #sing #singing #nhachay #vpop #kpop #guitar #piano #musictiktok #song #musician #singer #vocalist #nhactre #nhacviet #nhacremix #dj #edm</code></p>

<h2>11. Hashtag Gaming</h2>
<p>Dành cho gaming content:</p>
<p><code>#game #gaming #gamer #esports #lienquan #freefire #pubg #valorant #leagueoflegends #gamingtiktok #mobilegame #pcgame #streamer #gameplay #gamevietnam #gamemobile #genshinimpact #minecraft #roblox #callofduty</code></p>

<h2>12. Hashtag Thú Cưng</h2>
<p>Dành cho pet content:</p>
<p><code>#thucung #pet #dog #cat #cho #meo #cute #pettiktok #puppy #kitten #petlover #adorable #cutepet #animalslover #dogsoftiktok #catsoftiktok #choconmeo #bosuo #hamster #cunchim</code></p>

<h2>13. Hashtag Tài Chính & Kinh Doanh</h2>
<p>Dành cho finance/business content:</p>
<p><code>#taichinh #kinhdoanh #dautu #finance #business #money #investing #chungkhoan #tietkiem #lamgiau #startup #entrepreneur #marketing #sales #tradingview #crypto #bitcoin #forex #realestate #batdongsan</code></p>

<h2>14. Hashtag Sức Khỏe</h2>
<p>Dành cho health content:</p>
<p><code>#suckhoe #health #healthy #wellness #healthcare #suckhoetinhthan #mentalhealth #doctor #bacsi #dinh duong #nutrition #vitamin #giamcan #weightloss #yoga #meditation #selfcare #healthylifestyle #detox #thucphamchucnang</code></p>

<h2>15. Hashtag Parenting & Gia Đình</h2>
<p>Dành cho family content:</p>
<p><code>#giadinh #family #baby #em be #mevabe #parenting #babylove #newborn #toddler #kids #momlife #dadlife #familytime #lamme #lamcha #nuoicon #treviet #concungme #familytiktok #babylaugh</code></p>

<h2>16. Hashtag Lifestyle & Vlog</h2>
<p>Dành cho daily life content:</p>
<p><code>#lifestyle #vlog #dailylife #cuocsong #motngay #routine #morningroutine #nightroutine #aesthetic #minimalist #lifestyletiktok #dayinmylife #vlogger #vlognhatky #checkin #ootd #roomtour #haul #unboxing #whatieatinaday</code></p>

<h2>Cách Sử Dụng Hashtag Hiệu Quả</h2>
<p>Đừng copy tất cả hashtag! Hãy chọn 5-7 hashtag theo công thức:</p>
<ul>
<li><strong>2 hashtag trending:</strong> #fyp #xuhuong</li>
<li><strong>2-3 hashtag chủ đề:</strong> Phù hợp với nội dung video</li>
<li><strong>1-2 hashtag niche:</strong> Cụ thể và ít cạnh tranh</li>
</ul>

<h2>Công Cụ Tìm Hashtag Trending</h2>
<p>Danh sách trên là hashtag phổ biến, nhưng hashtag TRENDING thay đổi mỗi ngày!</p>
<p>Sử dụng các công cụ của <a href='/'>TrendTag</a> để:</p>
<ul>
<li><a href='/'>Xem Top 100 Hashtag Trending</a> - Cập nhật mỗi 6 giờ</li>
<li><a href='/hashtag/tao-hashtag-ai'>Tạo Hashtag AI</a> - Gợi ý hashtag dựa trên nội dung</li>
<li><a href='/phan-tich/theo-doi-tang-truong'>Theo Dõi Tăng Trưởng</a> - Phân tích xu hướng hashtag</li>
</ul>

<h2>Lưu Ý Quan Trọng</h2>
<ul>
<li><strong>Không spam hashtag:</strong> TikTok có thể giảm reach nếu bạn dùng quá nhiều hashtag</li>
<li><strong>Hashtag phải liên quan:</strong> Sử dụng hashtag không liên quan sẽ bị phạt</li>
<li><strong>Cập nhật thường xuyên:</strong> Hashtag trending thay đổi liên tục</li>
<li><strong>Test và đo lường:</strong> Dùng TikTok Analytics để xem hashtag nào hiệu quả</li>
</ul>

<p><strong>Bookmark trang này và quay lại mỗi khi cần hashtag cho video mới!</strong></p>",
                    MetaTitle = "500+ Hashtag TikTok Việt Nam Theo Chủ Đề [2026] - Copy Ngay!",
                    MetaDescription = "Danh sách 500+ hashtag TikTok Việt Nam đầy đủ nhất, phân loại theo 16 chủ đề. Copy và sử dụng ngay cho video của bạn!",
                    MetaKeywords = "hashtag tiktok việt nam, hashtag tiktok, danh sách hashtag, hashtag theo chủ đề, hashtag trending việt nam, copy hashtag tiktok",
                    Author = "TrendTag Team",
                    CategoryId = hashtagTrendingCat.Id,
                    Status = "Published",
                    PublishedAt = DateTime.UtcNow.AddHours(-6),
                    ViewCount = 0,
                    CreatedAt = DateTime.UtcNow.AddHours(-6)
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
