-- ================================================
-- Script: Seed Blog Posts with Images
-- Description: Adds more blog posts for each category
-- Run this script on the database to add blog posts
-- ================================================

-- First, ensure categories exist
IF NOT EXISTS (SELECT 1 FROM BlogCategories WHERE Slug = 'phan-tich-trending')
BEGIN
    INSERT INTO BlogCategories (Name, DisplayNameVi, Slug, Description, IsActive, CreatedAt)
    VALUES ('Hashtag Trending', N'Phân Tích Trending', 'phan-tich-trending', N'Phân tích các hashtag trending mới nhất', 1, GETUTCDATE())
END

IF NOT EXISTS (SELECT 1 FROM BlogCategories WHERE Slug = 'meo-tiktok')
BEGIN
    INSERT INTO BlogCategories (Name, DisplayNameVi, Slug, Description, IsActive, CreatedAt)
    VALUES ('TikTok Tips', N'Mẹo TikTok', 'meo-tiktok', N'Tips và chiến lược tăng trưởng trên TikTok', 1, GETUTCDATE())
END

IF NOT EXISTS (SELECT 1 FROM BlogCategories WHERE Slug = 'huong-dan-creator')
BEGIN
    INSERT INTO BlogCategories (Name, DisplayNameVi, Slug, Description, IsActive, CreatedAt)
    VALUES ('Creator Guide', N'Hướng Dẫn Creator', 'huong-dan-creator', N'Hướng dẫn cho TikTok creators', 1, GETUTCDATE())
END

-- Get category IDs
DECLARE @TrendingCatId INT = (SELECT Id FROM BlogCategories WHERE Slug = 'phan-tich-trending')
DECLARE @TipsCatId INT = (SELECT Id FROM BlogCategories WHERE Slug = 'meo-tiktok')
DECLARE @GuideCatId INT = (SELECT Id FROM BlogCategories WHERE Slug = 'huong-dan-creator')

-- ========== HASHTAG TRENDING CATEGORY ==========

-- Post 1: Top Hashtag Trending
IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'top-hashtag-tiktok-trending-thang-1-2026')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Excerpt, Content, FeaturedImage, MetaTitle, MetaDescription, MetaKeywords, Author, CategoryId, Status, PublishedAt, ViewCount, CreatedAt)
    VALUES (
        N'Top Hashtag TikTok Trending Tháng 1/2026',
        'top-hashtag-tiktok-trending-thang-1-2026',
        N'Danh sách top hashtag TikTok đang hot nhất tháng 1/2026. Phân tích chi tiết với metrics và tips sử dụng hiệu quả cho creators.',
        N'<h2>Top Hashtag TikTok Trending Tháng 1/2026</h2>
<p>Chào mừng năm mới 2026! Đây là danh sách những hashtag đang hot nhất trên TikTok trong tháng đầu tiên của năm. Việc sử dụng đúng hashtag trending có thể giúp video của bạn viral nhanh chóng.</p>

<h3>Top 10 Hashtag Trending Tuần Này</h3>
<ol>
<li><strong>#newyear2026</strong> - 2.5B views - Nội dung chào năm mới</li>
<li><strong>#2026goals</strong> - 1.8B views - Mục tiêu năm mới</li>
<li><strong>#wintervibes</strong> - 1.2B views - Nội dung mùa đông</li>
<li><strong>#fyp</strong> - Luôn trending - For You Page</li>
<li><strong>#viral2026</strong> - 800M views - Trending mới</li>
</ol>

<h3>Phân Tích Chi Tiết</h3>
<p>Tháng 1 là thời điểm tuyệt vời để sáng tạo nội dung về:</p>
<ul>
<li>Mục tiêu và kế hoạch năm mới</li>
<li>Review năm cũ, dự đoán năm mới</li>
<li>Thử thách đầu năm</li>
<li>Nội dung mùa lễ hội</li>
</ul>

<h3>Cách Sử Dụng Hiệu Quả</h3>
<p>Kết hợp 2-3 hashtag trending với 3-4 hashtag niche của bạn. Đừng quên theo dõi <a href="/">TrendTag</a> để cập nhật hashtag mới nhất hàng ngày!</p>

<p><em>Cập nhật lần cuối: Tháng 1/2026</em></p>',
        'https://images.unsplash.com/photo-1611162616305-c69b3fa7fbe0?w=1200&q=80',
        N'Top Hashtag TikTok Trending Tháng 1/2026 | TrendTag',
        N'Danh sách top hashtag TikTok trending tháng 1/2026 với metrics và phân tích chi tiết. Cập nhật hàng ngày.',
        N'hashtag trending 2026, top hashtag tiktok, hashtag thang 1, trending tiktok',
        'TrendTag Team',
        @TrendingCatId,
        'Published',
        DATEADD(DAY, -2, GETUTCDATE()),
        156,
        DATEADD(DAY, -2, GETUTCDATE())
    )
