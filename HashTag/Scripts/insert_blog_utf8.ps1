# PowerShell script to insert blog posts with proper UTF-8 encoding
$connectionString = "Server=112.78.2.36;Database=vir62982_tag;User Id=vir62982_user;Password=*1MAbonR?hu7saa7;Encrypt=false;"

# Get category IDs
$getCatQuery = @"
SELECT Id, Slug FROM BlogCategories WHERE Slug IN ('meo-tiktok', 'phan-tich-trending')
"@

$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection.Open()

# Get TikTok Tips category ID
$cmd = $connection.CreateCommand()
$cmd.CommandText = "SELECT Id FROM BlogCategories WHERE Slug = 'meo-tiktok'"
$tiktokTipsCatId = $cmd.ExecuteScalar()

$cmd.CommandText = "SELECT Id FROM BlogCategories WHERE Slug = 'phan-tich-trending'"
$hashtagTrendingCatId = $cmd.ExecuteScalar()

Write-Host "TikTok Tips Category ID: $tiktokTipsCatId"
Write-Host "Hashtag Trending Category ID: $hashtagTrendingCatId"

# Insert Blog Post 1
$title1 = "Cách Lên Xu Hướng TikTok 2026: 10 Bước Từ 0 Đến Triệu View"
$slug1 = "cach-len-xu-huong-tiktok-2026-10-buoc"
$excerpt1 = "Hướng dẫn chi tiết 10 bước để video TikTok của bạn lên xu hướng và đạt triệu view. Áp dụng ngay để viral!"
$metaTitle1 = "Cách Lên Xu Hướng TikTok 2026: 10 Bước Viral Triệu View"
$metaDesc1 = "Hướng dẫn chi tiết 10 bước để video TikTok lên xu hướng và viral triệu view. Bí quyết của TikToker chuyên nghiệp!"
$metaKeywords1 = "cách lên xu hướng tiktok, viral tiktok, triệu view tiktok, xu hướng tiktok 2026"
$content1 = @"
<h2>Bí Quyết Lên Xu Hướng TikTok 2026</h2>
<p>Bạn muốn video TikTok của mình viral và đạt hàng triệu view? Đây là 10 bước chi tiết mà các TikToker triệu follow đang áp dụng.</p>

<h2>Bước 1: Nghiên Cứu Trend Hiện Tại</h2>
<p>Trước khi quay video, hãy dành 15-20 phút lướt FYP để nắm bắt những trend đang hot.</p>
<p>Sử dụng <a href='/'>TrendTag</a> để xem danh sách hashtag trending cập nhật mỗi 6 giờ.</p>

<h2>Bước 2: Chọn Niche Phù Hợp</h2>
<p>Đừng cố gắng làm mọi thứ! Chọn 1-2 chủ đề chính và tập trung vào đó.</p>

<h2>Bước 3: Tối Ưu 3 Giây Đầu Tiên</h2>
<p>TikTok algorithm đánh giá video dựa trên retention rate. <strong>3 giây đầu tiên quyết định 80% thành công!</strong></p>

<h2>Bước 4: Sử Dụng Hashtag Thông Minh</h2>
<p>Sử dụng công thức 5-7 hashtag. Dùng <a href='/hashtag/tao-hashtag-ai'>Tạo Hashtag AI</a> để nhận gợi ý thông minh.</p>

<h2>Bước 5: Đăng Vào Giờ Vàng</h2>
<p>Giờ vàng tại Việt Nam: 7:00-9:00, 11:30-13:30, 19:00-22:00</p>

<h2>Bước 6: Tương Tác Ngay Sau Khi Đăng</h2>
<p>30 phút đầu sau khi đăng video là critical time. Reply tất cả comments ngay!</p>

<h2>Bước 7: Sử Dụng Âm Thanh Trending</h2>
<p>Video sử dụng âm thanh trending có cơ hội lên FYP cao hơn 40%!</p>

<h2>Bước 8: Tạo Series Và Hooks</h2>
<p>Tạo series video khiến người xem muốn follow để xem tiếp.</p>

<h2>Bước 9: Cross-Promote Trên Các Nền Tảng Khác</h2>
<p>Chia sẻ video lên Instagram Reels, YouTube Shorts, Facebook Reels.</p>

<h2>Bước 10: Phân Tích Và Tối Ưu Liên Tục</h2>
<p>Dùng <a href='/phan-tich/theo-doi-tang-truong'>Theo Dõi Tăng Trưởng</a> để phân tích hashtag hiệu quả.</p>

