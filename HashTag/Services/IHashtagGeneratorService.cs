using HashTag.ViewModels;

namespace HashTag.Services;

/// <summary>
/// Service for AI-powered hashtag generation
/// </summary>
public interface IHashtagGeneratorService
{
    /// <summary>
    /// Generate recommended hashtags based on video description using AI (V1)
    /// </summary>
    Task<HashtagGeneratorResponse> GenerateHashtagsAsync(string description, int? userId = null, string? ipAddress = null);

    /// <summary>
    /// Generate recommended hashtags using optimized 2-step approach (V2 - 73% token reduction)
    /// Step 1: Identify category from description
    /// Step 2: Select hashtags by ID from filtered list
    /// </summary>
    Task<HashtagGeneratorResponse> GenerateHashtagsV2Async(string description, int? userId = null, string? ipAddress = null);

    /// <summary>
    /// Check if user/IP has exceeded rate limit
    /// </summary>
    Task<(bool IsAllowed, int RemainingGenerations)> CheckRateLimitAsync(int? userId, string? ipAddress);

    /// <summary>
    /// Test OpenAI API connection
    /// </summary>
    Task<string> TestConnectionAsync();
}