END

-- Post 2: Hashtag Beauty Trending
IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'hashtag-lam-dep-skincare-trending-2026')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Excerpt, Content, FeaturedImage, MetaTitle, MetaDescription, MetaKeywords, Author, CategoryId, Status, PublishedAt, ViewCount, CreatedAt)
    VALUES (
        N'Hashtag Làm Đẹp & Skincare Đang Trending 2026',
        'hashtag-lam-dep-skincare-trending-2026',
        N'Tổng hợp hashtag làm đẹp, skincare, makeup trending nhất trên TikTok 2026. Phù hợp cho beauty creators và brands.',
        N'<h2>Hashtag Làm Đẹp Trending 2026</h2>
<p>Ngành làm đẹp là một trong những niche lớn nhất trên TikTok. Dưới đây là những hashtag đang hot nhất cho beauty creators.</p>

<h3>Top Hashtag Skincare</h3>
<ul>
<li><strong>#skincare</strong> - 150B+ views - Hashtag chính</li>
<li><strong>#glowyskin</strong> - 25B views - Da sáng bóng</li>
<li><strong>#skincareRoutine</strong> - 40B views - Quy trình skincare</li>
<li><strong>#kbeauty</strong> - 20B views - Korean beauty</li>
<li><strong>#glassskin</strong> - 15B views - Da trong như thủy tinh</li>
</ul>

<h3>Top Hashtag Makeup</h3>
<ul>
<li><strong>#makeup</strong> - 200B+ views - Hashtag chính</li>
<li><strong>#makeuptutorial</strong> - 60B views - Hướng dẫn makeup</li>
<li><strong>#grwm</strong> - 80B views - Get Ready With Me</li>
<li><strong>#naturalmakeup</strong> - 30B views - Makeup tự nhiên</li>
<li><strong>#koreanmakeup</strong> - 25B views - Makeup Hàn Quốc</li>
</ul>

<h3>Tips Cho Beauty Creators</h3>
<p>Kết hợp hashtag lớn như #makeup với hashtag cụ thể như #cushionfoundation hoặc #lipcombo để tăng khả năng được đề xuất.</p>

<p>Sử dụng <a href="/hashtag-generator">Tạo Hashtag AI</a> để tìm hashtag phù hợp với nội dung cụ thể của bạn!</p>',
        'https://images.unsplash.com/photo-1596462502278-27bfdc403348?w=1200&q=80',
        N'Hashtag Làm Đẹp Skincare Trending 2026 | TrendTag',
        N'Tổng hợp hashtag làm đẹp, skincare, makeup trending TikTok 2026. Dành cho beauty creators.',
        N'hashtag lam dep, skincare trending, makeup hashtag, beauty tiktok',
        'TrendTag Team',
        @TrendingCatId,
        'Published',
        DATEADD(DAY, -5, GETUTCDATE()),
        234,
        DATEADD(DAY, -5, GETUTCDATE())
    )
END

-- Post 3: Hashtag Food Trending
IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'hashtag-am-thuc-food-tiktok-trending')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Excerpt, Content, FeaturedImage, MetaTitle, MetaDescription, MetaKeywords, Author, CategoryId, Status, PublishedAt, ViewCount, CreatedAt)
    VALUES (
        N'Hashtag Ẩm Thực & Food TikTok Đang Trending',
        'hashtag-am-thuc-food-tiktok-trending',
        N'Danh sách hashtag ẩm thực, nấu ăn, food review trending trên TikTok. Dành cho food bloggers và nhà hàng.',
        N'<h2>Hashtag Food TikTok Trending</h2>
<p>Food content là một trong những niche phổ biến nhất trên TikTok với hàng tỷ lượt xem mỗi ngày. Đây là những hashtag giúp video ẩm thực của bạn tiếp cận nhiều người hơn.</p>

<h3>Top Hashtag Food Chung</h3>
<ul>
<li><strong>#food</strong> - 300B+ views</li>
<li><strong>#foodtiktok</strong> - 50B views</li>
<li><strong>#yummy</strong> - 40B views</li>
<li><strong>#foodie</strong> - 80B views</li>
<li><strong>#delicious</strong> - 35B views</li>
</ul>

<h3>Hashtag Nấu Ăn</h3>
<ul>
<li><strong>#cooking</strong> - 100B views</li>
<li><strong>#recipe</strong> - 60B views</li>
<li><strong>#homemade</strong> - 25B views</li>
<li><strong>#easyrecipe</strong> - 20B views</li>
<li><strong>#cookingtiktok</strong> - 15B views</li>
</ul>

<h3>Hashtag Ẩm Thực Việt Nam</h3>
<ul>
<li><strong>#vietnamesefood</strong> - 5B views</li>
<li><strong>#pho</strong> - 3B views</li>
<li><strong>#banhmi</strong> - 2B views</li>
<li><strong>#streetfoodvietnam</strong> - 1B views</li>
</ul>

<h3>Mẹo Viral Food Content</h3>
<p>Video ASMR ăn uống, mukbang, và recipe nhanh (dưới 60s) thường có tỷ lệ viral cao. Đừng quên bật âm thanh sống động!</p>',
        'https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=1200&q=80',
        N'Hashtag Ẩm Thực Food TikTok Trending | TrendTag',
        N'Danh sách hashtag ẩm thực, food, nấu ăn trending TikTok. Dành cho food bloggers.',
        N'hashtag food, am thuc tiktok, food trending, cooking hashtag',
        'TrendTag Team',
        @TrendingCatId,
        'Published',
        DATEADD(DAY, -7, GETUTCDATE()),
        189,
        DATEADD(DAY, -7, GETUTCDATE())
    )