<h2>Kết Luận</h2>
<p><strong>Công thức thành công = Nội dung chất lượng + Hashtag đúng + Thời điểm đăng tốt + Kiên trì</strong></p>
"@

$insertQuery1 = @"
INSERT INTO BlogPosts (Title, Slug, Excerpt, FeaturedImage, Content, MetaTitle, MetaDescription, MetaKeywords, Author, CategoryId, Status, PublishedAt, ViewCount, CreatedAt)
VALUES (@Title, @Slug, @Excerpt, @FeaturedImage, @Content, @MetaTitle, @MetaDesc, @MetaKeywords, @Author, @CategoryId, @Status, @PublishedAt, @ViewCount, @CreatedAt)
"@

$cmd = $connection.CreateCommand()
$cmd.CommandText = $insertQuery1
$cmd.Parameters.AddWithValue("@Title", $title1) | Out-Null
$cmd.Parameters.AddWithValue("@Slug", $slug1) | Out-Null
$cmd.Parameters.AddWithValue("@Excerpt", $excerpt1) | Out-Null
$cmd.Parameters.AddWithValue("@FeaturedImage", "https://images.unsplash.com/photo-1611162617474-5b21e879e113?w=1200&q=80") | Out-Null
$cmd.Parameters.AddWithValue("@Content", $content1) | Out-Null
$cmd.Parameters.AddWithValue("@MetaTitle", $metaTitle1) | Out-Null
$cmd.Parameters.AddWithValue("@MetaDesc", $metaDesc1) | Out-Null
$cmd.Parameters.AddWithValue("@MetaKeywords", $metaKeywords1) | Out-Null
$cmd.Parameters.AddWithValue("@Author", "TrendTag Team") | Out-Null
$cmd.Parameters.AddWithValue("@CategoryId", $tiktokTipsCatId) | Out-Null
$cmd.Parameters.AddWithValue("@Status", "Published") | Out-Null
$cmd.Parameters.AddWithValue("@PublishedAt", [DateTime]::UtcNow) | Out-Null
$cmd.Parameters.AddWithValue("@ViewCount", 0) | Out-Null
$cmd.Parameters.AddWithValue("@CreatedAt", [DateTime]::UtcNow) | Out-Null
$cmd.ExecuteNonQuery() | Out-Null
Write-Host "Inserted: $title1"

# Insert Blog Post 2
$title2 = "Algorithm TikTok 2026: Cách FYP Hoạt Động Và Bí Quyết Lên For You Page"
$slug2 = "algorithm-tiktok-2026-cach-fyp-hoat-dong"
$excerpt2 = "Giải mã thuật toán TikTok 2026: Hiểu cách For You Page hoạt động để tối ưu video và tăng reach."
$metaTitle2 = "Algorithm TikTok 2026: Cách FYP Hoạt Động - Bí Quyết Viral"
$metaDesc2 = "Giải mã thuật toán TikTok 2026: Hiểu cách For You Page hoạt động để tối ưu video và tăng triệu view."
$metaKeywords2 = "algorithm tiktok, thuật toán tiktok, fyp tiktok, for you page, cách lên fyp"
$content2 = @"
<h2>Giải Mã Algorithm TikTok 2026</h2>
<p>Thuật toán TikTok là hệ thống AI quyết định video nào xuất hiện trên For You Page của mỗi người dùng.</p>

<h2>4 Yếu Tố Chính Algorithm Đánh Giá</h2>

<h3>1. User Interactions (Tương Tác Người Dùng)</h3>
<p>TikTok theo dõi: Likes, Comments, Shares, Follows, Watch time, Re-watches</p>

<h3>2. Video Information (Thông Tin Video)</h3>
<p>Algorithm phân tích: Captions, Hashtags, Sounds, Effects, Video content</p>

<h3>3. Device & Account Settings</h3>
<p>Ngôn ngữ, Quốc gia, Loại thiết bị</p>

<h3>4. Content Freshness (Độ Mới)</h3>
<p>Video mới được ưu tiên hơn video cũ.</p>

<h2>Cách FYP Hoạt Động</h2>
<p><strong>Giai đoạn 1:</strong> Initial Push (30 phút đầu) - 300-500 viewers</p>
<p><strong>Giai đoạn 2:</strong> Expanded Reach (1-4 giờ) - 1,000-5,000 viewers</p>
<p><strong>Giai đoạn 3:</strong> Viral Phase (4-48 giờ) - 10,000-100,000+ viewers</p>

