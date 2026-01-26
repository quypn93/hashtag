namespace HashTag.Models;

/// <summary>
/// System log for tracking background service lifecycle and important events
/// Useful for diagnosing IIS app pool recycling issues
/// </summary>
public class SystemLog
{
    public int Id { get; set; }

    /// <summary>
    /// Service or component name (e.g., "HashtagCrawlerHostedService")
    /// </summary>
    public string ServiceName { get; set; } = null!;

    /// <summary>
    /// Event type: Started, Stopped, Scheduled, Error, Info
    /// </summary>
    public string EventType { get; set; } = null!;

    /// <summary>
    /// Detailed message about the event
    /// </summary>
    public string Message { get; set; } = null!;

    /// <summary>
    /// When the event occurred
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Additional data (JSON) - e.g., next scheduled time, config values
    /// </summary>
    public string? AdditionalData { get; set; }
}
