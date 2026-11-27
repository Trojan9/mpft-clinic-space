using ClinicSpace.Application.Common;

namespace ClinicSpace.UnitTests.Application;

public class UkDateFormatterTests
{
    [Fact]
    public void Winter_times_are_shown_in_GMT()
    {
        var value = new DateTimeOffset(2026, 12, 8, 9, 0, 0, TimeSpan.Zero);

        UkDateFormatter.FormatDateTime(value).Should().Be("Tuesday 8 December 2026 at 9am");
    }

    [Fact]
    public void Summer_times_are_shown_in_BST()
    {
        // 08:30 UTC in July is 09:30 British Summer Time.
        var value = new DateTimeOffset(2026, 7, 14, 8, 30, 0, TimeSpan.Zero);

        UkDateFormatter.FormatDateTime(value).Should().Be("Tuesday 14 July 2026 at 9:30am");
    }

    [Theory]
    [InlineData(0, 0, "12am")]
    [InlineData(12, 0, "12pm")]
    [InlineData(14, 30, "2:30pm")]
    [InlineData(23, 45, "11:45pm")]
    public void Times_use_the_NHS_style(int hour, int minute, string expected) =>
        UkDateFormatter.FormatTime(new DateTimeOffset(2026, 1, 15, hour, minute, 0, TimeSpan.Zero)).Should().Be(expected);

    [Fact]
    public void A_same_day_period_names_the_date_once()
    {
        var start = new DateTimeOffset(2026, 12, 8, 9, 0, 0, TimeSpan.Zero);

        UkDateFormatter.FormatPeriod(start, start.AddHours(8)).Should().Be("Tuesday 8 December 2026, 9am to 5pm");
    }

    [Fact]
    public void A_multi_day_period_names_both_dates()
    {
        var start = new DateTimeOffset(2026, 12, 8, 9, 0, 0, TimeSpan.Zero);

        UkDateFormatter.FormatPeriod(start, start.AddDays(1)).Should()
            .Be("Tuesday 8 December 2026 at 9am to Wednesday 9 December 2026 at 9am");
    }
}
