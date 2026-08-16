using TelegramMovieBot.Api.Options;
using TelegramMovieBot.Api.Services;

namespace TelegramMovieBot.Tests.Services;

public sealed class NotificationScheduleTests
{
    [Fact]
    public void GetNextRunUtc_BeforeScheduledTime_ReturnsToday()
    {
        var schedule = CreateSchedule(hour: 10, minute: 0);
        var utcNow = new DateTimeOffset(2026, 8, 16, 6, 0, 0, TimeSpan.Zero);

        var nextRun = schedule.GetNextRunUtc(utcNow);

        Assert.Equal(
            new DateTimeOffset(2026, 8, 16, 7, 0, 0, TimeSpan.Zero),
            nextRun);
    }

    [Fact]
    public void GetNextRunUtc_AfterScheduledTime_ReturnsTomorrow()
    {
        var schedule = CreateSchedule(hour: 10, minute: 0);
        var utcNow = new DateTimeOffset(2026, 8, 16, 9, 0, 0, TimeSpan.Zero);

        var nextRun = schedule.GetNextRunUtc(utcNow);

        Assert.Equal(
            new DateTimeOffset(2026, 8, 17, 7, 0, 0, TimeSpan.Zero),
            nextRun);
    }

    [Fact]
    public void GetNextRunUtc_ExactlyAtScheduledTime_ReturnsTomorrow()
    {
        var schedule = CreateSchedule(hour: 10, minute: 0);
        var utcNow = new DateTimeOffset(2026, 8, 16, 7, 0, 0, TimeSpan.Zero);

        var nextRun = schedule.GetNextRunUtc(utcNow);

        Assert.Equal(
            new DateTimeOffset(2026, 8, 17, 7, 0, 0, TimeSpan.Zero),
            nextRun);
    }

    [Fact]
    public void GetLocalDate_UsesIstanbulTime()
    {
        var schedule = CreateSchedule(hour: 10, minute: 0);
        var utcNow = new DateTimeOffset(2026, 8, 15, 22, 30, 0, TimeSpan.Zero);

        var localDate = schedule.GetLocalDate(utcNow);

        Assert.Equal(new DateOnly(2026, 8, 16), localDate);
    }

    [Fact]
    public void IsTimeZoneValid_RejectsUnknownTimeZone()
    {
        Assert.True(NotificationSchedule.IsTimeZoneValid("Europe/Istanbul"));
        Assert.False(NotificationSchedule.IsTimeZoneValid("Unknown/Nowhere"));
    }

    private static NotificationSchedule CreateSchedule(int hour, int minute)
    {
        var options = Microsoft.Extensions.Options.Options.Create(
            new NotificationOptions
            {
                Hour = hour,
                Minute = minute,
                TimeZone = "Europe/Istanbul"
            });

        return new NotificationSchedule(options);
    }
}