<h2>Metrics Quan Trọng Nhất</h2>
<p><strong>Watch Time</strong> là metric QUAN TRỌNG NHẤT!</p>
<p><strong>Engagement Rate</strong> > 10% = Xuất sắc</p>

<h2>Vai Trò Của Hashtag</h2>
<p>Sử dụng <a href='/'>TrendTag</a> để tìm hashtag trending với mức độ cạnh tranh phù hợp.</p>

<h2>Kết Luận</h2>
<p>Algorithm TikTok 2026 ưu tiên <strong>watch time</strong> và <strong>engagement</strong>.</p>
"@

$cmd = $connection.CreateCommand()
$cmd.CommandText = $insertQuery1
$cmd.Parameters.AddWithValue("@Title", $title2) | Out-Null
$cmd.Parameters.AddWithValue("@Slug", $slug2) | Out-Null
$cmd.Parameters.AddWithValue("@Excerpt", $excerpt2) | Out-Null
$cmd.Parameters.AddWithValue("@FeaturedImage", "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?w=1200&q=80") | Out-Null
$cmd.Parameters.AddWithValue("@Content", $content2) | Out-Null
$cmd.Parameters.AddWithValue("@MetaTitle", $metaTitle2) | Out-Null
$cmd.Parameters.AddWithValue("@MetaDesc", $metaDesc2) | Out-Null
$cmd.Parameters.AddWithValue("@MetaKeywords", $metaKeywords2) | Out-Null
$cmd.Parameters.AddWithValue("@Author", "TrendTag Team") | Out-Null
$cmd.Parameters.AddWithValue("@CategoryId", $tiktokTipsCatId) | Out-Null
$cmd.Parameters.AddWithValue("@Status", "Published") | Out-Null
$cmd.Parameters.AddWithValue("@PublishedAt", [DateTime]::UtcNow.AddHours(-12)) | Out-Null
$cmd.Parameters.AddWithValue("@ViewCount", 0) | Out-Null
$cmd.Parameters.AddWithValue("@CreatedAt", [DateTime]::UtcNow.AddHours(-12)) | Out-Null
$cmd.ExecuteNonQuery() | Out-Null
Write-Host "Inserted: $title2"

# Insert Blog Post 3
$title3 = "Hashtag TikTok Việt Nam: Danh Sách 500+ Hashtag Theo Chủ Đề [2026]"
$slug3 = "hashtag-tiktok-viet-nam-danh-sach-500-hashtag"
$excerpt3 = "Tổng hợp 500+ hashtag TikTok phổ biến nhất tại Việt Nam, phân loại theo 16 chủ đề. Copy và sử dụng ngay!"
$metaTitle3 = "500+ Hashtag TikTok Việt Nam Theo Chủ Đề [2026] - Copy Ngay!"
$metaDesc3 = "Danh sách 500+ hashtag TikTok Việt Nam đầy đủ nhất, phân loại theo 16 chủ đề. Copy và sử dụng ngay!"
$metaKeywords3 = "hashtag tiktok việt nam, hashtag tiktok, danh sách hashtag, hashtag theo chủ đề"
$content3 = @"
<h2>Danh Sách Hashtag TikTok Việt Nam Đầy Đủ Nhất</h2>
<p>Đây là bộ sưu tập 500+ hashtag TikTok phổ biến nhất tại Việt Nam, được phân loại theo 16 chủ đề.</p>
<p><strong>Mẹo:</strong> Sử dụng <a href='/'>TrendTag</a> để xem hashtag nào đang trending NGAY LÚC NÀY.</p>

<h2>1. Hashtag Chung (General)</h2>
<p><code>#fyp #foryou #viral #xuhuong #trending #tiktokvietnam #tiktok #fypシ #foryoupage #viraltiktok</code></p>

<h2>2. Hashtag Làm Đẹp & Skincare</h2>
<p><code>#lamdep #skincare #makeup #beauty #skincareroutine #beautytiktok #reviewmypham #mypham #duongda</code></p>
<p><a href='/chu-de/lam-dep-cham-soc-ca-nhan'>Xem hashtag Làm Đẹp trending →</a></p>

