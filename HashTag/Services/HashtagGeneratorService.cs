using HashTag.Data;
using HashTag.Helpers;
using HashTag.Models;
using HashTag.Options;
using HashTag.Repositories;
using HashTag.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HashTag.Services;

public partial class HashtagGeneratorService : IHashtagGeneratorService
{
    private readonly TrendTagDbContext _context;
    private readonly IHashtagRepository _repository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenAIOptions _openAIOptions;
    private readonly HashtagGeneratorOptions _generatorOptions;
    private readonly ILogger<HashtagGeneratorService> _logger;

    public HashtagGeneratorService(
        TrendTagDbContext context,
        IHashtagRepository repository,
        IHttpClientFactory httpClientFactory,
        IOptions<OpenAIOptions> openAIOptions,
        IOptions<HashtagGeneratorOptions> generatorOptions,
        ILogger<HashtagGeneratorService> logger)
    {
        _context = context;
        _repository = repository;
        _httpClientFactory = httpClientFactory;
        _openAIOptions = openAIOptions.Value;
        _generatorOptions = generatorOptions.Value;
        _logger = logger;
    }

    public async Task<HashtagGeneratorResponse> GenerateHashtagsAsync(string description, int? userId = null, string? ipAddress = null)
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

            if (description.Length > 1000)
            {
                return new HashtagGeneratorResponse
                {
                    Success = false,
                    ErrorMessage = "Mô tả quá dài. Vui lòng giới hạn trong 1000 ký tự."
                };
            }

            // 3. Check cache
            var descriptionHash = ComputeHash(description.Trim().ToLower());
            var cachedResult = await GetCachedResultAsync(descriptionHash);
            if (cachedResult != null)
            {
                _logger.LogInformation("Returning cached result for hash {Hash}", descriptionHash);

                var cachedRecommendation = JsonSerializer.Deserialize<HashtagRecommendation>(cachedResult.RecommendedHashtags);

                // Verify hashtags existence even for cached results
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

            // 4. Get trending hashtags from database
            var trendingHashtags = await _repository.GetTrendingHashtagsAsync(null);
            if (!trendingHashtags.Any())
            {
                return new HashtagGeneratorResponse
                {
                    Success = false,
                    ErrorMessage = "Không tìm thấy hashtag trending. Vui lòng thử lại sau."
                };
            }

            // 5. Call OpenAI API (use top 100 to give AI more relevant options)
            var recommendation = await CallOpenAIAsync(description, trendingHashtags.Take(100).ToList());

            // 5.5. Verify hashtags exist in database and mark non-existent ones
            await VerifyHashtagsExistenceAsync(recommendation);

            // 6. Save to database
            var generation = new HashtagGeneration
            {
                InputDescription = description,
                InputDescriptionHash = descriptionHash,
                RecommendedHashtags = JsonSerializer.Serialize(recommendation),
                GenerationMethod = "AI",
                UserId = userId,
                GeneratedAt = DateTime.UtcNow,
                CachedUntil = DateTime.UtcNow.AddDays(_generatorOptions.CacheDurationDays),
                TokensUsed = null // Will be updated if we track tokens
            };

            _context.HashtagGenerations.Add(generation);

            // 7. Update rate limit
            await IncrementRateLimitAsync(userId, ipAddress);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Generated {Count} hashtags for user {UserId}, generation ID {GenerationId}",
                recommendation.TotalCount, userId, generation.Id);

