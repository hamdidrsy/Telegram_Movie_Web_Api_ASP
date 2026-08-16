namespace TelegramMovieBot.Api.Services;

public interface IScheduledNotificationDispatcher
{
    Task<bool> DispatchAsync(
        DateOnly notificationDate,
        CancellationToken cancellationToken);
}
