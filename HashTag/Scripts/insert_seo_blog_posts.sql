-- Script to insert new SEO-optimized blog posts
-- Run this on production database to add new content
-- Date: 2026-01-31

-- First, ensure the categories exist
IF NOT EXISTS (SELECT 1 FROM BlogCategories WHERE Slug = 'meo-tiktok')
BEGIN
    INSERT INTO BlogCategories (Name, DisplayNameVi, Slug, Description, IsActive)
    VALUES ('TikTok Tips', N'Mẹo TikTok', 'meo-tiktok', N'Tips và chiến lược tăng trưởng trên TikTok', 1);
END

IF NOT EXISTS (SELECT 1 FROM BlogCategories WHERE Slug = 'phan-tich-trending')
BEGIN
    INSERT INTO BlogCategories (Name, DisplayNameVi, Slug, Description, IsActive)
    VALUES ('Hashtag Trending', N'Phân Tích Trending', 'phan-tich-trending', N'Phân tích các hashtag trending mới nhất', 1);
END

-- Get category IDs
DECLARE @TikTokTipsCatId INT = (SELECT Id FROM BlogCategories WHERE Slug = 'meo-tiktok');
DECLARE @HashtagTrendingCatId INT = (SELECT Id FROM BlogCategories WHERE Slug = 'phan-tich-trending');

-- Insert Blog Post 1: Cách Lên Xu Hướng TikTok
IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'cach-len-xu-huong-tiktok-2026-10-buoc')
BEGIN
    INSERT INTO BlogPosts (
        Title, Slug, Excerpt, FeaturedImage, Content,
        MetaTitle, MetaDescription, MetaKeywords,
        Author, CategoryId, Status, PublishedAt, ViewCount, CreatedAt
    )
    VALUES (
        N'Cách Lên Xu Hướng TikTok 2026: 10 Bước Từ 0 Đến Triệu View',
        'cach-len-xu-huong-tiktok-2026-10-buoc',
        N'Hướng dẫn chi tiết 10 bước để video TikTok của bạn lên xu hướng và đạt triệu view. Áp dụng ngay để viral!',
        'https://images.unsplash.com/photo-1611162617474-5b21e879e113?w=1200&q=80',
        N'<h2>Bí Quyết Lên Xu Hướng TikTok 2026</h2>
<p>Bạn muốn video TikTok của mình viral và đạt hàng triệu view? Đây là 10 bước chi tiết mà các TikToker triệu follow đang áp dụng để liên tục lên xu hướng.</p>

<h2>Bước 1: Nghiên Cứu Trend Hiện Tại</h2>
<p>Trước khi quay video, hãy dành 15-20 phút lướt FYP (For You Page) để nắm bắt những trend đang hot. Chú ý đến:</p>
<ul>
<li>Âm nhạc đang được sử dụng nhiều</li>
<li>Hiệu ứng (effects) đang viral</li>
<li>Format video đang được ưa chuộng</li>
<li>Hashtag trending</li>
</ul>
<p>Sử dụng <a href=''/''>TrendTag</a> để xem danh sách hashtag trending cập nhật mỗi 6 giờ.</p>

<h2>Bước 2: Chọn Niche Phù Hợp</h2>
<p>Đừng cố gắng làm mọi thứ! Chọn 1-2 chủ đề chính và tập trung vào đó.</p>

<h2>Bước 3: Tối Ưu 3 Giây Đầu Tiên</h2>
<p>TikTok algorithm đánh giá video dựa trên retention rate. <strong>3 giây đầu tiên quyết định 80% thành công!</strong></p>

<h2>Bước 4: Sử Dụng Hashtag Thông Minh</h2>
<p>Đây là bước quan trọng nhất! Sử dụng công thức 5-7 hashtag. Sử dụng <a href=''/hashtag/tao-hashtag-ai''>Tạo Hashtag AI</a> để nhận gợi ý hashtag thông minh.</p>

<h2>Bước 5: Đăng Vào Giờ Vàng</h2>
<p>Giờ vàng tại Việt Nam: 7:00-9:00, 11:30-13:30, 19:00-22:00</p>

<h2>Bước 6: Tương Tác Ngay Sau Khi Đăng</h2>
<p>30 phút đầu sau khi đăng video là critical time.</p>

<h2>Bước 7: Sử Dụng Âm Thanh Trending</h2>
<p>Video sử dụng âm thanh trending có cơ hội lên FYP cao hơn 40%!</p>

<h2>Bước 8: Tạo Series Và Hooks</h2>
<p>Tạo series video khiến người xem muốn follow để xem tiếp.</p>

<h2>Bước 9: Cross-Promote Trên Các Nền Tảng Khác</h2>
<p>Chia sẻ video lên Instagram Reels, YouTube Shorts, Facebook Reels.</p>

<h2>Bước 10: Phân Tích Và Tối Ưu Liên Tục</h2>
<p>Dùng <a href=''/phan-tich/theo-doi-tang-truong''>Theo Dõi Tăng Trưởng</a> để phân tích hashtag nào đang trending.</p>

<h2>Kết Luận: Kiên Trì Là Chìa Khóa</h2>
<p><strong>Công thức thành công = Nội dung chất lượng + Hashtag đúng + Thời điểm đăng tốt + Kiên trì</strong></p>',
        N'Cách Lên Xu Hướng TikTok 2026: 10 Bước Viral Triệu View',
        N'Hướng dẫn chi tiết 10 bước để video TikTok lên xu hướng và viral triệu view. Bí quyết của TikToker chuyên nghiệp, áp dụng ngay 2026!',
        N'cách lên xu hướng tiktok, viral tiktok, triệu view tiktok, xu hướng tiktok 2026, cách làm video viral, tiktok trending',
        'TrendTag Team',
        @TikTokTipsCatId,
        'Published',
        GETUTCDATE(),
        0,
        GETUTCDATE()
    );
    PRINT 'Inserted: Cách Lên Xu Hướng TikTok 2026';
