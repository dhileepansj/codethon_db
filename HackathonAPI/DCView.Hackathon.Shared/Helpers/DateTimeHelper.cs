namespace DCView.Hackathon.Shared.Helpers;

/// <summary>
/// Centralized DateTime helper. All timestamps use Indian Standard Time (IST).
/// Change the TimeZoneId here to switch the entire application's timezone.
/// </summary>
public static class DateTimeHelper
{
    private static readonly TimeZoneInfo AppTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

    /// <summary>
    /// Returns the current date/time in the application's configured timezone (IST).
    /// </summary>
    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, AppTimeZone);

    /// <summary>
    /// Returns today's date at midnight in the app timezone.
    /// </summary>
    public static DateTime Today => Now.Date;

    /// <summary>
    /// Converts a UTC DateTime to the app's local timezone.
    /// </summary>
    public static DateTime FromUtc(DateTime utcDateTime) =>
        TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, AppTimeZone);

    /// <summary>
    /// Converts a local app time to UTC.
    /// </summary>
    public static DateTime ToUtc(DateTime localDateTime) =>
        TimeZoneInfo.ConvertTimeToUtc(localDateTime, AppTimeZone);
}
