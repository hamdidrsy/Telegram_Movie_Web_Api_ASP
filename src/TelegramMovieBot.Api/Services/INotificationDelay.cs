namespace TelegramMovieBot.Api.Services;

public interface INotificationDelay
{
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}