END

-- ========== TIKTOK TIPS CATEGORY ==========

-- Post 4: Thời điểm đăng video
IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'thoi-diem-dang-video-tiktok-tot-nhat')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Excerpt, Content, FeaturedImage, MetaTitle, MetaDescription, MetaKeywords, Author, CategoryId, Status, PublishedAt, ViewCount, CreatedAt)
    VALUES (
        N'Thời Điểm Đăng Video TikTok Tốt Nhất 2026',
        'thoi-diem-dang-video-tiktok-tot-nhat',
        N'Phân tích khung giờ vàng để đăng video TikTok. Tăng views và engagement bằng cách chọn đúng thời điểm.',
        N'<h2>Khung Giờ Vàng Đăng TikTok</h2>
<p>Thời điểm đăng video ảnh hưởng lớn đến số views ban đầu, quyết định video có được đẩy lên FYP hay không.</p>

<h3>Khung Giờ Tốt Nhất (Giờ Việt Nam)</h3>
<table>
<tr><th>Ngày</th><th>Khung giờ tốt</th><th>Khung giờ tốt nhất</th></tr>
<tr><td>Thứ 2-6</td><td>7h-9h, 12h-14h, 19h-22h</td><td>20h-21h</td></tr>
<tr><td>Thứ 7</td><td>9h-12h, 19h-23h</td><td>21h-22h</td></tr>
<tr><td>Chủ nhật</td><td>10h-13h, 18h-21h</td><td>19h-20h</td></tr>
</table>

<h3>Tại Sao Khung Giờ Quan Trọng?</h3>
<ul>
<li><strong>Engagement ban đầu:</strong> 1 giờ đầu quyết định video có viral không</li>
<li><strong>Algorithm:</strong> TikTok đánh giá video dựa trên tỷ lệ tương tác sớm</li>
<li><strong>Cạnh tranh:</strong> Ít video đăng = nhiều cơ hội được xem hơn</li>
</ul>

<h3>Mẹo Tối Ưu</h3>
<ol>
<li>Kiểm tra Analytics trong TikTok để xem audience của bạn online lúc nào</li>
<li>Test các khung giờ khác nhau trong 2 tuần</li>
<li>Đăng đều đặn cùng khung giờ để algorithm học được</li>
<li>Sử dụng tính năng Schedule để đăng đúng giờ</li>
</ol>

<p>Kết hợp với <a href="/">hashtag trending</a> để tăng hiệu quả tối đa!</p>',
        'https://images.unsplash.com/photo-1611162617474-5b21e879e113?w=1200&q=80',
        N'Thời Điểm Đăng Video TikTok Tốt Nhất 2026',
        N'Khung giờ vàng đăng video TikTok để tăng views. Phân tích chi tiết cho từng ngày trong tuần.',
        N'thoi diem dang tiktok, khung gio vang, best time post tiktok, tang views',
        'TrendTag Team',
        @TipsCatId,
        'Published',
        DATEADD(DAY, -4, GETUTCDATE()),
        312,
        DATEADD(DAY, -4, GETUTCDATE())
    )
END

