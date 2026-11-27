using System.Globalization;

namespace ClinicSpace.Application.Common;

/// <summary>
/// Formats dates and times in UK local time, following the NHS digital service manual style:
/// "Tuesday 8 December 2026", "9am", "2:30pm".
/// Times are stored as UTC offsets and always shown in Europe/London time, so BST is handled.
/// </summary>
public static class UkDateFormatter
{
    private static readonly CultureInfo UkCulture = CultureInfo.GetCultureInfo("en-GB");

    public static TimeZoneInfo UkTimeZone { get; } = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

    public static DateTimeOffset ToUkTime(DateTimeOffset value) => TimeZoneInfo.ConvertTime(value, UkTimeZone);

    public static string FormatDate(DateTimeOffset value) =>
        ToUkTime(value).ToString("dddd d MMMM yyyy", UkCulture);

    public static string FormatTime(DateTimeOffset value)
    {
        var local = ToUkTime(value);
        var hour = local.Hour % 12 == 0 ? 12 : local.Hour % 12;
        var suffix = local.Hour < 12 ? "am" : "pm";
        return local.Minute == 0
            ? $"{hour}{suffix}"
            : $"{hour}:{local.Minute:D2}{suffix}";
    }

    public static string FormatDateTime(DateTimeOffset value) => $"{FormatDate(value)} at {FormatTime(value)}";

    /// <summary>"Tuesday 8 December 2026, 9am to 5pm" or a two-date range across days.</summary>
    public static string FormatPeriod(DateTimeOffset start, DateTimeOffset end)
    {
        var s = ToUkTime(start);
        var e = ToUkTime(end);
        return s.Date == e.Date
            ? $"{FormatDate(start)}, {FormatTime(start)} to {FormatTime(end)}"
            : $"{FormatDateTime(start)} to {FormatDateTime(end)}";
    }
}
