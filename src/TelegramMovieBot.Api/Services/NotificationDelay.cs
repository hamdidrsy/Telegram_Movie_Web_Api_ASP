namespace TelegramMovieBot.Api.Services;

public sealed class NotificationDelay(TimeProvider timeProvider) : INotificationDelay
{
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        Task.Delay(delay, timeProvider, cancellationToken);
}