-- Post 5: Tăng followers
IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'cach-tang-followers-tiktok-nhanh-2026')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Excerpt, Content, FeaturedImage, MetaTitle, MetaDescription, MetaKeywords, Author, CategoryId, Status, PublishedAt, ViewCount, CreatedAt)
    VALUES (
        N'Cách Tăng Followers TikTok Nhanh Chóng 2026',
        'cach-tang-followers-tiktok-nhanh-2026',
        N'10 cách tăng followers TikTok hiệu quả và bền vững. Không cần mua followers, chỉ cần chiến lược đúng.',
        N'<h2>10 Cách Tăng Followers TikTok 2026</h2>
<p>Muốn tăng followers nhanh mà không cần mua? Đây là 10 chiến lược đã được chứng minh hiệu quả.</p>

<h3>1. Đăng Video Đều Đặn</h3>
<p>Tối thiểu 1-2 video/ngày. Consistency là chìa khóa để algorithm ưu tiên bạn.</p>

<h3>2. Bắt Trend Nhanh</h3>
<p>Theo dõi <a href="/">TrendTag</a> để biết trend mới nhất. Làm video trend trong 24-48h đầu sẽ có nhiều views hơn.</p>

<h3>3. Hook Mạnh Trong 3 Giây Đầu</h3>
<p>3 giây đầu quyết định người xem có tiếp tục hay không. Bắt đầu với điều gây sốc, câu hỏi, hoặc hành động bất ngờ.</p>

<h3>4. Tương Tác Với Comments</h3>
<p>Reply comments trong 1 giờ đầu sau khi đăng. Điều này tăng engagement và khiến algorithm đẩy video.</p>

<h3>5. Sử Dụng Hashtag Đúng Cách</h3>
<p>5-7 hashtag là đủ. Mix giữa hashtag trending và hashtag niche. Dùng <a href="/hashtag-generator">Tạo Hashtag AI</a> để tìm hashtag phù hợp.</p>

<h3>6. Collab Với Creators Khác</h3>
<p>Duet, Stitch, hoặc collab video giúp tiếp cận audience của người khác.</p>

<h3>7. Tối Ưu Profile</h3>
<ul>
<li>Ảnh đại diện rõ mặt</li>
<li>Bio ngắn gọn, có CTA</li>
<li>Link đến các platform khác</li>
</ul>

<h3>8. Tạo Series Nội Dung</h3>
<p>Series khiến người xem follow để xem phần tiếp theo.</p>

<h3>9. Sử Dụng Âm Nhạc Trending</h3>
<p>Video dùng nhạc trending có tỷ lệ được đề xuất cao hơn 30%.</p>

<h3>10. Phân Tích và Tối Ưu</h3>
<p>Xem Analytics thường xuyên. Video nào hoạt động tốt? Làm thêm những video tương tự!</p>',
        'https://images.unsplash.com/photo-1611162616475-46b635cb6868?w=1200&q=80',
        N'Cách Tăng Followers TikTok Nhanh 2026',
        N'10 cách tăng followers TikTok hiệu quả, bền vững. Chiến lược đã được chứng minh.',
        N'tang followers tiktok, cach tang follow, tiktok growth, viral tiktok',
        'TrendTag Team',
        @TipsCatId,
        'Published',
        DATEADD(DAY, -6, GETUTCDATE()),
        456,
        DATEADD(DAY, -6, GETUTCDATE())
    )
END

