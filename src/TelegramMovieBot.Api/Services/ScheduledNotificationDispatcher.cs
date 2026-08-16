namespace TelegramMovieBot.Api.Services;

public sealed class ScheduledNotificationDispatcher(
    IServiceScopeFactory scopeFactory,
    INotificationDelay notificationDelay,
    ILogger<ScheduledNotificationDispatcher> logger)
    : IScheduledNotificationDispatcher
{
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5)
    ];

    private DateOnly? _lastSuccessfulDate;

    public async Task<bool> DispatchAsync(
        DateOnly notificationDate,
        CancellationToken cancellationToken)
    {
        if (_lastSuccessfulDate == notificationDate)
        {
            logger.LogInformation(
                "{NotificationDate} tarihli bildirim daha önce gönderildi.",
                notificationDate);
            return false;
        }

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var notificationService = scope.ServiceProvider
                    .GetRequiredService<IMovieNotificationService>();

                await notificationService.SendMovieNotificationAsync(cancellationToken);
                _lastSuccessfulDate = notificationDate;

                logger.LogInformation(
                    "{NotificationDate} tarihli otomatik bildirim gönderildi.",
                    notificationDate);
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Otomatik bildirim denemesi başarısız. Deneme: {Attempt}/3",
                    attempt);

                if (attempt < 3)
                {
                    await notificationDelay.DelayAsync(
                        RetryDelays[attempt - 1],
                        cancellationToken);
                }
            }
        }

        logger.LogError(
            "{NotificationDate} tarihli bildirim üç denemede gönderilemedi.",
            notificationDate);
        return false;
    }
}
