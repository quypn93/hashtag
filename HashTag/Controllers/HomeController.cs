using System.Diagnostics;
using HashTag.Models;
using HashTag.Repositories;
using HashTag.Services;
using HashTag.ViewModels;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace HashTag.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHashtagRepository _repository;
        private readonly ITikTokLiveSearchService _liveSearchService;
        private readonly IBlogRepository _blogRepository;

        public HomeController(
            ILogger<HomeController> logger,
            IHashtagRepository repository,
            ITikTokLiveSearchService liveSearchService,
            IBlogRepository blogRepository)
        {
            _logger = logger;
            _repository = repository;
            _liveSearchService = liveSearchService;
            _blogRepository = blogRepository;
        }

        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "categoryId", "region" })]
        public async Task<IActionResult> Index(int? categoryId)
        {
            try
            {
                // Get current region from cookie
                var currentRegion = GetCurrentRegion();

                // Get top trending hashtags filtered by region
                var filterDto = new HashtagFilterDto
                {
                    CategoryId = categoryId,
                    CountryCode = currentRegion
                };

                var topHashtags = await _repository.GetTrendingHashtagsAsync(filterDto);
                var top10 = topHashtags.Take(100).ToList();

                // Get available categories for dropdown
                var categories = await _repository.GetActiveCategoriesAsync();

                // Get recent blog posts (3 most recent)
                var recentBlogPosts = await _blogRepository.GetRecentPostsAsync(3);

                var viewModel = new HomeIndexViewModel
                {
                    TopHashtags = top10,
                    Categories = categories.Select(c => new CategoryOption
                    {
                        Id = c.Id,
                        Name = c.Name,
                        DisplayNameVi = c.DisplayNameVi,
                        Slug = c.Slug
                    }).ToList(),
                    SelectedCategoryId = categoryId,
                    RecentBlogPosts = recentBlogPosts.ToList()
                };

                // Create SEO metadata for home page
                var seoMetadata = CreateHomeSeoMetadata(categoryId, categories);
                ViewData["SeoMetadata"] = seoMetadata;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading home page: {Message}", ex.Message);
                return View(new HomeIndexViewModel());
            }
        }

        /// <summary>
        /// SEO-friendly category page (e.g., /chu-de/giao-duc)
        /// Renders same view as Index but keeps SEO-friendly URL
        /// </summary>
        public async Task<IActionResult> Category(string slug)
        {
            try
            {
                // Get all categories
                var categories = await _repository.GetActiveCategoriesAsync();
                var category = categories.FirstOrDefault(c => c.Slug == slug);

                if (category == null)
                {
                    return NotFound();
                }

                // Get current region from cookie
                var currentRegion = GetCurrentRegion();

                // Get top 10 trending hashtags for this category and region
                var filterDto = new HashtagFilterDto
                {
                    CategoryId = category.Id,
                    CountryCode = currentRegion
                };

                var topHashtags = await _repository.GetTrendingHashtagsAsync(filterDto);
                var top10 = topHashtags.Take(10).ToList();

                // Get recent blog posts (3 most recent)
                var recentBlogPosts = await _blogRepository.GetRecentPostsAsync(3);

                var viewModel = new HomeIndexViewModel
                {
                    TopHashtags = top10,
                    Categories = categories.Select(c => new CategoryOption
                    {
                        Id = c.Id,
                        Name = c.Name,
                        DisplayNameVi = c.DisplayNameVi,
                        Slug = c.Slug
                    }).ToList(),
                    SelectedCategoryId = category.Id,
                    RecentBlogPosts = recentBlogPosts.ToList()
                };

                // Create SEO metadata for category page
                var seoMetadata = CreateHomeSeoMetadata(category.Id, categories);
                ViewData["SeoMetadata"] = seoMetadata;

                // Render Index view (same layout, different URL)
                return View("Index", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading category page for slug '{Slug}': {Message}", slug, ex.Message);
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// API endpoint to get top hashtags by category and region (AJAX)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTopHashtags(int? categoryId, string? region)
        {
            try
            {
                // Use provided region or fall back to cookie
                var currentRegion = !string.IsNullOrEmpty(region) ? region : GetCurrentRegion();

                var filterDto = new HashtagFilterDto
                {
                    CategoryId = categoryId,
                    CountryCode = currentRegion
                };

                var topHashtags = await _repository.GetTrendingHashtagsAsync(filterDto);
                var top10 = topHashtags.Take(100).ToList();

                return Json(top10);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top hashtags for categoryId '{CategoryId}': {Message}", categoryId, ex.Message);
                return Json(new List<TrendingHashtagDto>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Search(string q, int page = 1)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return RedirectToAction(nameof(Index));
                }

                // Search in database first
                var result = await _repository.SearchHashtagsAsync(q, page, pageSize: 20);

                var viewModel = new HashtagSearchViewModel
                {
                    Query = q,
                    Results = result,
                    CurrentPage = page
                };

                // If no results found in database, try live search from TikTok
                if (result.TotalCount == 0)
                {
                    _logger.LogInformation("No results in database for '{Query}', attempting live search", q);

                    var liveResult = await _liveSearchService.SearchTikTokCreativeCenterAsync(q);

                    if (liveResult != null && liveResult.IsAvailable)
                    {
                        viewModel.LiveResult = liveResult;
                        _logger.LogInformation("Found live result for '{Query}': PostCount={PostCount}",
                            q, liveResult.PostCount);
                    }
                    else
                    {
                        _logger.LogInformation("No live results found for '{Query}'", q);
                    }
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching hashtags for query '{Query}': {Message}", q, ex.Message);
                return View(new HashtagSearchViewModel { Query = q });
            }
        }

        /// <summary>
        /// Add a hashtag to tracking list (from live search results) - User action
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddToTracking(string tag, long? postCount)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tag))
                {
                    TempData["ErrorMessage"] = "Invalid hashtag";
                    return RedirectToAction(nameof(Index));
                }

                var normalizedTag = tag.TrimStart('#').ToLower();

                // Check if already exists
                var existing = await _repository.GetHashtagByTagAsync(normalizedTag);
                if (existing != null)
                {
                    TempData["InfoMessage"] = $"#{normalizedTag} is already being tracked!";
                    return RedirectToAction(nameof(Search), new { q = normalizedTag });
                }

                // Create new hashtag entry
                var hashtag = await _repository.GetOrCreateHashtagAsync(normalizedTag);

                // Set initial values
                hashtag.LatestPostCount = postCount;
                hashtag.IsActive = true;
                await _repository.UpdateHashtagAsync(hashtag);

                TempData["SuccessMessage"] = $"Added #{normalizedTag} to tracking list! It will be included in the next crawl.";

                _logger.LogInformation("User added hashtag #{Tag} to tracking from live search", normalizedTag);

                return RedirectToAction(nameof(Search), new { q = normalizedTag });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding hashtag to tracking: {Message}", ex.Message);
                TempData["ErrorMessage"] = "Failed to add hashtag to tracking";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Set user's preferred language
        /// </summary>
        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax
                }
            );

            return LocalRedirect(returnUrl ?? "/");
        }

        /// <summary>
        /// Cookie name for storing user's preferred region
        /// </summary>
        public const string RegionCookieName = "UserRegion";

        /// <summary>
        /// Get current region from cookie (default: VN)
        /// </summary>
        private string GetCurrentRegion()
        {
            return Request.Cookies[RegionCookieName] ?? "VN";
        }

        /// <summary>
        /// Set user's preferred region for hashtag data
        /// Also sets the UI language based on region (VN = Vietnamese, others = English)
        /// </summary>
        [HttpPost]
        public IActionResult SetRegion(string region, string returnUrl)
        {
            // Validate region code
            var validRegions = new[] { "VN", "US", "GB", "AU" };
            if (!validRegions.Contains(region?.ToUpper()))
            {
                region = "VN";
            }

            region = region.ToUpper();

            // Set region cookie
            Response.Cookies.Append(
                RegionCookieName,
                region,
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax
                }
            );

            // Also set language based on region (VN = Vietnamese, others = English)
            var culture = region == "VN" ? "vi" : "en";
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax
                }
            );

            return LocalRedirect(returnUrl ?? "/");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? statusCode = null)
        {
            if (statusCode == 404)
            {
                Response.StatusCode = 404;
                return View("NotFound");
            }

            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// Create comprehensive SEO metadata for home page
        /// </summary>
        private SeoMetadata CreateHomeSeoMetadata(int? categoryId, IEnumerable<Models.HashtagCategory> categories)
        {
            string title, description, keywords, canonicalUrl;

            if (categoryId.HasValue)
            {
                var category = categories.FirstOrDefault(c => c.Id == categoryId.Value);
                if (category != null)
                {
                    var categoryNameVi = !string.IsNullOrEmpty(category.DisplayNameVi) ? category.DisplayNameVi : category.Name;
                    title = $"Top Hashtag TikTok Trending {categoryNameVi}";
                    description = $"Khám phá top hashtag TikTok trending trong chủ đề {categoryNameVi}. Cập nhật mỗi 6 giờ với phân tích độ khó, lượt xem và dự đoán viral cho creator Việt Nam.";
                    keywords = $"hashtag {categoryNameVi}, hashtag tiktok {categoryNameVi}, trending {categoryNameVi}, hashtag viral {categoryNameVi}";
                    canonicalUrl = $"https://viralhashtag.vn/chu-de/{category.Slug}";
                }
                else
                {
                    title = "Top 10 Hashtag TikTok Trending Hôm Nay";
                    description = "Khám phá top 10 hashtag TikTok trending hôm nay tại Việt Nam. Cập nhật mỗi 6 giờ với phân tích độ khó, lượt xem và dự đoán viral cho creator.";
                    keywords = "hashtag tiktok trending, hashtag viral, hashtag tiktok việt nam, top hashtag tiktok";
                    canonicalUrl = "https://viralhashtag.vn";
                }
            }
            else
            {
                title = "TrendTag - Công Cụ Tìm Hashtag TikTok Trending Hàng Đầu Việt Nam";
                description = "Công cụ phân tích hashtag TikTok hàng đầu Việt Nam. Tìm hashtag trending, dự đoán lượt xem, phân tích độ khó cạnh tranh và tăng trưởng kênh TikTok hiệu quả. Cập nhật mỗi 6 giờ, 100% miễn phí.";
                keywords = "hashtag tiktok trending, công cụ tìm hashtag, hashtag tiktok việt nam, hashtag viral, phân tích hashtag tiktok, tăng view tiktok, hashtag trending hôm nay";
                canonicalUrl = "https://viralhashtag.vn";
            }

            // Create WebApplication + FAQPage structured data (JSON-LD array)
            var structuredData = @"[
                {
                    ""@context"": ""https://schema.org"",
                    ""@type"": ""WebApplication"",
                    ""name"": ""TrendTag"",
                    ""url"": """ + canonicalUrl + @""",
                    ""description"": ""Công cụ phân tích hashtag TikTok hàng đầu Việt Nam. Tìm hashtag trending, dự đoán lượt xem và tăng trưởng kênh TikTok hiệu quả."",
                    ""applicationCategory"": ""BusinessApplication"",
                    ""operatingSystem"": ""Web"",
                    ""offers"": {
                        ""@type"": ""Offer"",
                        ""price"": ""0"",
                        ""priceCurrency"": ""VND""
                    },
                    ""aggregateRating"": {
                        ""@type"": ""AggregateRating"",
                        ""ratingValue"": ""4.8"",
                        ""ratingCount"": ""1250"",
                        ""bestRating"": ""5"",
                        ""worstRating"": ""1""
                    },
                    ""featureList"": [
                        ""Phân tích hashtag TikTok trending real-time"",
                        ""Dự đoán lượt xem và độ khó cạnh tranh"",
                        ""Cập nhật dữ liệu mỗi 6 giờ"",
                        ""16+ chủ đề hashtag chuyên sâu"",
                        ""100% miễn phí, không giới hạn tra cứu""
                    ],
                    ""screenshot"": ""https://viralhashtag.vn/images/screenshot.png"",
                    ""inLanguage"": ""vi-VN"",
                    ""countryOfOrigin"": ""VN""
                },
                {
                    ""@context"": ""https://schema.org"",
                    ""@type"": ""FAQPage"",
                    ""mainEntity"": [
                        {
                            ""@type"": ""Question"",
                            ""name"": ""Hashtag TikTok trending là gì?"",
                            ""acceptedAnswer"": {
                                ""@type"": ""Answer"",
                                ""text"": ""Hashtag TikTok trending là những hashtag đang được sử dụng nhiều nhất trên TikTok tại một thời điểm cụ thể. Sử dụng hashtag trending giúp video của bạn tiếp cận nhiều người xem hơn, tăng tương tác và có cơ hội cao hơn để xuất hiện trên trang For You (FYP). TrendTag cập nhật danh sách hashtag trending mỗi 6 giờ để đảm bảo bạn luôn có dữ liệu mới nhất.""
                            }
                        },
                        {
                            ""@type"": ""Question"",
                            ""name"": ""Cách chọn hashtag TikTok hiệu quả nhất?"",
                            ""acceptedAnswer"": {
                                ""@type"": ""Answer"",
                                ""text"": ""Để chọn hashtag hiệu quả, hãy kết hợp 3-5 hashtag trending có mức độ cạnh tranh Thấp hoặc Trung Bình với 2-3 hashtag ngách (niche) liên quan đến nội dung của bạn. Ví dụ: Nếu bạn làm video về du lịch Đà Lạt, hãy kết hợp #dulich (trending, cạnh tranh cao) với #dulichdalat2025 (ngách, cạnh tranh thấp). Chiến lược này giúp tăng cơ hội viral trong cả nhóm đối tượng rộng lẫn đối tượng mục tiêu cụ thể.""
                            }
                        },
                        {
                            ""@type"": ""Question"",
                            ""name"": ""TrendTag cập nhật dữ liệu hashtag bao lâu một lần?"",
                            ""acceptedAnswer"": {
                                ""@type"": ""Answer"",
                                ""text"": ""TrendTag tự động cập nhật dữ liệu hashtag mỗi 6 giờ từ nhiều nguồn uy tín như TikTok Creative Center và các nguồn khác. Điều này đảm bảo bạn luôn có danh sách hashtag trending mới nhất và chính xác nhất để tối ưu video của mình.""
                            }
                        },
                        {
                            ""@type"": ""Question"",
                            ""name"": ""Mức độ cạnh tranh của hashtag là gì?"",
                            ""acceptedAnswer"": {
                                ""@type"": ""Answer"",
                                ""text"": ""Mức độ cạnh tranh cho biết độ khó để video của bạn nổi bật khi sử dụng hashtag đó. TrendTag phân loại thành 4 mức: Thấp (dễ viral, ít người dùng, phù hợp cho creator mới), Trung Bình (cơ hội tốt nếu nội dung chất lượng), Cao (khó viral, nhiều người dùng, cần nội dung xuất sắc), và Rất Cao (cực kỳ cạnh tranh, chỉ phù hợp creator lớn).""
                            }
                        },
                        {
                            ""@type"": ""Question"",
                            ""name"": ""Có nên sử dụng hashtag viral có lượt xem cao?"",
                            ""acceptedAnswer"": {
                                ""@type"": ""Answer"",
                                ""text"": ""Không hoàn toàn. Hashtag viral có lượt xem cao thường có mức độ cạnh tranh Rất Cao, khiến video của bạn dễ bị 'chìm' trong hàng triệu video khác. Thay vào đó, hãy chọn hashtag có lượt xem vừa phải (100K-5M) và mức độ cạnh tranh Thấp-Trung Bình để tăng cơ hội lên FYP. TrendTag hiển thị cả số lượt xem lẫn mức độ cạnh tranh để bạn đưa ra quyết định thông minh.""
                            }
                        },
                        {
                            ""@type"": ""Question"",
                            ""name"": ""TrendTag có miễn phí không?"",
                            ""acceptedAnswer"": {
                                ""@type"": ""Answer"",
                                ""text"": ""100% miễn phí! TrendTag không yêu cầu đăng ký tài khoản, không giới hạn số lần tra cứu hashtag, và không có phí ẩn. Tất cả tính năng phân tích hashtag, xem trending, theo dõi mức độ cạnh tranh đều hoàn toàn miễn phí cho cộng đồng creator TikTok Việt Nam.""
                            }
                        },
                        {
                            ""@type"": ""Question"",
                            ""name"": ""Làm sao để tìm hashtag theo chủ đề cụ thể?"",
                            ""acceptedAnswer"": {
                                ""@type"": ""Answer"",
                                ""text"": ""TrendTag cung cấp 16+ chủ đề hashtag để bạn dễ dàng lọc và tìm kiếm. Chỉ cần chọn chủ đề từ dropdown ở đầu trang (ví dụ: Giáo Dục, Du Lịch, Thời Trang, Công Nghệ, v.v.) và hệ thống sẽ tự động hiển thị top 10 hashtag trending trong chủ đề đó. Bạn cũng có thể sử dụng thanh tìm kiếm để tìm hashtag cụ thể.""
                            }
                        },
                        {
                            ""@type"": ""Question"",
                            ""name"": ""Nên dùng bao nhiêu hashtag trong một video TikTok?"",
                            ""acceptedAnswer"": {
                                ""@type"": ""Answer"",
                                ""text"": ""TikTok khuyến nghị sử dụng 5-8 hashtag cho mỗi video. Công thức hiệu quả nhất là: 3-5 hashtag trending (mức độ cạnh tranh Thấp-Trung Bình) để tăng reach và 2-3 hashtag ngách (liên quan trực tiếp đến nội dung) để target đúng đối tượng. Tránh dùng quá nhiều hashtag (>10) vì có thể làm giảm hiệu quả và trông spam.""
                            }
                        },
                        {
                            ""@type"": ""Question"",
                            ""name"": ""Hashtag trending có thay đổi theo thời gian không?"",
                            ""acceptedAnswer"": {
                                ""@type"": ""Answer"",
                                ""text"": ""Có, thường xuyên! Hashtag trending trên TikTok thay đổi liên tục theo sự kiện, mùa vụ, xu hướng văn hóa và hành vi người dùng. Một hashtag có thể trending hôm nay nhưng không còn hiệu quả sau vài ngày hoặc vài tuần. Đó là lý do TrendTag cập nhật dữ liệu mỗi 6 giờ để đảm bảo bạn luôn sử dụng hashtag trending mới nhất.""
                            }
                        },
                        {
                            ""@type"": ""Question"",
                            ""name"": ""TrendTag lấy dữ liệu từ đâu?"",
                            ""acceptedAnswer"": {
                                ""@type"": ""Answer"",
                                ""text"": ""TrendTag thu thập dữ liệu từ nhiều nguồn uy tín: TikTok Creative Center (dữ liệu trending chính thức) và TikTok API (số lượt xem và số bài đăng realtime). Tất cả dữ liệu được xử lý và phân tích bằng thuật toán AI để cho ra kết quả chính xác nhất.""
                            }
                        },
                        {
                            ""@type"": ""Question"",
                            ""name"": ""Hashtag trending có giúp tăng follower không?"",
                            ""acceptedAnswer"": {
                                ""@type"": ""Answer"",
                                ""text"": ""Có, nhưng gián tiếp. Hashtag trending giúp video của bạn tiếp cận nhiều người xem hơn (tăng reach và impressions). Khi video có nhiều lượt xem và tương tác cao, thuật toán TikTok sẽ đẩy video lên For You Page (FYP) rộng hơn, từ đó tăng cơ hội được follow. Tuy nhiên, yếu tố quan trọng nhất vẫn là chất lượng nội dung - hashtag chỉ là công cụ hỗ trợ.""
                            }
                        },
                        {
                            ""@type"": ""Question"",
                            ""name"": ""Có nên dùng hashtag bằng tiếng Việt hay tiếng Anh?"",
                            ""acceptedAnswer"": {
                                ""@type"": ""Answer"",
                                ""text"": ""Tùy thuộc vào đối tượng mục tiêu của bạn. Hashtag tiếng Việt phù hợp nếu bạn target khán giả Việt Nam (ví dụ: #dulich, #giaoduc, #amthuc). Hashtag tiếng Anh phù hợp nếu muốn tiếp cận đối tượng quốc tế (ví dụ: #travel, #education, #food). Chiến lược tốt nhất là kết hợp cả hai: 3-4 hashtag tiếng Việt + 2-3 hashtag tiếng Anh để maximize reach cả trong lẫn ngoài nước.""
                            }
                        },
                        {
                            ""@type"": ""Question"",
                            ""name"": ""Làm sao biết hashtag nào phù hợp với nội dung của mình?"",
                            ""acceptedAnswer"": {
                                ""@type"": ""Answer"",
                                ""text"": ""TrendTag giúp bạn dễ dàng tìm hashtag phù hợp qua 3 bước: 1) Chọn chủ đề - Lọc hashtag theo 16+ chủ đề (Giáo Dục, Du Lịch, Thời Trang, v.v.), 2) Xem mức độ cạnh tranh - Chọn hashtag có mức độ Thấp-Trung Bình, 3) Kiểm tra số lượt xem - Chọn hashtag có 100K-5M lượt xem cho tỷ lệ viral tốt nhất. Bạn cũng có thể sử dụng thanh tìm kiếm để tìm hashtag cụ thể và xem phân tích chi tiết.""
                            }
                        },
                        {
                            ""@type"": ""Question"",
                            ""name"": ""Có nên dùng cùng một bộ hashtag cho mọi video?"",
                            ""acceptedAnswer"": {
                                ""@type"": ""Answer"",
                                ""text"": ""Không nên! Mỗi video nên có bộ hashtag riêng biệt phù hợp với nội dung cụ thể. Sử dụng cùng một bộ hashtag cho mọi video có thể làm giảm hiệu quả tiếp cận (thuật toán TikTok phát hiện pattern lặp lại), không tối ưu cho từng loại nội dung khác nhau, và bỏ lỡ cơ hội từ hashtag trending mới. Hãy dành 2-3 phút để chọn hashtag phù hợp cho mỗi video bằng TrendTag!""
                            }
                        },
                        {
                            ""@type"": ""Question"",
                            ""name"": ""TrendTag có hỗ trợ các nền tảng khác ngoài TikTok không?"",
                            ""acceptedAnswer"": {
                                ""@type"": ""Answer"",
                                ""text"": ""Hiện tại, TrendTag tập trung 100% vào TikTok để mang lại dữ liệu chính xác và chuyên sâu nhất cho creator TikTok Việt Nam. Tuy nhiên, nhiều hashtag trending trên TikTok cũng hoạt động tốt trên Instagram Reels và YouTube Shorts. Trong tương lai, chúng tôi có kế hoạch mở rộng sang các nền tảng video ngắn khác dựa trên nhu cầu của cộng đồng.""
                            }
                        }
                    ]
                }
            ]";

            return new SeoMetadata
            {
                Title = title,
                Description = description,
                Keywords = keywords,
                CanonicalUrl = canonicalUrl,
                OgTitle = title,
                OgDescription = description,
                OgType = "website",
                PageType = "home",
                StructuredDataJson = structuredData
            };
        }
    }
}