-- Post 6: Algorithm TikTok
IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'cach-algorithm-tiktok-hoat-dong-2026')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Excerpt, Content, FeaturedImage, MetaTitle, MetaDescription, MetaKeywords, Author, CategoryId, Status, PublishedAt, ViewCount, CreatedAt)
    VALUES (
        N'Cách Algorithm TikTok Hoạt Động 2026',
        'cach-algorithm-tiktok-hoat-dong-2026',
        N'Hiểu rõ cách algorithm TikTok hoạt động để tối ưu video và tăng cơ hội viral. Cập nhật mới nhất 2026.',
        N'<h2>Algorithm TikTok 2026 Hoạt Động Như Thế Nào?</h2>
<p>Hiểu algorithm là chìa khóa để viral trên TikTok. Đây là những gì chúng ta biết về cách TikTok đề xuất video.</p>

<h3>Các Yếu Tố Algorithm Xem Xét</h3>

<h4>1. User Interactions (Tương tác người dùng)</h4>
<ul>
<li>Like, comment, share</li>
<li>Thời gian xem (Watch time)</li>
<li>Xem lại video</li>
<li>Follow sau khi xem</li>
<li>Đánh dấu "Không quan tâm"</li>
</ul>

<h4>2. Video Information (Thông tin video)</h4>
<ul>
<li>Caption và hashtag</li>
<li>Âm nhạc/âm thanh</li>
<li>Effects sử dụng</li>
<li>Chủ đề nội dung</li>
</ul>

<h4>3. Device & Account Settings</h4>
<ul>
<li>Ngôn ngữ</li>
<li>Quốc gia</li>
<li>Loại thiết bị</li>
<li>Sở thích đã chọn</li>
</ul>

<h3>Vòng Đời Của Video TikTok</h3>
<ol>
<li><strong>Batch 1:</strong> Video được test với 200-500 người</li>
<li><strong>Đánh giá:</strong> Nếu engagement tốt (>10% watch time), tiếp tục</li>
<li><strong>Batch 2:</strong> Mở rộng đến 1,000-5,000 người</li>
<li><strong>Viral:</strong> Nếu tiếp tục tốt, video được đẩy lên FYP rộng</li>
</ol>

<h3>Metrics Quan Trọng Nhất</h3>
<ol>
<li><strong>Watch Time:</strong> Quan trọng nhất! Người xem xem hết video = điểm cao</li>
<li><strong>Shares:</strong> Chia sẻ có giá trị cao hơn likes</li>
<li><strong>Comments:</strong> Comments dài có giá trị hơn emoji</li>
<li><strong>Replays:</strong> Xem lại = nội dung hấp dẫn</li>
</ol>

<h3>Cách Tối Ưu Cho Algorithm</h3>
<ul>
<li>Làm video ngắn (15-30s) có tỷ lệ xem hết cao hơn</li>
<li>Hook mạnh trong 1-3 giây đầu</li>
<li>Kết thúc với CTA (like, follow, comment)</li>
<li>Dùng <a href="/">hashtag trending</a> phù hợp</li>
<li>Đăng vào khung giờ vàng</li>
</ul>',
        'https://images.unsplash.com/photo-1432888498266-38ffec3eaf0a?w=1200&q=80',
        N'Cách Algorithm TikTok Hoạt Động 2026',
        N'Hiểu algorithm TikTok để tối ưu video. Cập nhật mới nhất về cách FYP hoạt động.',
        N'algorithm tiktok, fyp tiktok, cach viral, tiktok algorithm 2026',
        'TrendTag Team',
        @TipsCatId,
        'Published',
        DATEADD(DAY, -8, GETUTCDATE()),
        523,
        DATEADD(DAY, -8, GETUTCDATE())
    )
END

-- ========== CREATOR GUIDE CATEGORY ==========

-- Post 7: Bắt đầu làm creator
IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'huong-dan-bat-dau-lam-tiktok-creator')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Excerpt, Content, FeaturedImage, MetaTitle, MetaDescription, MetaKeywords, Author, CategoryId, Status, PublishedAt, ViewCount, CreatedAt)
    VALUES (
        N'Hướng Dẫn Bắt Đầu Làm TikTok Creator 2026',
        'huong-dan-bat-dau-lam-tiktok-creator',
        N'Hướng dẫn từ A-Z cho người mới bắt đầu làm TikTok creator. Setup, niche, content strategy và kiếm tiền.',
        N'<h2>Hướng Dẫn Làm TikTok Creator Từ Số 0</h2>
<p>Bạn muốn bắt đầu làm TikTok creator nhưng không biết bắt đầu từ đâu? Đây là hướng dẫn chi tiết từ A-Z.</p>

<h3>Bước 1: Chọn Niche</h3>
<p>Niche là lĩnh vực bạn sẽ tập trung sáng tạo nội dung. Một số niche phổ biến:</p>
<ul>
<li><strong>Giải trí:</strong> Hài, skits, lip-sync</li>
<li><strong>Giáo dục:</strong> Dạy kỹ năng, kiến thức</li>
<li><strong>Lifestyle:</strong> Cuộc sống hàng ngày, vlog</li>
<li><strong>Làm đẹp:</strong> Makeup, skincare, fashion</li>
<li><strong>Ẩm thực:</strong> Nấu ăn, review đồ ăn</li>
<li><strong>Gaming:</strong> Gameplay, tips game</li>
</ul>

<h3>Bước 2: Setup Cơ Bản</h3>
<ul>
<li><strong>Điện thoại:</strong> Camera tốt (iPhone hoặc flagship Android)</li>
<li><strong>Ánh sáng:</strong> Ring light hoặc ánh sáng tự nhiên</li>
<li><strong>Âm thanh:</strong> Micro không dây (nếu quay voiceover)</li>
<li><strong>Background:</strong> Gọn gàng, phù hợp niche</li>
</ul>

<h3>Bước 3: Tối Ưu Profile</h3>
<ul>
<li>Username dễ nhớ, liên quan đến niche</li>
<li>Ảnh đại diện rõ mặt hoặc logo</li>
<li>Bio ngắn gọn, có CTA</li>
<li>Link đến các platform khác</li>
</ul>

<h3>Bước 4: Content Strategy</h3>
<ol>
<li>Đăng 1-3 video/ngày trong tháng đầu</li>
<li>Theo dõi <a href="/">hashtag trending</a> hàng ngày</li>
<li>Phân tích video nào hoạt động tốt</li>
<li>Làm thêm những video tương tự</li>
</ol>

<h3>Bước 5: Kiếm Tiền</h3>
<p>Sau khi đạt 10,000 followers + 100,000 views, bạn có thể:</p>
<ul>
<li>Tham gia TikTok Creator Fund</li>
<li>Nhận sponsorship từ brands</li>
<li>Bán sản phẩm/dịch vụ riêng</li>
<li>Affiliate marketing</li>
<li>Live streaming và nhận gifts</li>
</ul>

<h3>Tips Quan Trọng</h3>
<ul>
<li>Consistency quan trọng hơn perfection</li>
<li>Đừng bỏ cuộc sau 10-20 video đầu tiên</li>
<li>Học từ top creators trong niche của bạn</li>
<li>Tương tác với community</li>
</ul>',
        'https://images.unsplash.com/photo-1533227268428-f9ed0900fb3b?w=1200&q=80',
        N'Hướng Dẫn Bắt Đầu Làm TikTok Creator 2026',
        N'Hướng dẫn từ A-Z cho người mới làm TikTok creator. Setup, niche, content và kiếm tiền.',
        N'tiktok creator, huong dan tiktok, bat dau lam tiktok, kiem tien tiktok',
        'TrendTag Team',
        @GuideCatId,
        'Published',
        DATEADD(DAY, -3, GETUTCDATE()),
        678,
        DATEADD(DAY, -3, GETUTCDATE())
    )
