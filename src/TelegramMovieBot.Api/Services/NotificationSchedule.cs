using Microsoft.Extensions.Options;
using TelegramMovieBot.Api.Options;

namespace TelegramMovieBot.Api.Services;

public sealed class NotificationSchedule
{
    private readonly NotificationOptions _options;
    private readonly TimeZoneInfo _timeZone;

    public NotificationSchedule(IOptions<NotificationOptions> options)
    {
        _options = options.Value;
        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZone);
    }

    public DateTimeOffset GetNextRunUtc(DateTimeOffset utcNow)
    {
        var localNow = TimeZoneInfo.ConvertTime(utcNow, _timeZone);
        var nextLocal = new DateTime(
            localNow.Year,
            localNow.Month,
            localNow.Day,
            _options.Hour,
            _options.Minute,
            0,
            DateTimeKind.Unspecified);

        if (nextLocal <= localNow.DateTime)
        {
            nextLocal = nextLocal.AddDays(1);
        }

        var nextUtc = TimeZoneInfo.ConvertTimeToUtc(nextLocal, _timeZone);
        return new DateTimeOffset(nextUtc, TimeSpan.Zero);
    }

    public DateOnly GetLocalDate(DateTimeOffset utcNow)
    {
        var localTime = TimeZoneInfo.ConvertTime(utcNow, _timeZone);
        return DateOnly.FromDateTime(localTime.DateTime);
    }

    public static bool IsTimeZoneValid(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return false;
        }

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}