END

-- Insert Blog Post 2: Algorithm TikTok 2026
IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'algorithm-tiktok-2026-cach-fyp-hoat-dong')
BEGIN
    INSERT INTO BlogPosts (
        Title, Slug, Excerpt, FeaturedImage, Content,
        MetaTitle, MetaDescription, MetaKeywords,
        Author, CategoryId, Status, PublishedAt, ViewCount, CreatedAt
    )
    VALUES (
        N'Algorithm TikTok 2026: Cách FYP Hoạt Động Và Bí Quyết Lên For You Page',
        'algorithm-tiktok-2026-cach-fyp-hoat-dong',
        N'Giải mã thuật toán TikTok 2026: Hiểu cách For You Page hoạt động để tối ưu video và tăng reach một cách khoa học.',
        'https://images.unsplash.com/photo-1516321318423-f06f85e504b3?w=1200&q=80',
        N'<h2>Giải Mã Algorithm TikTok 2026</h2>
<p>Thuật toán TikTok là "bí ẩn" mà mọi creator đều muốn hiểu. Bài viết này sẽ giải thích chi tiết cách TikTok algorithm hoạt động.</p>

<h2>TikTok Algorithm Là Gì?</h2>
<p>Algorithm TikTok là hệ thống AI phức tạp quyết định video nào xuất hiện trên For You Page (FYP) của mỗi người dùng.</p>

<h2>4 Yếu Tố Chính Algorithm Đánh Giá</h2>
<h3>1. User Interactions</h3>
<p>TikTok theo dõi: Likes, Comments, Shares, Follows, Watch time, Re-watches</p>

<h3>2. Video Information</h3>
<p>Algorithm phân tích: Captions, Hashtags, Sounds, Effects, Video content</p>

<h3>3. Device & Account Settings</h3>
<p>Ngôn ngữ, Quốc gia, Loại thiết bị</p>

<h3>4. Content Freshness</h3>
<p>Video mới được ưu tiên hơn video cũ.</p>

<h2>Cách FYP Hoạt Động</h2>
<p>Giai đoạn 1: Initial Push (30 phút đầu) - 300-500 viewers</p>
<p>Giai đoạn 2: Expanded Reach (1-4 giờ) - 1,000-5,000 viewers</p>
<p>Giai đoạn 3: Viral Phase (4-48 giờ) - 10,000-100,000+ viewers</p>

<h2>Metrics Quan Trọng Nhất</h2>
<p><strong>Watch Time</strong> là metric QUAN TRỌNG NHẤT!</p>
<p><strong>Engagement Rate</strong> > 10% = Xuất sắc, 5-10% = Tốt</p>

<h2>Vai Trò Của Hashtag</h2>
<p>Sử dụng <a href=''/''>TrendTag</a> để tìm hashtag trending với mức độ cạnh tranh phù hợp.</p>

<h2>Kết Luận</h2>
<p>Algorithm TikTok 2026 ưu tiên <strong>watch time</strong> và <strong>engagement</strong>.</p>',
        N'Algorithm TikTok 2026: Cách FYP Hoạt Động - Bí Quyết Viral',
        N'Giải mã thuật toán TikTok 2026: Hiểu cách For You Page (FYP) hoạt động để tối ưu video và tăng triệu view một cách khoa học.',
        N'algorithm tiktok, thuật toán tiktok, fyp tiktok, for you page, cách lên fyp, tiktok 2026, viral tiktok',
        'TrendTag Team',
        @TikTokTipsCatId,
        'Published',
        DATEADD(HOUR, -12, GETUTCDATE()),
        0,
        DATEADD(HOUR, -12, GETUTCDATE())
    );
    PRINT 'Inserted: Algorithm TikTok 2026';