END

-- Post 8: Thiết bị quay video
IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'thiet-bi-quay-video-tiktok-tot-nhat')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Excerpt, Content, FeaturedImage, MetaTitle, MetaDescription, MetaKeywords, Author, CategoryId, Status, PublishedAt, ViewCount, CreatedAt)
    VALUES (
        N'Thiết Bị Quay Video TikTok Tốt Nhất 2026',
        'thiet-bi-quay-video-tiktok-tot-nhat',
        N'Review và so sánh các thiết bị quay video TikTok từ cơ bản đến chuyên nghiệp. Phù hợp mọi ngân sách.',
        N'<h2>Thiết Bị Cần Thiết Cho TikToker</h2>
<p>Thiết bị tốt giúp nâng cao chất lượng video, nhưng không phải lúc nào cũng cần đắt tiền. Đây là guide đầy đủ.</p>

<h3>1. Điện Thoại (Camera)</h3>

<h4>Ngân sách thấp (dưới 10 triệu)</h4>
<ul>
<li>Samsung Galaxy A54 - Camera tốt, giá hợp lý</li>
<li>Xiaomi 13T - Chất lượng video 4K</li>
<li>iPhone SE 2022 - Compact, camera tốt</li>
</ul>

<h4>Ngân sách trung bình (10-20 triệu)</h4>
<ul>
<li>iPhone 14 - Tiêu chuẩn cho creators</li>
<li>Samsung S23 - Video stabilization tốt</li>
<li>Google Pixel 8 - Màu sắc tự nhiên</li>
</ul>

<h4>Chuyên nghiệp (trên 20 triệu)</h4>
<ul>
<li>iPhone 15 Pro Max - Cinematic mode, Action mode</li>
<li>Samsung S24 Ultra - Zoom tốt, 8K video</li>
</ul>

<h3>2. Ánh Sáng</h3>
<ul>
<li><strong>Ring Light 26cm:</strong> 200-500K - Đủ cho mặt</li>
<li><strong>Ring Light 45cm:</strong> 500K-1tr - Full body</li>
<li><strong>Softbox:</strong> 1-2tr - Chuyên nghiệp hơn</li>
</ul>

<h3>3. Âm Thanh</h3>
<ul>
<li><strong>Boya BY-M1:</strong> 300K - Mic cài áo cơ bản</li>
<li><strong>Rode Wireless Go:</strong> 3-5tr - Không dây, chất lượng cao</li>
<li><strong>DJI Mic:</strong> 7-10tr - Professional</li>
</ul>

<h3>4. Tripod & Gimbal</h3>
<ul>
<li><strong>Tripod điện thoại:</strong> 100-300K - Cơ bản</li>
<li><strong>DJI OM 6:</strong> 3-4tr - Gimbal smartphone tốt nhất</li>
<li><strong>Zhiyun Smooth 5:</strong> 3tr - Alternative tốt</li>
</ul>

<h3>Bắt Đầu Với Bao Nhiêu?</h3>
<p>Bạn có thể bắt đầu với điện thoại hiện tại + ánh sáng tự nhiên. Upgrade dần khi có thu nhập từ TikTok!</p>',
        'https://images.unsplash.com/photo-1598327105666-5b89351aff97?w=1200&q=80',
        N'Thiết Bị Quay Video TikTok Tốt Nhất 2026',
        N'Review thiết bị quay video TikTok: điện thoại, đèn, mic, tripod. Phù hợp mọi ngân sách.',
        N'thiet bi quay tiktok, camera tiktok, ring light, mic tiktok',
        'TrendTag Team',
        @GuideCatId,
        'Published',
        DATEADD(DAY, -9, GETUTCDATE()),
        234,
        DATEADD(DAY, -9, GETUTCDATE())
    )