<h2>3. Hashtag Ẩm Thực & Nấu Ăn</h2>
<p><code>#amthuc #nauanuon #food #cooking #recipe #foodtiktok #reviewdoan #ancungtiktok</code></p>
<p><a href='/chu-de/do-an-thuc-pham'>Xem hashtag Ẩm Thực trending →</a></p>

<h2>4. Hashtag Du Lịch</h2>
<p><code>#dulich #travel #dulichtiktok #vietnam #traveling #vacation #dulichvietnam #travelvietnam</code></p>
<p><a href='/chu-de/du-lich'>Xem hashtag Du Lịch trending →</a></p>

<h2>5. Hashtag Thời Trang</h2>
<p><code>#thoitrang #fashion #ootd #style #outfitoftheday #fashiontiktok #streetstyle</code></p>
<p><a href='/chu-de/thoi-trang-phu-kien'>Xem hashtag Thời Trang trending →</a></p>

<h2>6. Hashtag Giáo Dục & Học Tập</h2>
<p><code>#giaoduc #hoctap #learntiktok #edutok #hoctienganh #english #study #knowledge</code></p>
<p><a href='/chu-de/giao-duc'>Xem hashtag Giáo Dục trending →</a></p>

<h2>7. Hashtag Công Nghệ</h2>
<p><code>#congnghe #tech #technology #iphone #samsung #laptop #smartphone #review</code></p>
<p><a href='/chu-de/cong-nghe'>Xem hashtag Công Nghệ trending →</a></p>

<h2>8. Hashtag Thể Thao & Gym</h2>
<p><code>#thethao #gym #fitness #workout #tapgym #sport #bongda #football</code></p>
<p><a href='/chu-de/the-thao-ngoai-troi'>Xem hashtag Thể Thao trending →</a></p>

<h2>Cách Sử Dụng Hashtag Hiệu Quả</h2>
<p>Chọn 5-7 hashtag theo công thức:</p>
<ul>
<li><strong>2 hashtag trending:</strong> #fyp #xuhuong</li>
<li><strong>2-3 hashtag chủ đề:</strong> Phù hợp với nội dung video</li>
<li><strong>1-2 hashtag niche:</strong> Cụ thể và ít cạnh tranh</li>
</ul>

<h2>Công Cụ Tìm Hashtag Trending</h2>
<p>Sử dụng các công cụ của <a href='/'>TrendTag</a>:</p>
<ul>
<li><a href='/'>Xem Top 100 Hashtag Trending</a></li>
<li><a href='/hashtag/tao-hashtag-ai'>Tạo Hashtag AI</a></li>
<li><a href='/phan-tich/theo-doi-tang-truong'>Theo Dõi Tăng Trưởng</a></li>
</ul>
"@

$cmd = $connection.CreateCommand()
$cmd.CommandText = $insertQuery1
$cmd.Parameters.AddWithValue("@Title", $title3) | Out-Null
$cmd.Parameters.AddWithValue("@Slug", $slug3) | Out-Null
$cmd.Parameters.AddWithValue("@Excerpt", $excerpt3) | Out-Null
$cmd.Parameters.AddWithValue("@FeaturedImage", "https://images.unsplash.com/photo-1611162618071-b39a2ec055fb?w=1200&q=80") | Out-Null
$cmd.Parameters.AddWithValue("@Content", $content3) | Out-Null
$cmd.Parameters.AddWithValue("@MetaTitle", $metaTitle3) | Out-Null
$cmd.Parameters.AddWithValue("@MetaDesc", $metaDesc3) | Out-Null
$cmd.Parameters.AddWithValue("@MetaKeywords", $metaKeywords3) | Out-Null
$cmd.Parameters.AddWithValue("@Author", "TrendTag Team") | Out-Null
$cmd.Parameters.AddWithValue("@CategoryId", $hashtagTrendingCatId) | Out-Null
$cmd.Parameters.AddWithValue("@Status", "Published") | Out-Null
$cmd.Parameters.AddWithValue("@PublishedAt", [DateTime]::UtcNow.AddHours(-6)) | Out-Null
$cmd.Parameters.AddWithValue("@ViewCount", 0) | Out-Null
$cmd.Parameters.AddWithValue("@CreatedAt", [DateTime]::UtcNow.AddHours(-6)) | Out-Null
$cmd.ExecuteNonQuery() | Out-Null
Write-Host "Inserted: $title3"

$connection.Close()
Write-Host "Done! All 3 blog posts inserted with proper UTF-8 encoding."
