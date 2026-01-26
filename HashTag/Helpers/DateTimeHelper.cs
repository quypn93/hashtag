namespace HashTag.Helpers;

/// <summary>
/// Helper methods for DateTime operations, especially timezone conversions
/// </summary>
public static class DateTimeHelper
{
    // Vietnam timezone (UTC+7)
    private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    /// <summary>
    /// Convert UTC DateTime to Vietnam time (UTC+7)
    /// Assumes datetime from database is UTC even if Kind is Unspecified
    /// </summary>
    public static DateTime ToVietnamTime(this DateTime utcDateTime)
    {
        // If Kind is Unspecified (common from SQL Server), treat as UTC
        if (utcDateTime.Kind == DateTimeKind.Unspecified)
        {
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }

        if (utcDateTime.Kind != DateTimeKind.Utc)
        {
            // Already local time
            return utcDateTime;
        }

        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, VietnamTimeZone);
    }

    /// <summary>
    /// Format DateTime to Vietnamese format with timezone
    /// Assumes datetime from database is UTC
    /// </summary>
    public static string ToVietnameseString(this DateTime dateTime, string format = "dd/MM/yyyy HH:mm")
    {
        var vietnamTime = dateTime.ToVietnamTime();
        return vietnamTime.ToString(format);
    }
}
