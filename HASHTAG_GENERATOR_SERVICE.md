# Hashtag Generator Service - Implementation

Full implementation code for AI-powered hashtag generator service.

---

## 📁 File Structure

```
HashTag/
├── Services/
│   ├── IHashtagGeneratorService.cs (Interface)
│   └── HashtagGeneratorService.cs (Implementation)
├── Options/
│   └── OpenAIOptions.cs (Configuration)
├── ViewModels/
│   └── HashtagGeneratorViewModel.cs ✅ (Created)
├── Models/
│   └── HashtagGeneration.cs ✅ (Created)
└── Controllers/
    └── HashtagGeneratorController.cs (API)
```

---

## 🔧 Implementation Code

### 1. Create Interface: `Services/IHashtagGeneratorService.cs`

```csharp
using HashTag.ViewModels;

namespace HashTag.Services;

public interface IHashtagGeneratorService
{
    /// <summary>
    /// Generate hashtag recommendations using AI
    /// </summary>
    Task<HashtagGeneratorResponse> GenerateHashtagsAsync(string description, int? userId = null, string? ipAddress = null);

    /// <summary>
    /// Check if user/IP has exceeded rate limit
    /// </summary>
    Task<(bool allowed, int remaining)> CheckRateLimitAsync(int? userId, string? ipAddress);

    /// <summary>
    /// Mark generation as copied by user
    /// </summary>
    Task MarkAsCopiedAsync(int generationId);

    /// <summary>
    /// Submit user feedback for a generation
    /// </summary>
    Task SubmitFeedbackAsync(int generationId, string feedback);

    /// <summary>
    /// Test OpenAI connection
    /// </summary>
    Task<string> TestConnectionAsync();
}
```

### 2. Create Service: `Services/HashtagGeneratorService.cs`

