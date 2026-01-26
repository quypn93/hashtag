namespace HashTag.Options;

/// <summary>
/// Hashtag Generator configuration options
/// </summary>
public class HashtagGeneratorOptions
{
    public int FreeUserDailyLimit { get; set; } = 5;
    public int PremiumUserDailyLimit { get; set; } = 100;
    public bool EnableCaching { get; set; } = true;
    public int CacheDurationDays { get; set; } = 7;
}