END

-- Insert Blog Post 3: Hashtag TikTok Việt Nam
IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'hashtag-tiktok-viet-nam-danh-sach-500-hashtag')
BEGIN
    INSERT INTO BlogPosts (
        Title, Slug, Excerpt, FeaturedImage, Content,
        MetaTitle, MetaDescription, MetaKeywords,
        Author, CategoryId, Status, PublishedAt, ViewCount, CreatedAt
    )
    VALUES (
        N'Hashtag TikTok Việt Nam: Danh Sách 500+ Hashtag Theo Chủ Đề [Cập Nhật 2026]',
        'hashtag-tiktok-viet-nam-danh-sach-500-hashtag',
        N'Tổng hợp 500+ hashtag TikTok phổ biến nhất tại Việt Nam, phân loại theo 16 chủ đề. Copy và sử dụng ngay!',
        'https://images.unsplash.com/photo-1611162618071-b39a2ec055fb?w=1200&q=80',
        N'<h2>Danh Sách Hashtag TikTok Việt Nam Đầy Đủ Nhất</h2>
<p>Đây là bộ sưu tập 500+ hashtag TikTok phổ biến nhất tại Việt Nam, được phân loại theo 16 chủ đề.</p>

<h2>1. Hashtag Chung (General)</h2>
<p><code>#fyp #foryou #viral #xuhuong #trending #tiktokvietnam #tiktok #fypシ #foryoupage #viraltiktok</code></p>

<h2>2. Hashtag Làm Đẹp & Skincare</h2>
<p><code>#lamdep #skincare #makeup #beauty #skincareroutine #beautytiktok #reviewmypham #mypham #duongda</code></p>

<h2>3. Hashtag Ẩm Thực & Nấu Ăn</h2>
<p><code>#amthuc #nauanuon #food #cooking #recipe #foodtiktok #reviewdoan #ancungtiktok</code></p>

<h2>4. Hashtag Du Lịch</h2>
<p><code>#dulich #travel #dulichtiktok #vietnam #traveling #vacation #dulichvietnam #travelvietnam</code></p>

<h2>5. Hashtag Thời Trang</h2>
<p><code>#thoitrang #fashion #ootd #style #outfitoftheday #fashiontiktok #streetstyle</code></p>

<h2>6. Hashtag Giáo Dục & Học Tập</h2>
<p><code>#giaoduc #hoctap #learntiktok #edutok #hoctienganh #english #study #knowledge</code></p>

<h2>7. Hashtag Công Nghệ</h2>
<p><code>#congnghe #tech #technology #iphone #samsung #laptop #smartphone #review</code></p>

<h2>8. Hashtag Thể Thao & Gym</h2>
<p><code>#thethao #gym #fitness #workout #tapgym #sport #bongda #football</code></p>

<h2>Cách Sử Dụng Hashtag Hiệu Quả</h2>
<p>Chọn 5-7 hashtag theo công thức:</p>
<ul>
<li><strong>2 hashtag trending:</strong> #fyp #xuhuong</li>
<li><strong>2-3 hashtag chủ đề:</strong> Phù hợp với nội dung video</li>
<li><strong>1-2 hashtag niche:</strong> Cụ thể và ít cạnh tranh</li>
</ul>

<h2>Công Cụ Tìm Hashtag Trending</h2>
<p>Sử dụng các công cụ của <a href=''/''>TrendTag</a> để xem hashtag trending NGAY LÚC NÀY!</p>',
        N'500+ Hashtag TikTok Việt Nam Theo Chủ Đề [2026] - Copy Ngay!',
        N'Danh sách 500+ hashtag TikTok Việt Nam đầy đủ nhất, phân loại theo 16 chủ đề. Copy và sử dụng ngay cho video của bạn!',
        N'hashtag tiktok việt nam, hashtag tiktok, danh sách hashtag, hashtag theo chủ đề, hashtag trending việt nam, copy hashtag tiktok',
        'TrendTag Team',
        @HashtagTrendingCatId,
        'Published',
        DATEADD(HOUR, -6, GETUTCDATE()),
        0,
        DATEADD(HOUR, -6, GETUTCDATE())
    );
    PRINT 'Inserted: Hashtag TikTok Việt Nam';
END

PRINT 'Script completed successfully!';