END

-- Post 9: Chỉnh sửa video
IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'app-chinh-sua-video-tiktok-tot-nhat')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Excerpt, Content, FeaturedImage, MetaTitle, MetaDescription, MetaKeywords, Author, CategoryId, Status, PublishedAt, ViewCount, CreatedAt)
    VALUES (
        N'Top App Chỉnh Sửa Video TikTok Tốt Nhất 2026',
        'app-chinh-sua-video-tiktok-tot-nhat',
        N'So sánh các app edit video TikTok phổ biến nhất. CapCut, InShot, VN và nhiều hơn nữa.',
        N'<h2>Top App Chỉnh Sửa Video TikTok</h2>
<p>Editor video tốt giúp bạn tạo content chuyên nghiệp hơn. Đây là những app được TikTokers sử dụng nhiều nhất.</p>

<h3>1. CapCut (Miễn phí)</h3>
<p><strong>Ưu điểm:</strong></p>
<ul>
<li>Miễn phí hoàn toàn, không watermark</li>
<li>Templates trending sẵn có</li>
<li>Sync với TikTok</li>
<li>Auto captions tiếng Việt</li>
<li>Green screen, chroma key</li>
</ul>
<p><strong>Nhược điểm:</strong> Một số tính năng cần Pro</p>
<p><strong>Đánh giá:</strong> ⭐⭐⭐⭐⭐ (5/5) - Best for TikTok</p>

<h3>2. VN Video Editor (Miễn phí)</h3>
<p><strong>Ưu điểm:</strong></p>
<ul>
<li>Giao diện chuyên nghiệp</li>
<li>Keyframe animation</li>
<li>Multi-layer editing</li>
<li>Export 4K</li>
</ul>
<p><strong>Nhược điểm:</strong> Learning curve cao hơn</p>
<p><strong>Đánh giá:</strong> ⭐⭐⭐⭐ (4/5)</p>

<h3>3. InShot (Freemium)</h3>
<p><strong>Ưu điểm:</strong></p>
<ul>
<li>Dễ sử dụng</li>
<li>Filters đẹp</li>
<li>Text effects phong phú</li>
</ul>
<p><strong>Nhược điểm:</strong> Watermark trong bản miễn phí</p>
<p><strong>Đánh giá:</strong> ⭐⭐⭐⭐ (4/5)</p>

<h3>4. Adobe Premiere Rush (Trả phí)</h3>
<p><strong>Ưu điểm:</strong></p>
<ul>
<li>Sync với Adobe Creative Cloud</li>
<li>Chất lượng export cao</li>
<li>Chuyên nghiệp</li>
</ul>
<p><strong>Nhược điểm:</strong> Phí hàng tháng</p>
<p><strong>Đánh giá:</strong> ⭐⭐⭐⭐ (4/5)</p>

<h3>Nên Dùng App Nào?</h3>
<ul>
<li><strong>Mới bắt đầu:</strong> CapCut - Dễ dùng, đủ tính năng</li>
<li><strong>Muốn chuyên nghiệp hơn:</strong> VN Editor</li>
<li><strong>Đã quen edit:</strong> Premiere Rush hoặc Final Cut</li>
</ul>

<h3>Tips Edit Video Viral</h3>
<ul>
<li>Cắt nhanh, không để đoạn thừa</li>
<li>Thêm text ở điểm quan trọng</li>
<li>Dùng transitions tự nhiên</li>
<li>Đừng quá lạm dụng effects</li>
</ul>',
        'https://images.unsplash.com/photo-1574717024653-61fd2cf4d44d?w=1200&q=80',
        N'Top App Chỉnh Sửa Video TikTok Tốt Nhất 2026',
        N'So sánh các app edit video TikTok: CapCut, VN, InShot. Review chi tiết ưu nhược điểm.',
        N'app edit video, capcut, vn editor, chinh sua video tiktok',
        'TrendTag Team',
        @GuideCatId,
        'Published',
        DATEADD(DAY, -10, GETUTCDATE()),
        345,
        DATEADD(DAY, -10, GETUTCDATE())
    )
