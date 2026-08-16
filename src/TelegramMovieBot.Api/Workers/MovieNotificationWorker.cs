using Microsoft.Extensions.Options;
using TelegramMovieBot.Api.Options;
using TelegramMovieBot.Api.Services;

namespace TelegramMovieBot.Api.Workers;

public sealed class MovieNotificationWorker(
    NotificationSchedule schedule,
    IScheduledNotificationDispatcher dispatcher,
    IOptions<NotificationOptions> options,
    TimeProvider timeProvider,
    ILogger<MovieNotificationWorker> logger) : BackgroundService
{
    private readonly NotificationOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Otomatik film bildirimi kapalı.");
            return;
        }

        logger.LogInformation(
            "Otomatik film bildirimi etkin. Plan: {Hour:D2}:{Minute:D2} ({TimeZone})",
            _options.Hour,
            _options.Minute,
            _options.TimeZone);

        while (!stoppingToken.IsCancellationRequested)
        {
            var utcNow = timeProvider.GetUtcNow();
            var nextRunUtc = schedule.GetNextRunUtc(utcNow);
            var delay = nextRunUtc - utcNow;

            logger.LogInformation(
                "Sonraki otomatik film bildirimi: {NextRunUtc} UTC",
                nextRunUtc);

            await Task.Delay(delay, timeProvider, stoppingToken);

            var notificationDate = schedule.GetLocalDate(timeProvider.GetUtcNow());
            await dispatcher.DispatchAsync(notificationDate, stoppingToken);
        }
    }
}