            return new HashtagGeneratorResponse
            {
                Success = true,
                Recommendation = recommendation,
                GenerationId = generation.Id
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "OpenAI API request failed: {Message}", ex.Message);
            return new HashtagGeneratorResponse
            {
                Success = false,
                ErrorMessage = "Không thể kết nối với dịch vụ AI. Vui lòng thử lại sau."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating hashtags: {Message}", ex.Message);
            return new HashtagGeneratorResponse
            {
                Success = false,
                ErrorMessage = "Đã xảy ra lỗi. Vui lòng thử lại sau."
            };
        }
    }

    public async Task<(bool IsAllowed, int RemainingGenerations)> CheckRateLimitAsync(int? userId, string? ipAddress)
    {
        var now = DateTime.UtcNow;
        var windowStart = now.Date; // Daily limit (reset at midnight UTC)

        var rateLimit = await _context.GenerationRateLimits
            .FirstOrDefaultAsync(r =>
                (userId.HasValue && r.UserId == userId) ||
                (!userId.HasValue && r.IpAddress == ipAddress));

        if (rateLimit == null)
        {
            // First generation for this user/IP
            return (true, _generatorOptions.FreeUserDailyLimit - 1);
        }

        // Check if window has expired (new day)
        if (rateLimit.WindowStartTime.Date < windowStart)
        {
            // Reset counter
            rateLimit.GenerationCount = 0;
            rateLimit.WindowStartTime = windowStart;
            await _context.SaveChangesAsync();
            return (true, _generatorOptions.FreeUserDailyLimit - 1);
        }

        // Check limit
        var limit = userId.HasValue ? _generatorOptions.PremiumUserDailyLimit : _generatorOptions.FreeUserDailyLimit;
        var remaining = limit - rateLimit.GenerationCount;

        return (remaining > 0, Math.Max(0, remaining - 1));
    }

    public async Task<string> TestConnectionAsync()
    {
        try
        {
            var response = await CallOpenAIAsync("Test connection", new List<TrendingHashtagDto>());
            return "OpenAI connection successful!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI connection test failed");
            throw;
        }
    }

    #region Private Methods

    private async Task<HashtagRecommendation> CallOpenAIAsync(string description, List<TrendingHashtagDto> trendingHashtags)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            var prompt = BuildPrompt(description, trendingHashtags);
            var systemPrompt = "Bạn là chuyên gia TikTok hashtag cho thị trường Việt Nam. Luôn trả về JSON hợp lệ.";

            _logger.LogInformation($"Calling {_openAIOptions.Provider} API at {_openAIOptions.ApiEndpoint}");

            string content;

            // Check if using Claude API (different format)
            if (_openAIOptions.Provider == "Claude")
            {
                content = await CallClaudeApiAsync(httpClient, systemPrompt, prompt, _openAIOptions.MaxTokens);
            }
            else
            {
                // OpenAI/Groq format
                content = await CallOpenAIFormatApiAsync(httpClient, systemPrompt, prompt, _openAIOptions.MaxTokens);
            }

            if (string.IsNullOrEmpty(content))
            {
                _logger.LogError($"{_openAIOptions.Provider} returned empty content");
                return await GenerateHashtagsRuleBased(description, trendingHashtags);
            }

            // Extract JSON from markdown code block if present
            var jsonContent = content;
            if (content.Contains("```json"))
            {
                var startIndex = content.IndexOf("```json") + 7;
                var endIndex = content.IndexOf("```", startIndex);
                if (endIndex > startIndex)
                {
                    jsonContent = content.Substring(startIndex, endIndex - startIndex).Trim();
                    _logger.LogInformation("Extracted JSON from markdown code block");
                }
            }
            else if (content.Contains("```"))
            {
                var startIndex = content.IndexOf("```") + 3;
                var endIndex = content.IndexOf("```", startIndex);
                if (endIndex > startIndex)
                {
                    jsonContent = content.Substring(startIndex, endIndex - startIndex).Trim();
                    _logger.LogInformation("Extracted JSON from generic code block");
                }
            }

            // Parse JSON response
            var recommendation = JsonSerializer.Deserialize<HashtagRecommendation>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (recommendation == null)
            {
                _logger.LogError("Failed to parse AI response");
                return await GenerateHashtagsRuleBased(description, trendingHashtags);
            }

            // Log what we got from AI
            _logger.LogInformation($"{_openAIOptions.Provider} API call successful");
            _logger.LogInformation($"Trending: {recommendation.TrendingHashtags?.Count ?? 0}, Niche: {recommendation.NicheHashtags?.Count ?? 0}, Ultra: {recommendation.UltraNicheHashtags?.Count ?? 0}");
            _logger.LogInformation($"Reasoning: {recommendation.Reasoning?.Substring(0, Math.Min(100, recommendation.Reasoning?.Length ?? 0))}...");

            return recommendation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calling {_openAIOptions.Provider} API: {ex.Message}");
            // Fallback to rule-based generator
            _logger.LogInformation("Falling back to rule-based generator due to error");
            return await GenerateHashtagsRuleBased(description, trendingHashtags);
        }
    }

    /// <summary>
    /// Call Claude API (Anthropic) - different format from OpenAI/Groq
    /// </summary>
    private async Task<string> CallClaudeApiAsync(HttpClient httpClient, string systemPrompt, string userPrompt, int maxTokens)
    {
        var requestBody = new
        {
            model = _openAIOptions.Model,
            max_tokens = maxTokens,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = userPrompt }
            }
        };

        // Claude uses different headers
        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("x-api-key", _openAIOptions.ApiKey);
        httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var response = await httpClient.PostAsJsonAsync(_openAIOptions.ApiEndpoint, requestBody);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError($"Claude API error ({response.StatusCode}): {errorContent}");
            return string.Empty;
        }

        var responseText = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"Raw Claude API response length: {responseText.Length} characters");

        // Parse Claude response format: { "content": [{ "type": "text", "text": "..." }] }
        var responseBody = JsonSerializer.Deserialize<JsonElement>(responseText);
        var contentArray = responseBody.GetProperty("content");
        if (contentArray.GetArrayLength() > 0)
        {
            var firstContent = contentArray[0];
            if (firstContent.TryGetProperty("text", out var textElement))
            {
                return textElement.GetString() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Call OpenAI/Groq format API (for backup/switching providers)
    /// </summary>
    private async Task<string> CallOpenAIFormatApiAsync(HttpClient httpClient, string systemPrompt, string userPrompt, int maxTokens)
    {
        var requestBody = new
        {
            model = _openAIOptions.Model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            max_tokens = maxTokens,
            temperature = _openAIOptions.Temperature
        };

        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_openAIOptions.ApiKey}");

        var response = await httpClient.PostAsJsonAsync(_openAIOptions.ApiEndpoint, requestBody);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError($"{_openAIOptions.Provider} API error ({response.StatusCode}): {errorContent}");
            return string.Empty;
        }

        var responseText = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"Raw API response length: {responseText.Length} characters");

        var responseBody = JsonSerializer.Deserialize<JsonElement>(responseText);
        var content = responseBody.GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content ?? string.Empty;
    }

    /// <summary>
    /// Rule-based hashtag generator - works without OpenAI
    /// </summary>
    private async Task<HashtagRecommendation> GenerateHashtagsRuleBased(string description, List<TrendingHashtagDto> trendingHashtags)
    {
        // Extract keywords from description
        var keywords = ExtractKeywords(description);

        // Score and rank hashtags based on relevance
        var scoredHashtags = trendingHashtags
            .Select(h => new
            {
                Hashtag = h,
                Score = CalculateRelevanceScore(h, keywords, description)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ToList();

        // Categorize hashtags - ensure no duplicates between categories
        var usedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var trending = scoredHashtags
            .Where(x => x.Hashtag.DifficultyLevel == "Hard" && x.Hashtag.LatestPostCount >= 100_000_000)
            .Where(x => usedTags.Add(x.Hashtag.Tag)) // Only add if not already used
            .Take(2)
            .Select(x => CreateRecommendedHashtag(x.Hashtag, "Cao", x.Score))
            .ToList();

        var niche = scoredHashtags
            .Where(x => (x.Hashtag.DifficultyLevel == "Medium" || x.Hashtag.DifficultyLevel == "Hard") &&
                       x.Hashtag.LatestPostCount >= 1_000_000 && x.Hashtag.LatestPostCount < 100_000_000)
            .Where(x => usedTags.Add(x.Hashtag.Tag)) // Only add if not already used
            .Take(3)
            .Select(x => CreateRecommendedHashtag(x.Hashtag, "Trung Bình", x.Score))
            .ToList();

        var ultraNiche = scoredHashtags
            .Where(x => x.Hashtag.DifficultyLevel == "Easy" && x.Hashtag.LatestPostCount < 1_000_000)
            .Where(x => usedTags.Add(x.Hashtag.Tag)) // Only add if not already used
            .Take(2)
            .Select(x => CreateRecommendedHashtag(x.Hashtag, "Thấp", x.Score))
            .ToList();

        // Generate reasoning
        var reasoning = GenerateReasoning(trending.Count, niche.Count, ultraNiche.Count, keywords);

        return new HashtagRecommendation
        {
            TrendingHashtags = trending,
            NicheHashtags = niche,
            UltraNicheHashtags = ultraNiche,
            Reasoning = reasoning
        };
    }

    private List<string> ExtractKeywords(string description)
    {
        // Simple keyword extraction - remove common Vietnamese stop words
        var stopWords = new HashSet<string> { "của", "và", "cho", "với", "tại", "về", "từ", "đến", "trong", "ngoài", "trên", "dưới", "là", "có", "không", "được", "này", "đó", "các", "những", "một", "để", "khi", "nếu", "thì", "như", "vì", "nhưng", "mà", "hay", "hoặc" };

        return description
            .ToLower()
            .Split(new[] { ' ', ',', '.', '!', '?', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Length >= 3 && !stopWords.Contains(word))
            .Distinct()
            .ToList();
    }

    private double CalculateRelevanceScore(TrendingHashtagDto hashtag, List<string> keywords, string description)
    {
        double score = 0;

        var hashtagLower = hashtag.Tag.ToLower();
        var descriptionLower = description.ToLower();

        // Exact match with any keyword
        if (keywords.Any(k => hashtagLower.Contains(k) || k.Contains(hashtagLower)))
        {
            score += 10;
        }

        // Partial match
        if (keywords.Any(k => hashtagLower.Contains(k.Substring(0, Math.Min(k.Length, 4)))))
        {
            score += 5;
        }

        // Hashtag appears in description
        if (descriptionLower.Contains(hashtagLower))
        {
            score += 15;
        }

        // Bonus for trending hashtags
        if (hashtag.LatestPostCount > 100_000_000)
        {
            score += 3;
        }

        // Bonus for category match (if any keywords match category)
        if (hashtag.CategoryName != null && keywords.Any(k => hashtag.CategoryName.ToLower().Contains(k)))
        {
            score += 5;
        }

        return score;
    }

    private RecommendedHashtag CreateRecommendedHashtag(TrendingHashtagDto hashtag, string competitionLevel, double relevanceScore)
    {
        var viewCount = hashtag.LatestViewCount ?? 0;
        var viralProbability = CalculateViralProbability(viewCount, competitionLevel, relevanceScore);
        var expectedReach = EstimateReach(viewCount, competitionLevel);

        return new RecommendedHashtag
        {
            Tag = hashtag.Tag,
            ViewCount = viewCount,
            CompetitionLevel = competitionLevel,
            ExpectedReach = expectedReach,
            ViralProbability = viralProbability,
            RecommendationNote = GenerateNote(hashtag, competitionLevel, viralProbability),
            IsSelected = true
        };
    }

    private int CalculateViralProbability(long viewCount, string competitionLevel, double relevanceScore)
    {
        int baseProbability = competitionLevel switch
        {
            "Thấp" => 70,      // Ultra-niche: high chance within small community
            "Trung Bình" => 50, // Niche: moderate chance
            "Cao" => 30,        // Trending: low chance due to high competition
            _ => 40
        };

        // Adjust based on relevance
        int bonus = (int)(relevanceScore * 2);
        return Math.Min(95, baseProbability + bonus);
    }

    private string EstimateReach(long viewCount, string competitionLevel)
    {
        return competitionLevel switch
        {
            "Thấp" when viewCount < 100_000 => "500-2K người",
            "Thấp" => "2K-10K người",
            "Trung Bình" when viewCount < 10_000_000 => "10K-50K người",
            "Trung Bình" => "50K-200K người",
            "Cao" when viewCount < 100_000_000 => "100K-500K người",
            "Cao" => "500K-2M người",
            _ => "5K-20K người"
        };
    }

    private string GenerateNote(TrendingHashtagDto hashtag, string competitionLevel, int viralProbability)
    {
        if (competitionLevel == "Thấp" && viralProbability >= 70)
        {
            return "Đề xuất: Dễ viral cho creators mới";
        }
        else if (competitionLevel == "Trung Bình" && viralProbability >= 50)
        {
            return "Cân bằng tốt giữa tiếp cận và cạnh tranh";
        }
        else if (competitionLevel == "Cao" && hashtag.LatestPostCount > 500_000_000)
        {
            return "Hashtag siêu hot, cạnh tranh cao nhưng tiếp cận rộng";
        }
        else if (hashtag.CategoryName != null)
        {
            return $"Phù hợp với chủ đề {hashtag.CategoryName}";
        }
        return "Hashtag phù hợp với nội dung của bạn";
    }

    private string GenerateReasoning(int trendingCount, int nicheCount, int ultraNicheCount, List<string> keywords)
    {
        var totalCount = trendingCount + nicheCount + ultraNicheCount;

        if (totalCount == 0)
        {
            return "Không tìm thấy hashtag phù hợp. Hãy thử mô tả chi tiết hơn về nội dung video của bạn.";
        }

        var parts = new List<string>();

        if (trendingCount > 0)
        {
            parts.Add($"{trendingCount} hashtag trending để tăng khả năng tiếp cận rộng");
        }

        if (nicheCount > 0)
        {
            parts.Add($"{nicheCount} hashtag niche để tối ưu hóa đúng đối tượng");
        }

        if (ultraNicheCount > 0)
        {
            parts.Add($"{ultraNicheCount} hashtag chuyên biệt để dễ nổi bật");
        }

        var keywordText = keywords.Count > 0 ? $" dựa trên từ khóa: {string.Join(", ", keywords.Take(5))}" : "";

        return $"Đề xuất {totalCount} hashtag phù hợp: {string.Join(", ", parts)}{keywordText}. " +
               "Chiến lược này giúp cân bằng giữa tiếp cận rộng và khả năng viral.";
    }

    private string BuildPrompt(string description, List<TrendingHashtagDto> trendingHashtags)
    {
        var trendingList = string.Join("\n",
            trendingHashtags.Select(h =>
                $"- #{h.Tag} ({GetDifficultyText(h.DifficultyLevel)}, {VietnameseHelper.FormatNumber(h.LatestPostCount ?? 0)} lượt xem)"
            ));

        return $@"Bạn là chuyên gia TikTok hashtag cho thị trường Việt Nam. Hãy đề xuất hashtag tối ưu cho video này.

Mô tả video: ""{description}""

Danh sách hashtag đang trending (tham khảo):
{trendingList}

Yêu cầu QUAN TRỌNG:
1. Đề xuất tổng cộng 5-7 hashtag
2. TẤT CẢ hashtag PHẢI LIÊN QUAN TRỰC TIẾP đến nội dung video
3. Phân loại:
   - Trending (1-2 hashtag): Hashtag đang viral, lượng tìm kiếm cao, CÓ LIÊN QUAN đến video
   - Niche (2-3 hashtag): Hashtag chuyên ngành, cạnh tranh vừa phải
   - Ultra Niche (1-2 hashtag): Hashtag siêu chuyên biệt, dễ nổi bật
4. Nếu KHÔNG có hashtag trending nào phù hợp trong danh sách, hãy TỰ TẠO hashtag mới liên quan đến video
5. Ưu tiên hashtag phù hợp với thị trường Việt Nam
6. Với video về bóng đá → dùng hashtag về bóng đá, KHÔNG dùng hashtag về thời trang/ăn uống
7. Kèm theo lý do chọn và dự đoán hiệu quả

Trả về JSON với cấu trúc:
{{
  ""TrendingHashtags"": [
    {{
      ""Tag"": ""tên_hashtag"",
      ""ViewCount"": 1000000,
      ""PostCount"": 50000,
      ""CompetitionLevel"": ""Cao|Trung Bình|Thấp"",
      ""ExpectedReach"": ""100K-500K người"",
      ""ViralProbability"": 75,
      ""RecommendationNote"": ""Lý do chọn hashtag này""
    }}
  ],
  ""NicheHashtags"": [...],
  ""UltraNicheHashtags"": [...],
  ""Reasoning"": ""Giải thích tổng quan về chiến lược hashtag này""
}}";
    }

    private RecommendedHashtag MapToRecommendedHashtag(AIHashtagItem item)
    {
        return new RecommendedHashtag
        {
            Tag = item.Tag ?? "",
            ViewCount = item.ViewCount ?? 0,
            CompetitionLevel = item.CompetitionLevel ?? "Trung Bình",
            ExpectedReach = item.ExpectedReach ?? "Không rõ",
            ViralProbability = item.ViralProbability ?? 50,
            RecommendationNote = item.RecommendationNote,
            IsSelected = true
        };
    }

    private string GetDifficultyText(string? difficulty)
    {
        return difficulty?.ToLower() switch
        {
            "easy" => "Dễ",
            "medium" => "Trung Bình",
            "hard" => "Khó",
            _ => "Trung Bình"
        };
    }

    private string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    private async Task<HashtagGeneration?> GetCachedResultAsync(string descriptionHash)
    {
        if (!_generatorOptions.EnableCaching)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        return await _context.HashtagGenerations
            .Where(g => g.InputDescriptionHash == descriptionHash &&
                       g.CachedUntil.HasValue &&
                       g.CachedUntil.Value > now)
            .OrderByDescending(g => g.GeneratedAt)
            .FirstOrDefaultAsync();
    }

    private async Task IncrementRateLimitAsync(int? userId, string? ipAddress)
    {
        var now = DateTime.UtcNow;
        var windowStart = now.Date;

        var rateLimit = await _context.GenerationRateLimits
            .FirstOrDefaultAsync(r =>
                (userId.HasValue && r.UserId == userId) ||
                (!userId.HasValue && r.IpAddress == ipAddress));

        if (rateLimit == null)
        {
            rateLimit = new GenerationRateLimit
            {
                UserId = userId,
                IpAddress = ipAddress,
                GenerationCount = 1,
                WindowStartTime = windowStart
            };
            _context.GenerationRateLimits.Add(rateLimit);
        }
        else
        {
            rateLimit.GenerationCount++;
        }

        await _context.SaveChangesAsync();
    }

    #endregion

    #region OpenAI Response Models

    private class OpenAIResponse
    {
        public List<Choice>? Choices { get; set; }
    }

    private class Choice
    {
        public Message? Message { get; set; }
    }

    private class Message
    {
        public string? Content { get; set; }
    }

    private class AIHashtagResponse
    {
        public List<AIHashtagItem>? Trending { get; set; }
        public List<AIHashtagItem>? Niche { get; set; }
        public List<AIHashtagItem>? UltraNiche { get; set; }
        public string? Reasoning { get; set; }
    }

    private class AIHashtagItem
    {
        public string? Tag { get; set; }
        public long? ViewCount { get; set; }
        public string? CompetitionLevel { get; set; }
        public string? ExpectedReach { get; set; }
        public int? ViralProbability { get; set; }
        public string? RecommendationNote { get; set; }
    }

    #endregion

    #region Hashtag Verification

    private async Task VerifyHashtagsExistenceAsync(HashtagRecommendation recommendation)
    {
        // Get all tags from recommendation
        var allTags = new List<string>();
        allTags.AddRange(recommendation.TrendingHashtags.Select(h => h.Tag));
        allTags.AddRange(recommendation.NicheHashtags.Select(h => h.Tag));
        allTags.AddRange(recommendation.UltraNicheHashtags.Select(h => h.Tag));

        // Remove # prefix and normalize
        var normalizedTags = allTags
            .Select(t => t.TrimStart('#').ToLower())
            .Distinct()
            .ToList();

        // ⚡ OPTIMIZED: Avoid ToLower() in SQL - normalize in C# instead
        // Load hashtags by exact match (Tag column is already lowercase)
        var existingTags = await _context.Hashtags
            .Where(h => normalizedTags.Contains(h.Tag))
            .Select(h => h.Tag)
            .ToListAsync();

        var existingTagsSet = new HashSet<string>(existingTags, StringComparer.OrdinalIgnoreCase);

        // Mark hashtags that don't exist
        foreach (var hashtag in recommendation.TrendingHashtags)
        {
            var normalizedTag = hashtag.Tag.TrimStart('#').ToLower();
            hashtag.ExistsInDatabase = existingTagsSet.Contains(normalizedTag);
        }

        foreach (var hashtag in recommendation.NicheHashtags)
        {
            var normalizedTag = hashtag.Tag.TrimStart('#').ToLower();
            hashtag.ExistsInDatabase = existingTagsSet.Contains(normalizedTag);
        }

        foreach (var hashtag in recommendation.UltraNicheHashtags)
        {
            var normalizedTag = hashtag.Tag.TrimStart('#').ToLower();
            hashtag.ExistsInDatabase = existingTagsSet.Contains(normalizedTag);
        }

        _logger.LogInformation("Verified {Total} hashtags: {Existing} exist in database, {Missing} missing",
            normalizedTags.Count,
            existingTags.Count,
            normalizedTags.Count - existingTags.Count);
    }

    #endregion
}