END

-- Post 10: Kiếm tiền TikTok
IF NOT EXISTS (SELECT 1 FROM BlogPosts WHERE Slug = 'cach-kiem-tien-tren-tiktok-2026')
BEGIN
    INSERT INTO BlogPosts (Title, Slug, Excerpt, Content, FeaturedImage, MetaTitle, MetaDescription, MetaKeywords, Author, CategoryId, Status, PublishedAt, ViewCount, CreatedAt)
    VALUES (
        N'Cách Kiếm Tiền Trên TikTok 2026 - Hướng Dẫn Chi Tiết',
        'cach-kiem-tien-tren-tiktok-2026',
        N'Tất cả các cách kiếm tiền trên TikTok 2026: Creator Fund, sponsorship, affiliate, live gifts và nhiều hơn.',
        N'<h2>Hướng Dẫn Kiếm Tiền TikTok 2026</h2>
<p>TikTok không chỉ để giải trí mà còn là nguồn thu nhập cho hàng triệu creators. Đây là các cách kiếm tiền phổ biến nhất.</p>

<h3>1. TikTok Creator Rewards Program</h3>
<p><strong>Yêu cầu:</strong></p>
<ul>
<li>18+ tuổi</li>
<li>10,000+ followers</li>
<li>100,000+ views trong 30 ngày gần nhất</li>
<li>Tuân thủ Community Guidelines</li>
</ul>
<p><strong>Thu nhập:</strong> $0.02-0.04/1000 views (tùy niche và audience)</p>

<h3>2. Brand Sponsorship</h3>
<p><strong>Mức giá tham khảo:</strong></p>
<ul>
<li>10K-50K followers: 500K-2tr/video</li>
<li>50K-100K followers: 2-5tr/video</li>
<li>100K-500K followers: 5-20tr/video</li>
<li>500K-1M followers: 20-50tr/video</li>
<li>1M+ followers: 50tr+/video</li>
</ul>

<h3>3. TikTok Shop & Affiliate</h3>
<ul>
<li>Bán sản phẩm trực tiếp qua TikTok Shop</li>
<li>Làm affiliate với commission 5-30%</li>
<li>Review sản phẩm với link affiliate</li>
</ul>

<h3>4. Live Streaming Gifts</h3>
<ul>
<li>Nhận quà từ người xem khi live</li>
<li>Quy đổi diamonds thành tiền</li>
<li>Top creators kiếm 10-100tr/tháng từ live</li>
</ul>

<h3>5. Bán Sản Phẩm/Dịch Vụ Riêng</h3>
<ul>
<li>Khóa học online</li>
<li>Coaching/consulting</li>
<li>Merchandise</li>
<li>Ebook, template</li>
</ul>

<h3>6. TikTok Series (Trả phí xem)</h3>
<ul>
<li>Tạo content độc quyền cho subscribers</li>
<li>Thu phí hàng tháng</li>
<li>Cần 10K+ followers để unlock</li>
</ul>

<h3>Tips Tăng Thu Nhập</h3>
<ol>
<li>Focus vào niche có CPC cao: tài chính, công nghệ, làm đẹp</li>
<li>Xây dựng email list để không phụ thuộc platform</li>
<li>Đa dạng hóa nguồn thu (đừng chỉ dựa vào 1 nguồn)</li>
<li>Tạo content chất lượng với <a href="/">hashtag trending</a></li>
</ol>

<h3>Bắt Đầu Kiếm Tiền Từ Đâu?</h3>
<p>Nếu mới bắt đầu, hãy focus vào tăng followers trước. Khi đạt 10K followers, bạn sẽ có nhiều cơ hội kiếm tiền hơn!</p>',
        'https://images.unsplash.com/photo-1553729459-uj9b1d7f5d89?w=1200&q=80',
        N'Cách Kiếm Tiền Trên TikTok 2026 - Hướng Dẫn Đầy Đủ',
        N'Hướng dẫn kiếm tiền TikTok 2026: Creator Fund, sponsorship, affiliate, live gifts.',
        N'kiem tien tiktok, tiktok creator fund, sponsorship tiktok, lam giau tu tiktok',
        'TrendTag Team',
        @GuideCatId,
        'Published',
        DATEADD(DAY, -1, GETUTCDATE()),
        892,
        DATEADD(DAY, -1, GETUTCDATE())
    )
END

PRINT 'Blog posts seeded successfully!'
GO
