using HashTag.Data;
using HashTag.Models;
using HashTag.Repositories;
using HashTag.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;

namespace HashTag.Services;

/// <summary>
/// V2: Optimized 2-step AI approach to reduce tokens
/// Step 1: Find category from description (minimal tokens)
/// Step 2: Get top hashtags for that category by ID only (minimal output tokens)
/// </summary>
public partial class HashtagGeneratorService
{
    /// <summary>
    /// V2: Optimized hashtag generation with 2-step approach
    /// - Step 1: AI identifies category from description (~200 tokens)
    /// - Step 2: AI selects hashtag IDs from filtered list (~400 tokens)
    /// Total: ~600 tokens vs 2200 tokens in V1 (73% reduction!)
    /// </summary>
    public async Task<HashtagGeneratorResponse> GenerateHashtagsV2Async(
        string description,
        int? userId = null,
        string? ipAddress = null)
    {
        try
        {
            // 1. Check rate limit
            var (isAllowed, remaining) = await CheckRateLimitAsync(userId, ipAddress);
            if (!isAllowed)
            {
                return new HashtagGeneratorResponse
                {
                    Success = false,
                    ErrorMessage = $"Bạn đã vượt quá giới hạn sử dụng hôm nay. Còn lại: {remaining} lượt."
                };
            }

            // 2. Validate input
            if (string.IsNullOrWhiteSpace(description) || description.Length < 10)
            {
                return new HashtagGeneratorResponse
                {
                    Success = false,
                    ErrorMessage = "Vui lòng nhập mô tả video ít nhất 10 ký tự."
                };
            }

            // 3. Check cache
            var descriptionHash = ComputeHash(description.Trim().ToLower());
            var cachedResult = await GetCachedResultAsync(descriptionHash);
            if (cachedResult != null)
            {
                _logger.LogInformation("V2: Returning cached result for hash {Hash}", descriptionHash);

                var cachedRecommendation = JsonSerializer.Deserialize<HashtagRecommendation>(cachedResult.RecommendedHashtags);
                if (cachedRecommendation != null)
                {
                    await VerifyHashtagsExistenceAsync(cachedRecommendation);
                }

                return new HashtagGeneratorResponse
                {
                    Success = true,
                    Recommendation = cachedRecommendation,
                    GenerationId = cachedResult.Id
                };
            }

            // 4. Get trending hashtags
            var trendingHashtags = await _repository.GetTrendingHashtagsAsync(null);
            if (!trendingHashtags.Any())
            {
                return new HashtagGeneratorResponse
                {
                    Success = false,
                    ErrorMessage = "Không tìm thấy hashtag trending. Vui lòng thử lại sau."
                };
            }

            // STEP 1: AI identifies category (~200 tokens)
            _logger.LogInformation("V2 Step 1: Identifying category for description");
            var category = await IdentifyCategoryAsync(description);
            _logger.LogInformation("V2 Step 1: Category identified: {Category}", category);

            // STEP 2: Get relevant hashtags for the category
            var relevantHashtags = await GetHashtagsByCategoryAsync(category, trendingHashtags);
            _logger.LogInformation("V2 Step 2: Found {Count} relevant hashtags for category {Category}",
                relevantHashtags.Count, category);

            // STEP 3: AI selects hashtag IDs (~400 tokens)
            var recommendation = await SelectHashtagsV2Async(description, relevantHashtags);

            // 5. Verify hashtags existence
            await VerifyHashtagsExistenceAsync(recommendation);

            // 6. Save to database
            var generation = new HashtagGeneration
            {
                InputDescription = description,
                InputDescriptionHash = descriptionHash,
                RecommendedHashtags = JsonSerializer.Serialize(recommendation),
                GenerationMethod = "AI-V2",
                UserId = userId,
                GeneratedAt = DateTime.UtcNow,
                CachedUntil = DateTime.UtcNow.AddDays(_generatorOptions.CacheDurationDays)
            };

            _context.HashtagGenerations.Add(generation);
            await IncrementRateLimitAsync(userId, ipAddress);
            await _context.SaveChangesAsync();

            _logger.LogInformation("V2: Generated {Count} hashtags for user {UserId}",
                recommendation.TotalCount, userId);

            return new HashtagGeneratorResponse
            {
                Success = true,
                Recommendation = recommendation,
                GenerationId = generation.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "V2: Error generating hashtags: {Message}", ex.Message);
            return new HashtagGeneratorResponse
            {
                Success = false,
                ErrorMessage = "Đã xảy ra lỗi. Vui lòng thử lại sau."
            };
        }
    }