```csharp
using Azure.AI.OpenAI;
using Azure;
using HashTag.Data;
using HashTag.Models;
using HashTag.Repositories;
using HashTag.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HashTag.Services;

public class HashtagGeneratorService : IHashtagGeneratorService
{
    private readonly TrendTagDbContext _context;
    private readonly IHashtagRepository _hashtagRepository;
    private readonly ILogger<HashtagGeneratorService> _logger;
    private readonly OpenAIOptions _openAIOptions;
    private readonly HashtagGeneratorOptions _generatorOptions;
    private readonly OpenAIClient _openAIClient;

    public HashtagGeneratorService(
        TrendTagDbContext context,
        IHashtagRepository hashtagRepository,
        ILogger<HashtagGeneratorService> logger,
        IOptions<OpenAIOptions> openAIOptions,
        IOptions<HashtagGeneratorOptions> generatorOptions)
    {
        _context = context;
        _hashtagRepository = hashtagRepository;
        _logger = logger;
        _openAIOptions = openAIOptions.Value;
        _generatorOptions = generatorOptions.Value;

        // Initialize OpenAI client
        _openAIClient = new OpenAIClient(_openAIOptions.ApiKey);
    }

    public async Task<HashtagGeneratorResponse> GenerateHashtagsAsync(
        string description,
        int? userId = null,
        string? ipAddress = null)
    {
        try
        {
            // Step 1: Check rate limit
            var (allowed, remaining) = await CheckRateLimitAsync(userId, ipAddress);
            if (!allowed)
            {
                return new HashtagGeneratorResponse
                {
                    Success = false,
                    ErrorMessage = $"Đã vượt quá giới hạn. Còn lại: {remaining} lượt hôm nay."
                };
            }

            // Step 2: Check cache
            var descriptionHash = ComputeHash(description);
            var cached = await GetCachedResultAsync(descriptionHash);

            if (cached != null)
            {
                _logger.LogInformation("Cache hit for description hash: {Hash}", descriptionHash);
                return new HashtagGeneratorResponse
                {
                    Success = true,
                    Recommendation = JsonSerializer.Deserialize<HashtagRecommendation>(cached.RecommendedHashtags),
                    GenerationId = cached.Id
                };
            }

            // Step 3: Get trending hashtags from database
            var trendingHashtags = await _hashtagRepository.GetTrendingHashtagsAsync(limit: 50);

            // Step 4: Build prompt for GPT
            var prompt = BuildPrompt(description, trendingHashtags.ToList());

            // Step 5: Call OpenAI API
            var gptResponse = await CallOpenAIAsync(prompt);

            // Step 6: Parse response
            var recommendation = ParseGPTResponse(gptResponse);

            // Step 7: Save to database
            var generation = new HashtagGeneration
            {
                InputDescription = description,
                InputDescriptionHash = descriptionHash,
                RecommendedHashtags = JsonSerializer.Serialize(recommendation),
                GenerationMethod = "AI",
                UserId = userId,
                GeneratedAt = DateTime.UtcNow,
                CachedUntil = DateTime.UtcNow.AddDays(_generatorOptions.CacheDurationDays),
                TokensUsed = CountTokens(prompt) + CountTokens(gptResponse)
            };

            _context.HashtagGenerations.Add(generation);
            await _context.SaveChangesAsync();

            // Step 8: Update rate limit
            await IncrementRateLimitAsync(userId, ipAddress);

            return new HashtagGeneratorResponse
            {
                Success = true,
                Recommendation = recommendation,
                GenerationId = generation.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating hashtags: {Message}", ex.Message);

            return new HashtagGeneratorResponse
            {
                Success = false,
                ErrorMessage = "Có lỗi xảy ra. Vui lòng thử lại sau."
            };
        }
    }

    private string BuildPrompt(string description, List<TrendingHashtagDto> trendingHashtags)
    {
        var trendingList = string.Join("\n",
            trendingHashtags.Take(50).Select(h =>
                $"- #{h.Tag} ({GetDifficultyText(h.DifficultyLevel)}, {FormatNumber(h.LatestPostCount)} views)"
            ));

        return $@"Bạn là chuyên gia TikTok hashtag cho thị trường Việt Nam. Nhiệm vụ của bạn là gợi ý hashtag tối ưu cho video dựa trên mô tả và danh sách trending hashtags.

**Mô tả video:**
""{description}""

**Danh sách Trending Hashtags (Việt Nam):**
{trendingList}

**Yêu cầu:**
1. Gợi ý 5-7 hashtags tổng cộng
2. Phân loại:
   - TRENDING (1-2 tags): Tiếp cận rộng, cạnh tranh cao (>500K views)
   - NICHE (2-3 tags): Tiếp cận mục tiêu, cạnh tranh trung bình (10K-500K views)
   - ULTRA-NICHE (1-2 tags): Dễ lên top, cạnh tranh thấp (<10K views)
3. Tất cả hashtags PHẢI liên quan trực tiếp đến nội dung
4. Ưu tiên hashtag tiếng Việt phù hợp với người dùng Việt Nam
5. Tính toán expected reach (ước lượng số người tiếp cận)
6. Tính viral probability (0-100%) dựa trên competition level

**Output format (JSON):**
{{
  ""trending"": [
    {{
      ""tag"": ""amthuc"",
      ""viewCount"": 45000000,
      ""competitionLevel"": ""Cao"",
      ""expectedReach"": ""100K-500K người"",
      ""viralProbability"": 30,
      ""recommendationNote"": null
    }}
  ],
  ""niche"": [
    {{
      ""tag"": ""nauandonngian"",
      ""viewCount"": 2300000,
      ""competitionLevel"": ""Trung Bình"",
      ""expectedReach"": ""10K-50K người"",
      ""viralProbability"": 65,
      ""recommendationNote"": ""Đề xuất: Dễ viral cho creators mới""
    }}
  ],
  ""ultraNiche"": [
    {{
      ""tag"": ""phobotainha"",
      ""viewCount"": 156000,
      ""competitionLevel"": ""Thấp"",
      ""expectedReach"": ""2K-10K người"",
      ""viralProbability"": 85,
      ""recommendationNote"": ""Cơ hội cao để lên top niche""
    }}
  ],
  ""reasoning"": ""Brief explanation in Vietnamese about why these hashtags were chosen""
}}

IMPORTANT: Return ONLY valid JSON, no additional text.";
    }

    private async Task<string> CallOpenAIAsync(string prompt)
    {
        var chatCompletionsOptions = new ChatCompletionsOptions()
        {
            DeploymentName = _openAIOptions.Model,
            Messages =
            {
                new ChatRequestSystemMessage("You are a TikTok hashtag expert. Always return valid JSON."),
                new ChatRequestUserMessage(prompt)
            },
            MaxTokens = _openAIOptions.MaxTokens,
            Temperature = (float)_openAIOptions.Temperature,
        };

        Response<ChatCompletions> response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);

        ChatResponseMessage responseMessage = response.Value.Choices[0].Message;
        return responseMessage.Content;
    }

    private HashtagRecommendation ParseGPTResponse(string gptResponse)
    {
        try
        {
            // Clean response (remove markdown code blocks if present)
            var cleaned = gptResponse.Trim();
            if (cleaned.StartsWith("```json"))
            {
                cleaned = cleaned.Substring(7);
            }
            if (cleaned.StartsWith("```"))
            {
                cleaned = cleaned.Substring(3);
            }
            if (cleaned.EndsWith("```"))
            {
                cleaned = cleaned.Substring(0, cleaned.Length - 3);
            }
            cleaned = cleaned.Trim();

            // Parse JSON
            var jsonDoc = JsonDocument.Parse(cleaned);
            var root = jsonDoc.RootElement;

            var recommendation = new HashtagRecommendation
            {
                TrendingHashtags = ParseHashtagArray(root, "trending"),
                NicheHashtags = ParseHashtagArray(root, "niche"),
                UltraNicheHashtags = ParseHashtagArray(root, "ultraNiche"),
                Reasoning = root.TryGetProperty("reasoning", out var reasoning)
                    ? reasoning.GetString()
                    : null
            };

            return recommendation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing GPT response: {Response}", gptResponse);

            // Return fallback empty recommendation
            return new HashtagRecommendation
            {
                Reasoning = "Có lỗi khi phân tích kết quả. Vui lòng thử lại."
            };
        }
    }

    private List<RecommendedHashtag> ParseHashtagArray(JsonElement root, string propertyName)
    {
        var hashtags = new List<RecommendedHashtag>();

        if (!root.TryGetProperty(propertyName, out var array))
        {
            return hashtags;
        }

        foreach (var item in array.EnumerateArray())
        {
            hashtags.Add(new RecommendedHashtag
            {
                Tag = item.GetProperty("tag").GetString() ?? "",
                ViewCount = item.GetProperty("viewCount").GetInt64(),
                CompetitionLevel = item.GetProperty("competitionLevel").GetString() ?? "",
                ExpectedReach = item.GetProperty("expectedReach").GetString() ?? "",
                ViralProbability = item.GetProperty("viralProbability").GetInt32(),
                RecommendationNote = item.TryGetProperty("recommendationNote", out var note) && note.ValueKind != JsonValueKind.Null
                    ? note.GetString()
                    : null,
                IsSelected = true
            });
        }

        return hashtags;
    }

    public async Task<(bool allowed, int remaining)> CheckRateLimitAsync(int? userId, string? ipAddress)
    {
        var limit = _generatorOptions.FreeUserDailyLimit; // TODO: Check if premium user

        var now = DateTime.UtcNow;
        var windowStart = now.Date; // Today 00:00 UTC

        // Query rate limit record
        var rateLimitQuery = _context.GenerationRateLimits
            .Where(r => r.WindowStartTime >= windowStart);

        if (userId.HasValue)
        {
            rateLimitQuery = rateLimitQuery.Where(r => r.UserId == userId.Value);
        }
        else if (!string.IsNullOrEmpty(ipAddress))
        {
            rateLimitQuery = rateLimitQuery.Where(r => r.IpAddress == ipAddress);
        }
        else
        {
            // No user/IP - allow (shouldn't happen)
            return (true, limit);
        }

        var rateLimit = await rateLimitQuery.FirstOrDefaultAsync();

        if (rateLimit == null)
        {
            return (true, limit);
        }

        var remaining = limit - rateLimit.GenerationCount;
        return (remaining > 0, Math.Max(0, remaining));
    }

    private async Task IncrementRateLimitAsync(int? userId, string? ipAddress)
    {
        var now = DateTime.UtcNow;
        var windowStart = now.Date;

        var rateLimitQuery = _context.GenerationRateLimits
            .Where(r => r.WindowStartTime >= windowStart);

        if (userId.HasValue)
        {
            rateLimitQuery = rateLimitQuery.Where(r => r.UserId == userId.Value);
        }
        else if (!string.IsNullOrEmpty(ipAddress))
        {
            rateLimitQuery = rateLimitQuery.Where(r => r.IpAddress == ipAddress);
        }

        var rateLimit = await rateLimitQuery.FirstOrDefaultAsync();

        if (rateLimit == null)
        {
            // Create new
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
            // Increment
            rateLimit.GenerationCount++;
        }

        await _context.SaveChangesAsync();
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

    public async Task MarkAsCopiedAsync(int generationId)
    {
        var generation = await _context.HashtagGenerations.FindAsync(generationId);
        if (generation != null)
        {
            generation.WasCopied = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task SubmitFeedbackAsync(int generationId, string feedback)
    {
        var generation = await _context.HashtagGenerations.FindAsync(generationId);
        if (generation != null)
        {
            generation.UserFeedback = feedback;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<string> TestConnectionAsync()
    {
        var response = await CallOpenAIAsync("Say 'Hello from TrendTag!' in Vietnamese");
        return response;
    }

    // Helper methods
    private string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input.ToLowerInvariant().Trim());
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    private int CountTokens(string text)
    {
        // Rough estimation: 1 token ≈ 4 characters for English, 2-3 for Vietnamese
        return text.Length / 3;
    }

    private string GetDifficultyText(DifficultyLevel level)
    {
        return level switch
        {
            DifficultyLevel.Low => "Thấp",
            DifficultyLevel.Medium => "Trung Bình",
            DifficultyLevel.High => "Cao",
            DifficultyLevel.VeryHigh => "Rất Cao",
            _ => "N/A"
        };
    }

    private string FormatNumber(long number)
    {
        if (number >= 1_000_000)
            return $"{number / 1_000_000.0:F1}M";
        if (number >= 1_000)
            return $"{number / 1_000.0:F0}K";
        return number.ToString();
    }
}
```

### 3. Create Options: `Options/OpenAIOptions.cs`

```csharp
namespace HashTag.Options;

public class OpenAIOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4";
    public int MaxTokens { get; set; } = 800;
    public double Temperature { get; set; } = 0.7;
}

public class HashtagGeneratorOptions
{
    public int FreeUserDailyLimit { get; set; } = 5;
    public int PremiumUserDailyLimit { get; set; } = 100;
    public bool EnableCaching { get; set; } = true;
    public int CacheDurationDays { get; set; } = 7;
}
```

### 4. Update DbContext: `Data/TrendTagDbContext.cs`

Add these DbSets:

```csharp
public DbSet<HashtagGeneration> HashtagGenerations { get; set; }
public DbSet<GenerationHashtagSelection> GenerationHashtagSelections { get; set; }
public DbSet<GenerationRateLimit> GenerationRateLimits { get; set; }
```

### 5. Register in Program.cs

```csharp
// Add configuration
builder.Services.Configure<OpenAIOptions>(
    builder.Configuration.GetSection("OpenAI"));

builder.Services.Configure<HashtagGeneratorOptions>(
    builder.Configuration.GetSection("HashtagGenerator"));

// Register service
builder.Services.AddScoped<IHashtagGeneratorService, HashtagGeneratorService>();
```

---

## 📝 Next: Create Controller & Frontend

See `HASHTAG_GENERATOR_CONTROLLER.md` for API endpoints
See `HASHTAG_GENERATOR_UI.md` for frontend implementation

---

**Status:** Service implementation complete ✅
**Next Steps:**
1. Install packages
2. Configure API key
3. Create controller
4. Build UI