    #region V2 Private Methods

    /// <summary>
    /// Step 1: Use AI to identify category from description
    /// Input: ~150 tokens (description + categories list)
    /// Output: ~50 tokens (category name)
    /// </summary>
    private async Task<string> IdentifyCategoryAsync(string description)
    {
        try
        {
            var categories = await _context.HashtagCategories
                .Select(c => c.Name)
                .ToListAsync();

            var prompt = $@"Video: ""{description}""

Chọn 1 thể loại từ: {string.Join(", ", categories)}

CHỈ trả về JSON, KHÔNG giải thích:
{{""category"":""tên_thể_loại""}}";

            var systemPrompt = "Bạn là AI trả về JSON. CHỈ trả về JSON thuần, KHÔNG thêm text hay giải thích.";

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(15);

            string content;

            // Log provider for debugging
            _logger.LogInformation("V2 Step 1: Using Provider={Provider}, Endpoint={Endpoint}",
                _openAIOptions.Provider, _openAIOptions.ApiEndpoint);

            // Check if using Claude API
            if (_openAIOptions.Provider == "Claude")
            {
                content = await CallClaudeApiAsync(httpClient, systemPrompt, prompt, 50);
            }
            else
            {
                content = await CallOpenAIFormatApiAsync(httpClient, systemPrompt, prompt, 50);
            }

            _logger.LogInformation("V2 Step 1: AI content response: {Content}", content);

            // Extract category from JSON - handle AI response that might have markdown code blocks
            var cleanContent = content?.Trim() ?? "{}";
            if (cleanContent.StartsWith("```json"))
            {
                cleanContent = cleanContent.Replace("```json", "").Replace("```", "").Trim();
            }
            else if (cleanContent.StartsWith("```"))
            {
                cleanContent = cleanContent.Replace("```", "").Trim();
            }

            var categoryDoc = JsonDocument.Parse(cleanContent);
            var category = categoryDoc.RootElement.GetProperty("category").GetString();

            return category ?? categories.First();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "V2: Failed to identify category, using default");
            return "Giải Trí"; // Default fallback
        }
    }

    /// <summary>
    /// Get top hashtags for the identified category
    /// Returns: Top 20 trending + Top 80 category-specific = ~100 total (like V1)
    /// </summary>
    private async Task<List<TrendingHashtagDto>> GetHashtagsByCategoryAsync(
        string categoryName,
        List<TrendingHashtagDto> allHashtags)
    {
        // Get top 20 overall trending
        var topTrending = allHashtags.Take(20).ToList();

        // Verify category exists in database
        var category = await _context.HashtagCategories
            .FirstOrDefaultAsync(c => c.Name == categoryName);

        if (category == null)
        {
            _logger.LogWarning("V2: Category {CategoryName} not found, using top 100 trending", categoryName);
            return allHashtags.Take(100).ToList();
        }

        // Get top 80 from the category by CategoryName match
        var categoryHashtags = allHashtags
            .Where(h => h.CategoryName == categoryName)
            .Take(80)
            .ToList();

        // FALLBACK: If no hashtags found by CategoryName, query database directly by CategoryId
        if (categoryHashtags.Count == 0)
        {
            _logger.LogWarning("V2: No hashtags found with CategoryName={CategoryName}, querying by CategoryId={CategoryId}",
                categoryName, category.Id);

            // Get hashtag IDs from database that belong to this category
            var categoryHashtagIds = await _context.Hashtags
                .Where(h => h.CategoryId == category.Id && h.IsActive)
                .OrderByDescending(h => h.TotalAppearances)
                .Take(80)
                .Select(h => h.Id)
                .ToListAsync();

            // Match with allHashtags list
            categoryHashtags = allHashtags
                .Where(h => categoryHashtagIds.Contains(h.Id))
                .ToList();

            _logger.LogInformation("V2: Found {Count} hashtags by CategoryId query", categoryHashtags.Count);
        }

        // FALLBACK 2: If still empty, use keyword matching from category name
        if (categoryHashtags.Count == 0)
        {
            _logger.LogWarning("V2: Still no hashtags found, using keyword matching for category {CategoryName}", categoryName);

            // Extract keywords from category name (e.g., "Giáo Dục" -> ["giáo", "dục", "giao", "duc"])
            var categoryKeywords = categoryName.ToLower()
                .Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(k => k.Length >= 2)
                .ToList();

            categoryHashtags = allHashtags
                .Where(h => categoryKeywords.Any(k =>
                    h.Tag.ToLower().Contains(k) ||
                    (h.CategoryName?.ToLower().Contains(k) ?? false)))
                .Take(80)
                .ToList();

            _logger.LogInformation("V2: Found {Count} hashtags by keyword matching", categoryHashtags.Count);
        }

        // Combine and deduplicate
        var combined = topTrending.Union(categoryHashtags).ToList();

        _logger.LogInformation("V2: Combined {TrendingCount} trending + {CategoryCount} category hashtags = {TotalCount} total",
            topTrending.Count, categoryHashtags.Count, combined.Count);

        return combined;
    }

    /// <summary>
    /// Step 2: AI selects hashtag IDs from filtered list
    /// Input: ~1000 tokens (description + ~100 hashtags with views/posts)
    /// Output: ~150 tokens (5-7 hashtag IDs)
    /// Total V2: ~1150 tokens vs 2225 tokens in V1 (48% reduction)
    /// </summary>
    private async Task<HashtagRecommendation> SelectHashtagsV2Async(
        string description,
        List<TrendingHashtagDto> hashtags)
    {
        try
        {
            // Build hashtag list with ViewCount for better AI selection
            var hashtagList = string.Join("\n", hashtags.Select(h =>
            {
                var views = FormatNumber(h.LatestViewCount ?? 0);
                var posts = FormatNumber(h.LatestPostCount ?? 0);
                return $"{h.Id}. #{h.Tag} ({views} views, {posts} posts)";
            }));

            var prompt = $@"Mô tả video: ""{description}""

Chọn 5-7 hashtag PHÙ HỢP NHẤT với nội dung video:
{hashtagList}

Phân loại:
- trending: 1-2 hashtag (views cao, viral)
- niche: 2-3 hashtag (liên quan trực tiếp đến chủ đề)
- ultra: 1-2 hashtag (siêu chi tiết, cụ thể)

Trả về JSON với array ID số:
{{""trending"":[1,2],""niche"":[3,4],""ultra"":[5],""reasoning"":""ngắn gọn""}}";

            var systemPrompt = "Bạn là AI trả về JSON. CHỈ trả về JSON thuần, KHÔNG thêm text hay giải thích.";

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(20);

            _logger.LogInformation("V2 Step 2: Calling AI to select hashtags");

            string content;

            // Check if using Claude API
            if (_openAIOptions.Provider == "Claude")
            {
                content = await CallClaudeApiAsync(httpClient, systemPrompt, prompt, 200);
            }
            else
            {
                content = await CallOpenAIFormatApiAsync(httpClient, systemPrompt, prompt, 200);
            }

            _logger.LogInformation("V2 Step 2: AI content response: {Content}", content);

            // Parse selected IDs - handle AI response that might have markdown code blocks
            var cleanContent = content?.Trim() ?? "{}";
            if (cleanContent.StartsWith("```json"))
            {
                cleanContent = cleanContent.Replace("```json", "").Replace("```", "").Trim();
            }
            else if (cleanContent.StartsWith("```"))
            {
                cleanContent = cleanContent.Replace("```", "").Trim();
            }

            _logger.LogInformation("V2 Step 2: Cleaned content for parsing: {CleanContent}", cleanContent);

            var selectionDoc = JsonDocument.Parse(cleanContent);

            // Parse with safe defaults
            var trendingIds = new List<int>();
            var nicheIds = new List<int>();
            var ultraIds = new List<int>();
            var reasoning = "Chiến lược hashtag cân bằng giữa reach và engagement.";

            // Track used IDs to prevent duplicates across categories
            var usedIds = new HashSet<int>();

            if (selectionDoc.RootElement.TryGetProperty("trending", out var trendingProp))
            {
                trendingIds = trendingProp.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out _))
                    .Select(e => e.GetInt32())
                    .Where(id => usedIds.Add(id)) // Only add if not already used
                    .ToList();
            }

            if (selectionDoc.RootElement.TryGetProperty("niche", out var nicheProp))
            {
                nicheIds = nicheProp.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out _))
                    .Select(e => e.GetInt32())
                    .Where(id => usedIds.Add(id)) // Only add if not already used
                    .ToList();
            }

            if (selectionDoc.RootElement.TryGetProperty("ultra", out var ultraProp))
            {
                ultraIds = ultraProp.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.Number && e.TryGetInt32(out _))
                    .Select(e => e.GetInt32())
                    .Where(id => usedIds.Add(id)) // Only add if not already used
                    .ToList();
            }

            if (selectionDoc.RootElement.TryGetProperty("reasoning", out var reasoningProp))
            {
                reasoning = reasoningProp.GetString() ?? reasoning;
            }

            _logger.LogInformation("V2: Parsed IDs - Trending: [{Trending}], Niche: [{Niche}], Ultra: [{Ultra}]",
                string.Join(",", trendingIds), string.Join(",", nicheIds), string.Join(",", ultraIds));

            // Validate: Ensure we have at least some hashtags
            if (trendingIds.Count == 0 && nicheIds.Count == 0 && ultraIds.Count == 0)
            {
                _logger.LogWarning("V2: AI returned no hashtags, using fallback selection");
                // Fallback: Select first 2 trending, next 3 as niche, next 2 as ultra
                trendingIds = hashtags.Take(2).Select(h => h.Id).ToList();
                nicheIds = hashtags.Skip(2).Take(3).Select(h => h.Id).ToList();
                ultraIds = hashtags.Skip(5).Take(2).Select(h => h.Id).ToList();
            }

            // Get full hashtag details from database
            var selectedIds = trendingIds.Union(nicheIds).Union(ultraIds).ToList();
            var selectedHashtags = hashtags.Where(h => selectedIds.Contains(h.Id)).ToList();

            _logger.LogInformation("V2: AI selected {Count} hashtags (IDs: {Ids})",
                selectedIds.Count, string.Join(",", selectedIds));

            // Build recommendation with full details
            return new HashtagRecommendation
            {
                TrendingHashtags = trendingIds
        .Select(id => hashtags.FirstOrDefault(x => x.Id == id))
        .Where(h => h != null)
        .Select(h => CreateRecommendedHashtag(h, "Cao", 10))
        .ToList(),

                NicheHashtags = nicheIds
        .Select(id => hashtags.FirstOrDefault(x => x.Id == id))
        .Where(h => h != null)
        .Select(h => CreateRecommendedHashtag(h, "Trung Bình", 7))
        .ToList(),

                UltraNicheHashtags = ultraIds
        .Select(id => hashtags.FirstOrDefault(x => x.Id == id))
        .Where(h => h != null)
        .Select(h => CreateRecommendedHashtag(h, "Thấp", 5))
        .ToList(),

                Reasoning = reasoning ?? "Chiến lược hashtag cân bằng giữa reach và engagement."
            };

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "V2: Failed to select hashtags with AI");
            throw;
        }
    }

    /// <summary>
    /// Format large numbers for better readability (e.g., 1.5M, 234K)
    /// </summary>
    private static string FormatNumber(long number)
    {
        if (number >= 1_000_000_000)
            return $"{number / 1_000_000_000.0:0.#}B";
        if (number >= 1_000_000)
            return $"{number / 1_000_000.0:0.#}M";
        if (number >= 1_000)
            return $"{number / 1_000.0:0.#}K";
        return number.ToString();
    }

    #endregion
}
